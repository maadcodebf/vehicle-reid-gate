using Microsoft.Extensions.Options;
using VehicleReId.Api.Data;
using VehicleReId.Api.Models;

namespace VehicleReId.Api.Services;

public class ReIdService
{
    private readonly AppDbContext _db;
    private readonly EmbeddingService _emb;
    private readonly QdrantService _qdrant;
    private readonly ReIdOptions _opt;

    public ReIdService(AppDbContext db, EmbeddingService emb, QdrantService qdrant, IOptions<ReIdOptions> opt)
    {
        _db = db;
        _emb = emb;
        _qdrant = qdrant;
        _opt = opt.Value;
    }

    public async Task<EnrollResponse> EnrollAsync(EnrollRequest req)
    {
        if (req.Images is null || req.Images.Count == 0 || req.Images.Count > 3)
            throw new ArgumentException("Images must contain 1 to 3 items.");

        var ev = new PassageEvent
        {
            BarrierId = req.BarrierId,
            TimestampUtc = req.TimestampUtc,
            ExternalTruckId = req.ExternalTruckId
        };
        _db.PassageEvents.Add(ev);
        await _db.SaveChangesAsync();

        int stored = 0;
        foreach (var img in req.Images)
        {
            var vec = _emb.EmbedFromBase64Jpeg(img.Base64Jpeg);
            var pointId = Guid.NewGuid().ToString("N");

            var payload = new
            {
                passage_event_id = ev.Id,
                barrier_id = req.BarrierId,
                timestamp_utc = req.TimestampUtc.ToString("O")
            };

            await _qdrant.UpsertAsync(pointId, vec, payload);

            _db.EmbeddingRecords.Add(new EmbeddingRecord
            {
                PassageEventId = ev.Id,
                BarrierId = req.BarrierId,
                TimestampUtc = req.TimestampUtc,
                ImageName = img.FileName,
                QdrantPointId = pointId
            });

            stored++;
        }

        await _db.SaveChangesAsync();
        return new EnrollResponse(ev.Id, stored);
    }

    public async Task<MatchResponse> MatchAsync(MatchRequest req)
    {
        if (req.Images is null || req.Images.Count == 0 || req.Images.Count > 3)
            throw new ArgumentException("Images must contain 1 to 3 items.");

        var from = req.TimestampUtc.AddMinutes(-Math.Abs(req.TimeWindowMinutes)).ToString("O");
        var to = req.TimestampUtc.AddMinutes(1).ToString("O");

        var filter = new
        {
            must = new object[]
            {
                new { key = "barrier_id", match = new { value = req.PreviousBarrierId } },
                new { key = "timestamp_utc", range = new { gte = from, lte = to } }
            }
        };

        var allCandidates = new List<MatchCandidate>();

        foreach (var img in req.Images)
        {
            var q = _emb.EmbedFromBase64Jpeg(img.Base64Jpeg);
            var hits = await _qdrant.SearchAsync(q, req.TopK, filter);

            foreach (var h in hits)
            {
                var passageEventId = h.Payload.GetProperty("passage_event_id").GetGuid();
                var barrierId = h.Payload.GetProperty("barrier_id").GetString()!;
                var ts = DateTime.Parse(h.Payload.GetProperty("timestamp_utc").GetString()!).ToUniversalTime();

                allCandidates.Add(new MatchCandidate(h.Id, passageEventId, h.Score, barrierId, ts));
            }
        }

        var merged = allCandidates
            .GroupBy(x => x.PassageEventId)
            .Select(g => g.OrderByDescending(x => x.Score).First())
            .OrderByDescending(x => x.Score)
            .Take(req.TopK)
            .ToList();

        var best = merged.FirstOrDefault();
        var bestScore = best?.Score ?? 0f;

        string decision = bestScore >= _opt.ThresholdHigh ? "MATCH"
                        : bestScore >= _opt.ThresholdLow ? "UNCERTAIN"
                        : "NO_MATCH";

        return new MatchResponse(decision, bestScore, best, merged);
    }
}

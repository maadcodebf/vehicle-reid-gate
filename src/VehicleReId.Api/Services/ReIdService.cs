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

    public async Task<EnrollResponse> EnrollAsync(EnrollFormRequest req)
    {
        if (req.Images is null || req.Images.Count == 0 || req.Images.Count > 3)
            throw new ArgumentException("Debe enviar entre 1 y 3 imágenes.");

        var ev = new PassageEvent
        {
            BarrierId = "GLOBAL",
            TimestampUtc = req.TimestampUtc,
            ExternalTruckId = req.LicensePlate
        };
        _db.PassageEvents.Add(ev);
        await _db.SaveChangesAsync();

        int stored = 0;
        foreach (var file in req.Images)
        {
            var vec = _emb.EmbedFromFormFile(file);
            var pointId = Guid.NewGuid().ToString("N");

            var payload = new
            {
                passage_event_id = ev.Id,
                license_plate = req.LicensePlate,
                timestamp_utc = req.TimestampUtc.ToString("O")
            };

            await _qdrant.UpsertAsync(pointId, vec, payload);

            _db.EmbeddingRecords.Add(new EmbeddingRecord
            {
                PassageEventId = ev.Id,
                BarrierId = "GLOBAL",
                TimestampUtc = req.TimestampUtc,
                ImageName = file.FileName,
                QdrantPointId = pointId
            });

            stored++;
        }

        await _db.SaveChangesAsync();
        return new EnrollResponse(ev.Id, req.LicensePlate, stored);
    }

    public async Task<MatchResponse> MatchAsync(MatchFormRequest req)
    {
        if (req.Images is null || req.Images.Count == 0 || req.Images.Count > 3)
            throw new ArgumentException("Debe enviar entre 1 y 3 imágenes.");

        object? filter = null;
        if (req.TimeWindowMinutes > 0)
        {
            var from = req.TimestampUtc.AddMinutes(-Math.Abs(req.TimeWindowMinutes)).ToString("O");
            var to = req.TimestampUtc.AddMinutes(5).ToString("O");

            filter = new
            {
                must = new object[]
                {
                    new { key = "timestamp_utc", range = new { gte = from, lte = to } }
                }
            };
        }

        var allCandidates = new List<MatchCandidate>();

        foreach (var file in req.Images)
        {
            var q = _emb.EmbedFromFormFile(file);
            var hits = await _qdrant.SearchAsync(q, req.TopK, filter);

            foreach (var h in hits)
            {
                var passageEventId = h.Payload.GetProperty("passage_event_id").GetGuid();
                var licensePlate = h.Payload.TryGetProperty("license_plate", out var lp) ? lp.GetString()! : "UNKNOWN";
                var ts = DateTime.Parse(h.Payload.GetProperty("timestamp_utc").GetString()!).ToUniversalTime();

                allCandidates.Add(new MatchCandidate(h.Id, passageEventId, licensePlate, h.Score, ts));
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

        string? matchedPlate = decision != "NO_MATCH" ? best?.LicensePlate : null;

        return new MatchResponse(decision, bestScore, matchedPlate, best, merged);
    }
}
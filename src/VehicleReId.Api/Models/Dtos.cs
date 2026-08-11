namespace VehicleReId.Api.Models;

public class EnrollFormRequest
{
    public required string LicensePlate { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public required List<IFormFile> Images { get; set; }
}

public class MatchFormRequest
{
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public int TimeWindowMinutes { get; set; } = 120;
    public required List<IFormFile> Images { get; set; }
    public int TopK { get; set; } = 1;
}

public record EnrollResponse(Guid PassageEventId, string LicensePlate, int StoredEmbeddings);

public record MatchCandidate(
    string QdrantPointId,
    Guid PassageEventId,
    string LicensePlate,
    float Score,
    DateTime TimestampUtc
);

public record MatchResponse(
    string Decision,
    float BestScore,
    string? MatchedLicensePlate,
    MatchCandidate? BestCandidate,
    List<MatchCandidate> TopCandidates
);
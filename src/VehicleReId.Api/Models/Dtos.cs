namespace VehicleReId.Api.Models;

// Input image as base64 JPEG
public record ImagePayload(string FileName, string Base64Jpeg);

// Request to enroll a new event (store vectors)
public record EnrollRequest(
    string BarrierId,
    DateTime TimestampUtc,
    string? ExternalTruckId,
    List<ImagePayload> Images
);

// Response after enrollment
public record EnrollResponse(Guid PassageEventId, int StoredEmbeddings);

// Request to match current truck against previous barrier events
public record MatchRequest(
    string CurrentBarrierId,
    string PreviousBarrierId,
    DateTime TimestampUtc,
    int TimeWindowMinutes,
    List<ImagePayload> Images,
    int TopK = 5
);

// Candidate returned by vector search
public record MatchCandidate(
    string QdrantPointId,
    Guid PassageEventId,
    float Score,
    string BarrierId,
    DateTime TimestampUtc
);

// Final decision payload
public record MatchResponse(
    string Decision,
    float BestScore,
    MatchCandidate? BestCandidate,
    List<MatchCandidate> TopCandidates
);

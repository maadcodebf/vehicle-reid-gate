namespace VehicleReId.Api.Models;

public class EmbeddingRecord
{
    // Internal unique id of this embedding record in SQL
    public Guid Id { get; set; } = Guid.NewGuid();

    // FK to passage event
    public Guid PassageEventId { get; set; }
    public PassageEvent PassageEvent { get; set; } = default!;

    // Redundant fields for easier querying/reporting
    public string BarrierId { get; set; } = default!;
    public DateTime TimestampUtc { get; set; }

    // Original file name (for traceability/debug)
    public string ImageName { get; set; } = default!;

    // Point id used in Qdrant for vector search
    public string QdrantPointId { get; set; } = default!;
}

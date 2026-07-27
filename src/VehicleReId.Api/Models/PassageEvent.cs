namespace VehicleReId.Api.Models;

public class PassageEvent
{
    // Internal unique id of the event
    public Guid Id { get; set; } = Guid.NewGuid();

    // Barrier/camera logical identifier (e.g. B1, B2, Gate-3)
    public string BarrierId { get; set; } = default!;

    // UTC timestamp when the event happened
    public DateTime TimestampUtc { get; set; }

    // Optional external business id if available in your flow
    public string? ExternalTruckId { get; set; }
}

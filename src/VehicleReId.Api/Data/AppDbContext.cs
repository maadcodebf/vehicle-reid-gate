using Microsoft.EntityFrameworkCore;
using VehicleReId.Api.Models;

namespace VehicleReId.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // One row per truck passage event at a barrier
    public DbSet<PassageEvent> PassageEvents => Set<PassageEvent>();

    // One row per stored embedding (usually 1..3 per passage event)
    public DbSet<EmbeddingRecord> EmbeddingRecords => Set<EmbeddingRecord>();
}

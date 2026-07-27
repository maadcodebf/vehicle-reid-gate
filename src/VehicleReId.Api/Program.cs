using Microsoft.EntityFrameworkCore;
using VehicleReId.Api.Data;
using VehicleReId.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Register ASP.NET services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register SQLite EF Core context
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// Bind settings from appsettings.json into typed options
builder.Services.Configure<ReIdOptions>(builder.Configuration.GetSection("ReId"));
builder.Services.Configure<QdrantOptions>(builder.Configuration.GetSection("Qdrant"));

// Register app services
builder.Services.AddSingleton<EmbeddingService>(); // ONNX model loaded once
builder.Services.AddHttpClient<QdrantService>();   // HTTP client for Qdrant REST API
builder.Services.AddScoped<ReIdService>();         // Per-request business logic

var app = builder.Build();

// Ensure DB schema and vector collection exist at startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    var qdrant = scope.ServiceProvider.GetRequiredService<QdrantService>();
    await qdrant.EnsureCollectionExistsAsync();
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.Run();

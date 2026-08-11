using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using VehicleReId.Api.Data;
using VehicleReId.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services.Configure<ReIdOptions>(builder.Configuration.GetSection("ReId"));
builder.Services.Configure<QdrantOptions>(builder.Configuration.GetSection("Qdrant"));

builder.Services.AddSingleton<EmbeddingService>();
builder.Services.AddHttpClient<QdrantService>();
builder.Services.AddScoped<ReIdService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    var qdrant = scope.ServiceProvider.GetRequiredService<QdrantService>();
    await qdrant.EnsureCollectionExistsAsync();
}

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.WithTitle("Vehicle Re-ID Gate API")
           .WithTheme(ScalarTheme.Moon);
});

app.MapControllers();
app.Run();
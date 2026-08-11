using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace VehicleReId.Api.Services;

public class QdrantService
{
    private readonly HttpClient _http;
    private readonly ReIdOptions _reid;

    public QdrantService(HttpClient http, IOptions<ReIdOptions> reidOpt, IOptions<QdrantOptions> qOpt)
    {
        _http = http;
        _reid = reidOpt.Value;
        _http.BaseAddress = new Uri(qOpt.Value.BaseUrl);
    }

    public async Task EnsureCollectionExistsAsync()
    {
        var get = await _http.GetAsync($"/collections/{_reid.CollectionName}");
        if (get.IsSuccessStatusCode) return;

        var body = new { vectors = new { size = _reid.VectorSize, distance = "Cosine" } };
        var res = await _http.PutAsync($"/collections/{_reid.CollectionName}", Json(body));
        res.EnsureSuccessStatusCode();
    }

    public async Task UpsertAsync(string pointId, float[] vector, object payload)
    {
        var body = new { points = new[] { new { id = pointId, vector, payload } } };
        var res = await _http.PutAsync($"/collections/{_reid.CollectionName}/points", Json(body));
        res.EnsureSuccessStatusCode();
    }

    public async Task<List<(string Id, float Score, JsonElement Payload)>> SearchAsync(float[] queryVector, int topK, object? filter = null)
    {
        var body = new { vector = queryVector, limit = topK, with_payload = true, filter };
        var res = await _http.PostAsync($"/collections/{_reid.CollectionName}/points/search", Json(body));
        res.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var arr = doc.RootElement.GetProperty("result").EnumerateArray();

        var list = new List<(string, float, JsonElement)>();
        foreach (var x in arr)
        {
            var id = x.GetProperty("id").ToString();
            var score = x.GetProperty("score").GetSingle();
            var payload = x.GetProperty("payload").Clone();
            list.Add((id, score, payload));
        }
        return list;
    }

    private static StringContent Json(object o) =>
        new(JsonSerializer.Serialize(o), Encoding.UTF8, "application/json");
}
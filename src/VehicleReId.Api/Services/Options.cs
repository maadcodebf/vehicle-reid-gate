namespace VehicleReId.Api.Services;

// Settings for Re-ID pipeline
public class ReIdOptions
{
    public string OnnxModelPath { get; set; } = "";
    public string InputName { get; set; } = "input";
    public string OutputName { get; set; } = "output";
    public int InputWidth { get; set; } = 256;
    public int InputHeight { get; set; } = 256;
    public int VectorSize { get; set; } = 512;
    public string CollectionName { get; set; } = "truck_reid";
    public float ThresholdHigh { get; set; } = 0.82f;
    public float ThresholdLow { get; set; } = 0.72f;
}

// Settings for Qdrant endpoint
public class QdrantOptions
{
    public string BaseUrl { get; set; } = "http://localhost:6333";
}

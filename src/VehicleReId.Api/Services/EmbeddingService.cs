using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using VehicleReId.Api.Models;

namespace VehicleReId.Api.Services;

public class EmbeddingService : IDisposable
{
    private readonly InferenceSession _session;
    private readonly ReIdOptions _opt;
    private readonly string _inputName;

    public EmbeddingService(IOptions<ReIdOptions> options)
    {
        _opt = options.Value;
        if (!File.Exists(_opt.OnnxModelPath))
            throw new FileNotFoundException($"No se encontró el modelo ONNX en: {_opt.OnnxModelPath}");

        _session = new InferenceSession(_opt.OnnxModelPath);
        
        // Auto-detectamos el tensor de entrada para evitar discrepancias de configuración
        _inputName = _session.InputMetadata.Keys.First();
        Console.WriteLine($"[ONNX Metadata] Detected Input Tensor Name: '{_inputName}'");
    }

    public float[] EmbedFromFormFile(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        byte[] bytes = ms.ToArray();

        using var img = Cv2.ImDecode(bytes, ImreadModes.Color);
        if (img.Empty()) throw new InvalidOperationException("No se pudo decodificar la imagen enviada.");

        return ExtractEmbedding(img);
    }

    public float[] EmbedFromBase64Jpeg(string base64)
    {
        var bytes = Convert.FromBase64String(base64);
        using var img = Cv2.ImDecode(bytes, ImreadModes.Color);
        if (img.Empty()) throw new InvalidOperationException("Imagen Base64 inválida.");

        return ExtractEmbedding(img);
    }

    private float[] ExtractEmbedding(Mat img)
    {
        using var resized = new Mat();
        Cv2.Resize(img, resized, new Size(_opt.InputWidth, _opt.InputHeight));

        var chw = ToNormalizedChw(resized);
        var tensor = new DenseTensor<float>(chw, new[] { 1, 3, _opt.InputHeight, _opt.InputWidth });

        using var results = _session.Run(new[]
        {
            NamedOnnxValue.CreateFromTensor(_inputName, tensor)
        });

        var output = string.IsNullOrWhiteSpace(_opt.OutputName) || !_session.OutputMetadata.ContainsKey(_opt.OutputName)
            ? results.First().AsEnumerable<float>().ToArray()
            : results.First(x => x.Name == _opt.OutputName).AsEnumerable<float>().ToArray();

        if (output.Length != _opt.VectorSize)
            throw new InvalidOperationException($"Tamaño de embedding inesperado {output.Length}, se esperaba {_opt.VectorSize}");

        return L2Normalize(output);
    }

    private static float[] ToNormalizedChw(Mat bgr)
    {
        int h = bgr.Rows; 
        int w = bgr.Cols;
        var chw = new float[3 * h * w];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                var p = bgr.At<Vec3b>(y, x);
                
                // Conversión BGR -> RGB y escalado [0, 1]
                float r = p.Item2 / 255f;
                float g = p.Item1 / 255f;
                float b = p.Item0 / 255f;

                // Estandarización ImageNet (Mean/Std)
                r = (r - 0.485f) / 0.229f;
                g = (g - 0.456f) / 0.224f;
                b = (b - 0.406f) / 0.225f;

                int idx = y * w + x;
                chw[idx] = r;
                chw[h * w + idx] = g;
                chw[2 * h * w + idx] = b;
            }
        }
        return chw;
    }

    private static float[] L2Normalize(float[] v)
    {
        double norm = Math.Sqrt(v.Sum(x => x * x)) + 1e-12;
        for (int i = 0; i < v.Length; i++) 
            v[i] = (float)(v[i] / norm);
        return v;
    }

    public void Dispose() => _session?.Dispose();
}
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.Extensions.Options;
using OpenCvSharp;

namespace VehicleReId.Api.Services;

public class EmbeddingService
{
    private readonly InferenceSession _session;
    private readonly ReIdOptions _opt;

    public EmbeddingService(IOptions<ReIdOptions> options)
    {
        _opt = options.Value;
        _session = new InferenceSession(_opt.OnnxModelPath);
    }

    public float[] EmbedFromBase64Jpeg(string base64)
    {
        var bytes = Convert.FromBase64String(base64);
        using var img = Cv2.ImDecode(bytes, ImreadModes.Color);
        if (img.Empty()) throw new InvalidOperationException("Invalid image");

        using var resized = new Mat();
        Cv2.Resize(img, resized, new Size(_opt.InputWidth, _opt.InputHeight));

        var chw = ToNormalizedChw(resized);
        var tensor = new DenseTensor<float>(chw, new[] { 1, 3, _opt.InputHeight, _opt.InputWidth });

        using var results = _session.Run(new[]
        {
            NamedOnnxValue.CreateFromTensor(_opt.InputName, tensor)
        });

        var output = string.IsNullOrWhiteSpace(_opt.OutputName)
            ? results.First().AsEnumerable<float>().ToArray()
            : results.First(x => x.Name == _opt.OutputName).AsEnumerable<float>().ToArray();

        if (output.Length != _opt.VectorSize)
            throw new InvalidOperationException($"Unexpected embedding size {output.Length}, expected {_opt.VectorSize}");

        return L2Normalize(output);
    }

    private static float[] ToNormalizedChw(Mat bgr)
    {
        int h = bgr.Rows; int w = bgr.Cols;
        var chw = new float[3 * h * w];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                var p = bgr.At<Vec3b>(y, x);
                float r = p.Item2 / 255f;
                float g = p.Item1 / 255f;
                float b = p.Item0 / 255f;

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
        for (int i = 0; i < v.Length; i++) v[i] = (float)(v[i] / norm);
        return v;
    }
}

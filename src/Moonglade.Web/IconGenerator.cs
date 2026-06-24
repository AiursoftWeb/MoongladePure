using SkiaSharp;
using System.Collections.Concurrent;

namespace MoongladePure.Web;

public static class MemoryStreamIconGenerator
{
    public static ConcurrentDictionary<string, byte[]> SiteIconDictionary { get; set; } = new();

    public static void GenerateIcons(string base64Data, string webRootPath, ILogger logger)
    {
        byte[] buffer;

        if (string.IsNullOrWhiteSpace(base64Data))
        {
            logger.LogWarning("SiteIconBase64 is empty or not valid, fall back to default image");

            var defaultIconImage = Path.Join($"{webRootPath}", "images", "siteicon-default.png");
            if (!File.Exists(defaultIconImage))
            {
                throw new FileNotFoundException("Can not find source image for generating favicons.", defaultIconImage);
            }

            var ext = Path.GetExtension(defaultIconImage);
            if (ext is not null && ext.ToLower() is not ".png")
            {
                throw new FormatException("Source file is not an PNG image.");
            }

            buffer = File.ReadAllBytes(defaultIconImage);
        }
        else
        {
            buffer = Convert.FromBase64String(base64Data);
        }

        using var ms = new MemoryStream(buffer);
        using var bitmap = SKBitmap.Decode(ms)
            ?? throw new InvalidOperationException("Invalid Site Icon Data");
        if (bitmap.Height != bitmap.Width)
        {
            throw new InvalidOperationException("Invalid Site Icon Data");
        }

        var dic = new Dictionary<string, int[]>
        {
            { "android-icon-", new[] { 36, 48, 72, 96, 144, 192 } },
            { "favicon-", new[] { 16, 32, 96 } },
            { "apple-icon-", new[] { 57, 60, 72, 76, 114, 120, 144, 152, 180 } }
        };

        foreach (var (key, value) in dic)
        {
            foreach (var size in value)
            {
                var fileName = $"{key}{size}x{size}.png";
                var bytes = ResizeImage(bitmap, size, size);

                SiteIconDictionary.TryAdd(fileName, bytes);
            }
        }

        var icon1Bytes = ResizeImage(bitmap, 192, 192);
        SiteIconDictionary.TryAdd("apple-icon.png", icon1Bytes);

        var icon2Bytes = ResizeImage(bitmap, 192, 192);
        SiteIconDictionary.TryAdd("apple-icon-precomposed.png", icon2Bytes);
    }

    public static byte[] GetIcon(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return null;
        return SiteIconDictionary.TryGetValue(fileName, out var value) ? value : null;
    }

    private static byte[] ResizeImage(SKBitmap source, int toWidth, int toHeight)
    {
        using var resized = new SKBitmap(toWidth, toHeight);
        using var canvas = new SKCanvas(resized);
        using var paint = new SKPaint();
        paint.IsAntialias = true;
canvas.DrawImage(SKImage.FromBitmap(source),
                    new SKRect(0, 0, source.Width, source.Height),
                    new SKRect(0, 0, toWidth, toHeight),
                    SKSamplingOptions.Default,
                    paint);
        canvas.Flush();

        using var image = SKImage.FromBitmap(resized);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var resultMs = new MemoryStream();
        data.SaveTo(resultMs);
        return resultMs.ToArray();
    }
}
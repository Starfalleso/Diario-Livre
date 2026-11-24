using DiarioLivre.Core.Interfaces;
using SkiaSharp;

namespace DiarioLivre.Infrastructure.Services;

/// <summary>
/// Processes local student photos by center-cropping and resizing to 256x256 WebP files via SkiaSharp.
/// </summary>
public sealed class SkiaAvatarService : IAvatarService
{
    private const int AvatarDimensionPx = 256;
    private const int WebpQuality = 85;
    private readonly string _avatarsDirectory;

    public SkiaAvatarService(string avatarsDirectory)
    {
        if (string.IsNullOrWhiteSpace(avatarsDirectory))
        {
            throw new ArgumentException("Avatars directory path cannot be empty.", nameof(avatarsDirectory));
        }

        _avatarsDirectory = avatarsDirectory;
        Directory.CreateDirectory(_avatarsDirectory);
    }

    /// <inheritdoc />
    public async Task<string> SaveAvatarAsync(string sourceFilePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
        {
            throw new FileNotFoundException("Source avatar image could not be found.", sourceFilePath);
        }

        await using FileStream stream = File.OpenRead(sourceFilePath);
        return await SaveAvatarAsync(stream, ct);
    }

    /// <inheritdoc />
    public async Task<string> SaveAvatarAsync(Stream imageStream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(imageStream);
        Directory.CreateDirectory(_avatarsDirectory);

        using MemoryStream buffer = new();
        await imageStream.CopyToAsync(buffer, ct);
        buffer.Position = 0;

        using SKBitmap source = SKBitmap.Decode(buffer)
            ?? throw new InvalidDataException("Unable to decode the supplied avatar image.");

        if (source.Width <= 0 || source.Height <= 0)
        {
            throw new InvalidDataException("Decoded avatar image has invalid dimensions.");
        }

        int side = Math.Min(source.Width, source.Height);
        int left = (source.Width - side) / 2;
        int top = (source.Height - side) / 2;

        var srcRect = new SKRect(left, top, left + side, top + side);
        var destRect = new SKRect(0, 0, AvatarDimensionPx, AvatarDimensionPx);

        using var target = new SKBitmap(AvatarDimensionPx, AvatarDimensionPx, SKColorType.Rgba8888, SKAlphaType.Premul);
        using (var canvas = new SKCanvas(target))
        using (var paint = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true })
        {
            canvas.Clear(SKColors.Transparent);
            canvas.DrawBitmap(source, srcRect, destRect, paint);
            canvas.Flush();
        }

        using SKImage encodedImage = SKImage.FromBitmap(target);
        using SKData encodedData = encodedImage.Encode(SKEncodedImageFormat.Webp, WebpQuality)
            ?? throw new InvalidOperationException("Failed to encode avatar image as WebP.");

        string fileName = $"{Guid.NewGuid():N}.webp";
        string outputPath = Path.Combine(_avatarsDirectory, fileName);

        await using FileStream output = File.Create(outputPath);
        await encodedData.AsStream().CopyToAsync(output, ct);

        return fileName;
    }

    /// <inheritdoc />
    public string? GetAvatarFullPath(string? photoFileName)
    {
        if (string.IsNullOrWhiteSpace(photoFileName))
        {
            return null;
        }

        string fullPath = Path.Combine(_avatarsDirectory, photoFileName);
        return File.Exists(fullPath) ? fullPath : null;
    }

    /// <inheritdoc />
    public void DeleteAvatar(string? photoFileName)
    {
        if (string.IsNullOrWhiteSpace(photoFileName))
        {
            return;
        }

        string fullPath = Path.Combine(_avatarsDirectory, photoFileName);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
}

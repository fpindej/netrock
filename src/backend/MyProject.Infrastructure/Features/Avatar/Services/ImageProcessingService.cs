using Microsoft.Extensions.Logging;
using MyProject.Application.Features.Avatar;
using MyProject.Application.Features.Avatar.Dtos;
using MyProject.Shared;
using SkiaSharp;

namespace MyProject.Infrastructure.Features.Avatar.Services;

/// <summary>
/// SkiaSharp-based image processing service for avatar images.
/// Validates, resizes, and re-encodes images to WebP format.
/// </summary>
internal sealed class ImageProcessingService(ILogger<ImageProcessingService> logger) : IImageProcessingService
{
    private const int MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
    private const int WebPQuality = 80;
    private const string WebPContentType = "image/webp";

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    /// <inheritdoc />
    public Result<ProcessedImageOutput> ProcessAvatar(byte[] imageData, string fileName, int maxDimension = 512)
    {
        if (imageData.Length > MaxFileSizeBytes)
        {
            return Result<ProcessedImageOutput>.Failure(ErrorMessages.Avatar.FileTooLarge);
        }

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
        {
            return Result<ProcessedImageOutput>.Failure(ErrorMessages.Avatar.UnsupportedFormat);
        }

        try
        {
            using var bitmap = SKBitmap.Decode(imageData);
            if (bitmap is null)
            {
                return Result<ProcessedImageOutput>.Failure(ErrorMessages.Avatar.ProcessingFailed);
            }

            var resized = ResizeProportionally(bitmap, maxDimension);
            try
            {
                var webpData = EncodeToWebP(resized);

                if (webpData is null || webpData.Length == 0)
                {
                    return Result<ProcessedImageOutput>.Failure(ErrorMessages.Avatar.ProcessingFailed);
                }

                logger.LogDebug(
                    "Processed avatar: {OriginalSize} bytes ({Width}x{Height}) → {ProcessedSize} bytes ({NewWidth}x{NewHeight}) WebP",
                    imageData.Length, bitmap.Width, bitmap.Height,
                    webpData.Length, resized.Width, resized.Height);

                var output = new ProcessedImageOutput(webpData, WebPContentType, webpData.Length);
                return Result<ProcessedImageOutput>.Success(output);
            }
            finally
            {
                if (!ReferenceEquals(resized, bitmap))
                {
                    resized.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process avatar image '{FileName}'", fileName);
            return Result<ProcessedImageOutput>.Failure(ErrorMessages.Avatar.ProcessingFailed);
        }
    }

    /// <summary>
    /// Resizes a bitmap proportionally to fit within the specified maximum dimension.
    /// Does not upscale images that are already smaller than the limit.
    /// </summary>
    private static SKBitmap ResizeProportionally(SKBitmap source, int maxDimension)
    {
        if (source.Width <= maxDimension && source.Height <= maxDimension)
        {
            return source;
        }

        var ratioX = (double)maxDimension / source.Width;
        var ratioY = (double)maxDimension / source.Height;
        var ratio = Math.Min(ratioX, ratioY);

        var newWidth = (int)Math.Round(source.Width * ratio);
        var newHeight = (int)Math.Round(source.Height * ratio);

        return source.Resize(new SKImageInfo(newWidth, newHeight), SKSamplingOptions.Default);
    }

    /// <summary>
    /// Encodes a bitmap to WebP format at the configured quality level.
    /// </summary>
    private static byte[] EncodeToWebP(SKBitmap bitmap)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Webp, WebPQuality);
        return data.ToArray();
    }
}

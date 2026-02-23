using MyProject.Application.Features.Avatar.Dtos;
using MyProject.Shared;

namespace MyProject.Application.Features.Avatar;

/// <summary>
/// Processes uploaded images (validation, resize, re-encode) for avatar usage.
/// </summary>
public interface IImageProcessingService
{
    /// <summary>
    /// Validates and processes an uploaded image into a normalized avatar format (WebP).
    /// </summary>
    /// <param name="imageData">The raw uploaded file bytes.</param>
    /// <param name="fileName">The original file name (used for extension validation).</param>
    /// <param name="maxDimension">Maximum width/height in pixels. Images are resized proportionally to fit; smaller images are not upscaled.</param>
    /// <returns>A Result containing the processed image data, or failure if validation fails.</returns>
    Result<ProcessedImageOutput> ProcessAvatar(byte[] imageData, string fileName, int maxDimension = 512);
}

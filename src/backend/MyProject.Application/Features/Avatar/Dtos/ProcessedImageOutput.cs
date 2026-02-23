namespace MyProject.Application.Features.Avatar.Dtos;

/// <summary>
/// Represents the result of processing an avatar image.
/// </summary>
/// <param name="ImageData">The processed image bytes (WebP format).</param>
/// <param name="ContentType">The MIME content type of the processed image.</param>
/// <param name="Size">The size in bytes of the processed image.</param>
public record ProcessedImageOutput(byte[] ImageData, string ContentType, long Size);

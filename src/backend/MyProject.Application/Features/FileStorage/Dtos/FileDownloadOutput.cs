namespace MyProject.Application.Features.FileStorage.Dtos;

/// <summary>
/// Represents the result of downloading a file from the object store.
/// </summary>
/// <param name="Data">The raw file bytes.</param>
/// <param name="ContentType">The MIME content type of the file.</param>
public record FileDownloadOutput(byte[] Data, string ContentType);

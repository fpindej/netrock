using MyProject.Application.Features.FileStorage.Dtos;
using MyProject.Shared;

namespace MyProject.Application.Features.FileStorage;

/// <summary>
/// Provides generic file storage operations against an S3-compatible object store.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Uploads a file to the object store.
    /// </summary>
    /// <param name="key">The object key (path) within the bucket.</param>
    /// <param name="data">The file contents.</param>
    /// <param name="contentType">The MIME content type of the file.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A Result indicating success or failure.</returns>
    Task<Result> UploadAsync(string key, byte[] data, string contentType, CancellationToken ct);

    /// <summary>
    /// Downloads a file from the object store.
    /// </summary>
    /// <param name="key">The object key (path) within the bucket.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A Result containing the file data and content type, or failure if not found.</returns>
    Task<Result<FileDownloadOutput>> DownloadAsync(string key, CancellationToken ct);

    /// <summary>
    /// Deletes a file from the object store.
    /// </summary>
    /// <param name="key">The object key (path) within the bucket.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A Result indicating success or failure.</returns>
    Task<Result> DeleteAsync(string key, CancellationToken ct);

    /// <summary>
    /// Checks whether a file exists in the object store.
    /// </summary>
    /// <param name="key">The object key (path) within the bucket.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns><c>true</c> if the object exists; otherwise <c>false</c>.</returns>
    Task<bool> ExistsAsync(string key, CancellationToken ct);
}

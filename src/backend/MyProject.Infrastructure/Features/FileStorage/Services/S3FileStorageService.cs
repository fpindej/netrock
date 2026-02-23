using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyProject.Application.Features.FileStorage;
using MyProject.Application.Features.FileStorage.Dtos;
using MyProject.Infrastructure.Features.FileStorage.Options;
using MyProject.Shared;

namespace MyProject.Infrastructure.Features.FileStorage.Services;

/// <summary>
/// S3-compatible implementation of <see cref="IFileStorageService"/> using the AWS SDK.
/// Works with MinIO (local) and any S3-compatible provider (production).
/// </summary>
internal sealed class S3FileStorageService(
    IAmazonS3 s3Client,
    IOptions<FileStorageOptions> options,
    ILogger<S3FileStorageService> logger) : IFileStorageService
{
    private readonly string _bucketName = options.Value.BucketName;
    private readonly SemaphoreSlim _bucketInitLock = new(1, 1);
    private bool _bucketEnsured;

    /// <inheritdoc />
    public async Task<Result> UploadAsync(string key, byte[] data, string contentType, CancellationToken ct)
    {
        try
        {
            await EnsureBucketExistsAsync(ct);

            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                ContentType = contentType,
                InputStream = new MemoryStream(data),
                AutoCloseStream = true
            };

            await s3Client.PutObjectAsync(request, ct);
            logger.LogDebug("Uploaded object '{Key}' to bucket '{Bucket}' ({Size} bytes)",
                key, _bucketName, data.Length);

            return Result.Success();
        }
        catch (AmazonS3Exception ex)
        {
            logger.LogError(ex, "Failed to upload object '{Key}' to bucket '{Bucket}'", key, _bucketName);
            return Result.Failure("Failed to upload file to storage.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Unexpected error uploading object '{Key}' to bucket '{Bucket}'", key, _bucketName);
            return Result.Failure("Failed to upload file to storage.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<FileDownloadOutput>> DownloadAsync(string key, CancellationToken ct)
    {
        try
        {
            var request = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            using var response = await s3Client.GetObjectAsync(request, ct);
            using var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream, ct);

            var output = new FileDownloadOutput(memoryStream.ToArray(), response.Headers.ContentType);
            return Result<FileDownloadOutput>.Success(output);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Result<FileDownloadOutput>.Failure("File not found.", ErrorType.NotFound);
        }
        catch (AmazonS3Exception ex)
        {
            logger.LogError(ex, "Failed to download object '{Key}' from bucket '{Bucket}'", key, _bucketName);
            return Result<FileDownloadOutput>.Failure("Failed to retrieve file from storage.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Unexpected error downloading object '{Key}' from bucket '{Bucket}'", key, _bucketName);
            return Result<FileDownloadOutput>.Failure("Failed to retrieve file from storage.");
        }
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(string key, CancellationToken ct)
    {
        try
        {
            var request = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            await s3Client.DeleteObjectAsync(request, ct);
            logger.LogDebug("Deleted object '{Key}' from bucket '{Bucket}'", key, _bucketName);

            return Result.Success();
        }
        catch (AmazonS3Exception ex)
        {
            logger.LogError(ex, "Failed to delete object '{Key}' from bucket '{Bucket}'", key, _bucketName);
            return Result.Failure("Failed to delete file from storage.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Unexpected error deleting object '{Key}' from bucket '{Bucket}'", key, _bucketName);
            return Result.Failure("Failed to delete file from storage.");
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string key, CancellationToken ct)
    {
        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            await s3Client.GetObjectMetadataAsync(request, ct);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (AmazonS3Exception ex)
        {
            logger.LogWarning(ex, "Failed to check existence of '{Key}' in bucket '{Bucket}'", key, _bucketName);
            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Unexpected error checking existence of '{Key}' in bucket '{Bucket}'", key, _bucketName);
            return false;
        }
    }

    /// <summary>
    /// Creates the bucket if it does not already exist (idempotent, thread-safe).
    /// Uses double-check locking so only the first concurrent request pays the S3 call cost.
    /// Handles <see cref="AmazonS3Exception"/> with "BucketAlreadyOwnedByYou" which MinIO throws
    /// instead of silently succeeding like AWS S3.
    /// </summary>
    private async Task EnsureBucketExistsAsync(CancellationToken ct)
    {
        if (_bucketEnsured) return;

        await _bucketInitLock.WaitAsync(ct);
        try
        {
            if (_bucketEnsured) return;

            await s3Client.PutBucketAsync(new PutBucketRequest { BucketName = _bucketName }, ct);
            _bucketEnsured = true;
            logger.LogDebug("Bucket '{Bucket}' is ready", _bucketName);
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode is "BucketAlreadyOwnedByYou" or "BucketAlreadyExists")
        {
            _bucketEnsured = true;
            logger.LogDebug("Bucket '{Bucket}' already exists", _bucketName);
        }
        catch (AmazonS3Exception ex)
        {
            logger.LogWarning(ex, "Failed to ensure bucket '{Bucket}' exists", _bucketName);
            throw;
        }
        finally
        {
            _bucketInitLock.Release();
        }
    }
}

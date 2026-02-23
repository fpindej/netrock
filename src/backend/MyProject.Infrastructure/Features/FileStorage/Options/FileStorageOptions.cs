using System.ComponentModel.DataAnnotations;

namespace MyProject.Infrastructure.Features.FileStorage.Options;

/// <summary>
/// Configuration options for S3-compatible file storage (MinIO locally, any S3-compatible service in production).
/// </summary>
public class FileStorageOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "FileStorage";

    /// <summary>
    /// The S3-compatible endpoint URL (e.g. <c>http://storage:9000</c> for MinIO).
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string Endpoint { get; init; } = string.Empty;

    /// <summary>
    /// The access key for S3 authentication.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string AccessKey { get; init; } = string.Empty;

    /// <summary>
    /// The secret key for S3 authentication.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string SecretKey { get; init; } = string.Empty;

    /// <summary>
    /// The bucket name for storing files.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string BucketName { get; init; } = string.Empty;

    /// <summary>
    /// The AWS region (only required for AWS S3, not MinIO).
    /// </summary>
    public string? Region { get; init; }

    /// <summary>
    /// Whether to use SSL/TLS for the S3 connection.
    /// </summary>
    public bool UseSSL { get; init; }
}

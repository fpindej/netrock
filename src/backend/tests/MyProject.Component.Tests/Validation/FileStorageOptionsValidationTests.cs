using System.ComponentModel.DataAnnotations;
using MyProject.Infrastructure.Features.FileStorage.Options;

namespace MyProject.Component.Tests.Validation;

public class FileStorageOptionsValidationTests
{
    private static List<ValidationResult> Validate(object instance)
    {
        var context = new ValidationContext(instance);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, context, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void ValidOptions_NoErrors()
    {
        var options = new FileStorageOptions
        {
            Endpoint = "http://storage:9000",
            AccessKey = "minioadmin",
            SecretKey = "minioadmin",
            BucketName = "test-files"
        };

        var results = Validate(options);

        Assert.Empty(results);
    }

    [Fact]
    public void MissingEndpoint_ReturnsError()
    {
        var options = new FileStorageOptions
        {
            Endpoint = "",
            AccessKey = "minioadmin",
            SecretKey = "minioadmin",
            BucketName = "test-files"
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(FileStorageOptions.Endpoint)));
    }

    [Fact]
    public void MissingAccessKey_ReturnsError()
    {
        var options = new FileStorageOptions
        {
            Endpoint = "http://storage:9000",
            AccessKey = "",
            SecretKey = "minioadmin",
            BucketName = "test-files"
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(FileStorageOptions.AccessKey)));
    }

    [Fact]
    public void MissingSecretKey_ReturnsError()
    {
        var options = new FileStorageOptions
        {
            Endpoint = "http://storage:9000",
            AccessKey = "minioadmin",
            SecretKey = "",
            BucketName = "test-files"
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(FileStorageOptions.SecretKey)));
    }

    [Fact]
    public void MissingBucketName_ReturnsError()
    {
        var options = new FileStorageOptions
        {
            Endpoint = "http://storage:9000",
            AccessKey = "minioadmin",
            SecretKey = "minioadmin",
            BucketName = ""
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(FileStorageOptions.BucketName)));
    }

    [Fact]
    public void OptionalRegion_Null_NoErrors()
    {
        var options = new FileStorageOptions
        {
            Endpoint = "http://storage:9000",
            AccessKey = "minioadmin",
            SecretKey = "minioadmin",
            BucketName = "test-files",
            Region = null
        };

        var results = Validate(options);

        Assert.Empty(results);
    }

    [Fact]
    public void OptionalRegion_WithValue_NoErrors()
    {
        var options = new FileStorageOptions
        {
            Endpoint = "https://s3.amazonaws.com",
            AccessKey = "access-key",
            SecretKey = "secret-key",
            BucketName = "my-bucket",
            Region = "us-east-1",
            UseSSL = true
        };

        var results = Validate(options);

        Assert.Empty(results);
    }
}

using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyProject.Application.Features.FileStorage;
using MyProject.Infrastructure.Features.FileStorage.Options;
using MyProject.Infrastructure.Features.FileStorage.Services;

namespace MyProject.Infrastructure.Features.FileStorage.Extensions;

/// <summary>
/// Extension methods for registering file storage services.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers file storage services (S3-compatible) with options validation.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddFileStorageServices(IConfiguration configuration)
        {
            services.AddOptions<FileStorageOptions>()
                .BindConfiguration(FileStorageOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            var options = configuration
                .GetSection(FileStorageOptions.SectionName)
                .Get<FileStorageOptions>()!;

            services.AddSingleton<IAmazonS3>(_ =>
            {
                var config = new AmazonS3Config
                {
                    ServiceURL = options.Endpoint,
                    ForcePathStyle = true,
                    UseHttp = !options.UseSSL
                };

                if (!string.IsNullOrEmpty(options.Region))
                {
                    config.AuthenticationRegion = options.Region;
                }

                return new AmazonS3Client(options.AccessKey, options.SecretKey, config);
            });

            services.AddSingleton<IFileStorageService, S3FileStorageService>();

            return services;
        }
    }
}

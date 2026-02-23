using Microsoft.Extensions.DependencyInjection;
using MyProject.Application.Features.Avatar;
using MyProject.Infrastructure.Features.Avatar.Services;

namespace MyProject.Infrastructure.Features.Avatar.Extensions;

/// <summary>
/// Extension methods for registering avatar feature services.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the image processing service for avatar handling.
        /// </summary>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddAvatarServices()
        {
            services.AddSingleton<IImageProcessingService, ImageProcessingService>();
            return services;
        }
    }
}

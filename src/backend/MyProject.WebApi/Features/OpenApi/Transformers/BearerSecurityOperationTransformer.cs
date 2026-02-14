using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace MyProject.WebApi.Features.OpenApi.Transformers;

/// <summary>
/// Applies the <c>bearerAuth</c> security requirement to operations whose endpoint
/// is protected by <see cref="AuthorizeAttribute"/>, unless overridden by
/// <see cref="AllowAnonymousAttribute"/>.
/// Uses reflection with <c>inherit: true</c> so attributes on base controllers are detected.
/// </summary>
internal sealed class BearerSecurityOperationTransformer : IOpenApiOperationTransformer
{
    /// <inheritdoc />
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;

        // EndpointMetadata doesn't include inherited attributes from base controllers,
        // so we also check via reflection on the controller type and action method.
        var hasAuthorize = metadata.OfType<AuthorizeAttribute>().Any();
        var hasAllowAnonymous = metadata.OfType<AllowAnonymousAttribute>().Any();

        if (!hasAuthorize && context.Description.ActionDescriptor is ControllerActionDescriptor cad)
        {
            hasAuthorize = cad.ControllerTypeInfo.IsDefined(typeof(AuthorizeAttribute), inherit: true)
                        || cad.MethodInfo.IsDefined(typeof(AuthorizeAttribute), inherit: true);

            if (!hasAllowAnonymous)
            {
                hasAllowAnonymous = cad.MethodInfo.IsDefined(typeof(AllowAnonymousAttribute), inherit: true);
            }
        }

        if (hasAuthorize && !hasAllowAnonymous)
        {
            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("bearerAuth", context.Document)] = []
            });
        }

        return Task.CompletedTask;
    }
}

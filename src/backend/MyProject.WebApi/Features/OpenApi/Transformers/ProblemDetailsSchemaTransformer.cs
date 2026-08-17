using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using MyProject.WebApi.Shared;

namespace MyProject.WebApi.Features.OpenApi.Transformers;

/// <summary>
/// Documents the <c>code</c> extension on every <see cref="ProblemDetails"/>-derived schema.
/// The extension is written at runtime via <c>ProblemDetails.Extensions</c>, which the schema
/// generator cannot see, so it is declared here to keep generated clients (frontend <c>v1.d.ts</c>) accurate.
/// </summary>
/// <remarks>See .claude/skills/backend-conventions/SKILL.md for the error code convention.</remarks>
internal sealed class ProblemDetailsSchemaTransformer : IOpenApiSchemaTransformer
{
    /// <inheritdoc />
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (!typeof(ProblemDetails).IsAssignableFrom(context.JsonTypeInfo.Type))
        {
            return Task.CompletedTask;
        }

        schema.Properties ??= new Dictionary<string, IOpenApiSchema>();
        schema.Properties[ProblemFactory.CodeExtensionKey] = new OpenApiSchema
        {
            Type = JsonSchemaType.String,
            Description = "Stable, machine-readable error code (snake_case). Use it to branch on the error " +
                          "or as a translation key instead of matching the human-readable detail."
        };

        return Task.CompletedTask;
    }
}

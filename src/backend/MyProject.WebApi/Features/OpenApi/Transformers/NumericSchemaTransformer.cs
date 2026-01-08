using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace MyProject.WebApi.Features.OpenApi.Transformers;

internal sealed class NumericSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        if (underlyingType == typeof(int) || underlyingType == typeof(long) ||
            underlyingType == typeof(double) || underlyingType == typeof(float) ||
            underlyingType == typeof(decimal))
        {
            if (schema.Type.HasValue && schema.Type.Value.HasFlag(JsonSchemaType.String))
            {
                schema.Type &= ~JsonSchemaType.String;
                schema.Pattern = null;
            }
        }

        return Task.CompletedTask;
    }
}

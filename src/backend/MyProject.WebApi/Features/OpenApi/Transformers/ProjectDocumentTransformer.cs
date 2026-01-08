using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace MyProject.WebApi.Features.OpenApi.Transformers;

internal sealed class ProjectDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Info.Title = "MyProject API";
        document.Info.Version = "v1";
        document.Info.Description = """
                                    API uses cookie-based authentication.

                                    To authenticate:
                                    1. Call `POST /api/auth/login` with your credentials
                                    2. The response will set HttpOnly cookies containing the access and refresh tokens
                                    3. Subsequent requests will automatically include these cookies

                                    **Note:** Make sure "withCredentials" is enabled in your HTTP client to send cookies with requests.
                                    """;

        return Task.CompletedTask;
    }
}

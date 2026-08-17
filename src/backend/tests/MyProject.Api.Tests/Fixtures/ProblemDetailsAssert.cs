using System.Net.Http.Json;
using System.Text.Json;
using MyProject.Shared;

namespace MyProject.Api.Tests.Fixtures;

/// <summary>
/// Shared assertions for <c>ProblemDetails</c> (RFC 9457) responses.
/// </summary>
public static class ProblemDetailsAssert
{
    /// <summary>
    /// Asserts that the response body is a <c>ProblemDetails</c> with the expected status and, when
    /// <paramref name="expectedError"/> is given, the expected <c>detail</c> and <c>code</c>.
    /// </summary>
    public static async Task MatchesAsync(HttpResponseMessage response, int expectedStatus, Error? expectedError = null)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(expectedStatus, json.GetProperty("status").GetInt32());

        if (expectedError is not null)
        {
            Assert.Equal(expectedError.Message, json.GetProperty("detail").GetString());
            Assert.Equal(expectedError.Code, json.GetProperty("code").GetString());
        }
    }

    /// <summary>
    /// Asserts that the response body is a <c>ProblemDetails</c> with the expected status and <c>code</c>.
    /// Use for framework-generated bodies (validation, status code pages) that have no <c>ErrorMessages</c> entry.
    /// </summary>
    public static async Task HasCodeAsync(HttpResponseMessage response, int expectedStatus, string expectedCode)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(expectedStatus, json.GetProperty("status").GetInt32());
        Assert.Equal(expectedCode, json.GetProperty("code").GetString());
    }
}

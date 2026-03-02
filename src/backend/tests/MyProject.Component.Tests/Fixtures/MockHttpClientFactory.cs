namespace MyProject.Component.Tests.Fixtures;

/// <summary>
/// Test HTTP client factory that returns a client wired to the given handler.
/// </summary>
internal sealed class MockHttpClientFactory(MockHttpMessageHandler handler) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(handler);
}

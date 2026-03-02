using System.Net;
using System.Text;
using System.Text.Json;

namespace MyProject.Component.Tests.Fixtures;

/// <summary>
/// Test HTTP message handler that returns preconfigured responses.
/// Supports queuing multiple responses for sequential requests.
/// </summary>
internal sealed class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses = new();
    private readonly List<HttpRequestMessage> _sentRequests = [];
    private readonly List<string?> _capturedBodies = [];

    /// <summary>
    /// Gets all requests that were sent through this handler.
    /// </summary>
    public IReadOnlyList<HttpRequestMessage> SentRequests => _sentRequests;

    /// <summary>
    /// Gets the captured request body strings (read before the content is disposed).
    /// Index corresponds to <see cref="SentRequests"/>.
    /// </summary>
    public IReadOnlyList<string?> CapturedBodies => _capturedBodies;

    /// <summary>
    /// Enqueues a JSON response to be returned by the next matching request.
    /// </summary>
    public MockHttpMessageHandler WithJsonResponse(object body, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(body);
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        _responses.Enqueue(response);
        return this;
    }

    /// <summary>
    /// Enqueues a response with the given status code and no body.
    /// </summary>
    public MockHttpMessageHandler WithStatusCode(HttpStatusCode statusCode)
    {
        _responses.Enqueue(new HttpResponseMessage(statusCode));
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _sentRequests.Add(request);
        _capturedBodies.Add(request.Content is not null
            ? await request.Content.ReadAsStringAsync(cancellationToken)
            : null);

        if (_responses.Count == 0)
        {
            throw new InvalidOperationException(
                $"MockHttpMessageHandler: no response queued for {request.Method} {request.RequestUri}");
        }

        return _responses.Dequeue();
    }
}

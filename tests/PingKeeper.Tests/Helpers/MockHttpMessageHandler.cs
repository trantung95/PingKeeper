using System.Net;

namespace PingKeeper.Tests.Helpers;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _sendFunc;

    public List<HttpRequestMessage> SentRequests { get; } = [];

    public MockHttpMessageHandler(HttpStatusCode statusCode)
    {
        _sendFunc = (_, _) => Task.FromResult(new HttpResponseMessage(statusCode));
    }

    public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendFunc)
    {
        _sendFunc = sendFunc;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        SentRequests.Add(request);
        return await _sendFunc(request, cancellationToken);
    }
}

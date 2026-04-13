using System.Net;
using System.Text;
using System.Text.Json;

namespace trampbazaar.Tests;

internal sealed class StubApiServer : IAsyncDisposable
{
    private readonly HttpListener listener = new();
    private readonly Func<HttpListenerRequest, Task<StubApiResponse>> handler;
    private readonly CancellationTokenSource shutdown = new();
    private readonly Task loopTask;

    private StubApiServer(string baseUrl, Func<HttpListenerRequest, Task<StubApiResponse>> handler)
    {
        BaseUrl = baseUrl;
        this.handler = handler;
        listener.Prefixes.Add($"{baseUrl}/");
        listener.Start();
        loopTask = Task.Run(ListenLoopAsync);
    }

    public string BaseUrl { get; }

    public static StubApiServer Start(Func<HttpListenerRequest, Task<StubApiResponse>> handler)
    {
        var port = TestPortHelper.GetFreeTcpPort();
        return new StubApiServer($"http://127.0.0.1:{port}", handler);
    }

    public async ValueTask DisposeAsync()
    {
        shutdown.Cancel();
        listener.Stop();
        listener.Close();

        try
        {
            await loopTask;
        }
        catch
        {
        }

        shutdown.Dispose();
    }

    private async Task ListenLoopAsync()
    {
        while (!shutdown.IsCancellationRequested)
        {
            HttpListenerContext? context = null;

            try
            {
                context = await listener.GetContextAsync();
                var stubResponse = await handler(context.Request);
                await WriteResponseAsync(context.Response, stubResponse);
            }
            catch (HttpListenerException) when (shutdown.IsCancellationRequested)
            {
                return;
            }
            catch (ObjectDisposedException) when (shutdown.IsCancellationRequested)
            {
                return;
            }
            catch when (context is not null)
            {
                await WriteResponseAsync(context.Response, StubApiResponse.Text(HttpStatusCode.InternalServerError, "stub error"));
            }
        }
    }

    private static async Task WriteResponseAsync(HttpListenerResponse response, StubApiResponse stubResponse)
    {
        response.StatusCode = (int)stubResponse.StatusCode;
        response.ContentType = stubResponse.ContentType;

        if (!string.IsNullOrEmpty(stubResponse.Body))
        {
            var buffer = Encoding.UTF8.GetBytes(stubResponse.Body);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer);
        }

        response.Close();
    }
}

internal sealed record StubApiResponse(HttpStatusCode StatusCode, string ContentType, string Body)
{
    public static StubApiResponse Json(HttpStatusCode statusCode, object payload)
        => new(statusCode, "application/json", JsonSerializer.Serialize(payload));

    public static StubApiResponse Text(HttpStatusCode statusCode, string body)
        => new(statusCode, "text/plain", body);
}

internal static class TestPortHelper
{
    public static int GetFreeTcpPort()
    {
        using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        return ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
    }
}

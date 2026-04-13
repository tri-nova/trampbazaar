using System.Diagnostics;
namespace trampbazaar.Tests;

internal sealed class TestHostProcess : IAsyncDisposable
{
    private readonly Process _process;
    private readonly string _healthPath;

    private TestHostProcess(Process process, string baseUrl, string healthPath)
    {
        _process = process;
        BaseUrl = baseUrl;
        _healthPath = healthPath;
    }

    public string BaseUrl { get; }

    public static async Task<TestHostProcess> StartAsync(
        string projectPath,
        string workingDirectory,
        IReadOnlyDictionary<string, string> environmentVariables,
        string healthPath = "/health/live",
        CancellationToken cancellationToken = default)
    {
        var port = TestPortHelper.GetFreeTcpPort();
        var baseUrl = $"http://127.0.0.1:{port}";

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --no-launch-profile --project \"{projectPath}\" --urls {baseUrl}",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        foreach (var pair in environmentVariables)
        {
            startInfo.Environment[pair.Key] = pair.Value;
        }

        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Test host baslatilamadi: {projectPath}");

        var host = new TestHostProcess(process, baseUrl, healthPath);
        try
        {
            await host.WaitForHealthyAsync(cancellationToken);
            return host;
        }
        catch
        {
            await host.DisposeAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await _process.WaitForExitAsync(cts.Token);
        }
        catch
        {
        }

        _process.Dispose();
    }

    private async Task WaitForHealthyAsync(CancellationToken cancellationToken)
    {
        using var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(2)
        };

        var deadline = DateTimeOffset.UtcNow.AddSeconds(30);
        Exception? lastError = null;

        while (DateTimeOffset.UtcNow < deadline && !_process.HasExited)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var response = await client.GetAsync($"{BaseUrl}{_healthPath}", cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }

                lastError = new InvalidOperationException($"Health check basarisiz dondu: {(int)response.StatusCode}");
            }
            catch (Exception ex)
            {
                lastError = ex;
            }

            await Task.Delay(500, cancellationToken);
        }

        if (_process.HasExited)
        {
            var stdErr = await _process.StandardError.ReadToEndAsync(cancellationToken);
            var stdOut = await _process.StandardOutput.ReadToEndAsync(cancellationToken);
            throw new InvalidOperationException($"Test host erken kapandi.\nSTDOUT:\n{stdOut}\nSTDERR:\n{stdErr}");
        }

        throw new InvalidOperationException("Test host health durumuna gecemedi.", lastError);
    }
}

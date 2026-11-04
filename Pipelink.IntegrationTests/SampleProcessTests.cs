using System.Diagnostics;

namespace Pipelink.IntegrationTests;

public class SampleProcessTests
{
    [Fact]
    public async Task ConsoleExecutable_ShouldPrintGreetingAndExitZero_WhenNameArgumentPassed()
    {
        var (exitCode, output) = await RunSampleAsync("PipelinkTest.Console.dll", "QA");

        Assert.Equal(0, exitCode);
        Assert.Contains("Hello, QA!", output);
    }

    [Fact]
    public async Task WorkerExecutable_ShouldProcessOneTickAndExitZero_WhenRunOnceFlagPassed()
    {
        var (exitCode, output) = await RunSampleAsync("PipelinkTest.Worker.dll", "--run-once");

        Assert.Equal(0, exitCode);
        Assert.Contains("ProcessTickCommand tick=1 result=2", output);
    }

    private static async Task<(int ExitCode, string Output)> RunSampleAsync(string assemblyName, string argument)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, assemblyName));
        startInfo.ArgumentList.Add(argument);

        using var process = Process.Start(startInfo)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        return (process.ExitCode, await stdout + await stderr);
    }
}

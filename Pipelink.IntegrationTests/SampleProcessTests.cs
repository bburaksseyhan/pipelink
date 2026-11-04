using System.Diagnostics;

namespace Pipelink.IntegrationTests;

public class SampleProcessTests
{
    [Fact]
    public async Task ConsoleExecutable_ShouldPrintGreetingAndExitZero_WhenNameArgumentPassed()
    {
        var (exitCode, output) = await RunSampleAsync("PipelinkTest.Console", "QA");

        Assert.Equal(0, exitCode);
        Assert.Contains("Hello, QA!", output);
    }

    [Fact]
    public async Task WorkerExecutable_ShouldProcessOneTickAndExitZero_WhenRunOnceFlagPassed()
    {
        var (exitCode, output) = await RunSampleAsync("PipelinkTest.Worker", "--run-once");

        Assert.Equal(0, exitCode);
        Assert.Contains("ProcessTickCommand tick=1 result=2", output);
    }

    // Samples run from their own build output: copied into this test's output they would bind
    // against the test project's dependency set (ASP.NET Core) and miss their own package assets.
    private static string GetSampleAssemblyPath(string projectName)
    {
        var testOutput = new DirectoryInfo(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar));
        var targetFramework = testOutput.Name;
        var configuration = testOutput.Parent!.Name;

        var root = testOutput;
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "pipelink.sln")))
        {
            root = root.Parent;
        }

        return Path.Combine(root!.FullName, projectName, "bin", configuration, targetFramework, $"{projectName}.dll");
    }

    private static async Task<(int ExitCode, string Output)> RunSampleAsync(string projectName, string argument)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(GetSampleAssemblyPath(projectName));
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

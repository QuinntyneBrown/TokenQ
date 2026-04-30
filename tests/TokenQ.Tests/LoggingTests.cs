// Acceptance Test
// Traces to: L2-010, L2-011
// Description: Logger routes by level + stream, --verbose flips Info -> Debug, redaction holds

using Xunit;

namespace TokenQ.Tests;

public class LoggingTests : IDisposable
{
    private readonly List<string> _tempDirs = [];

    public void Dispose()
    {
        foreach (var dir in _tempDirs)
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
    }

    private string NewTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        _tempDirs.Add(dir);
        return dir;
    }

    [Fact] // L2-010 #1
    public void Run_Success_WritesInfoToStdoutNamingFile_NoDebug()
    {
        var dir = NewTempDir();

        var (stdout, _, exitCode) = RunMain("--name", "IFooService", "--output", dir);

        Assert.Equal(0, exitCode);
        Assert.Contains("info:", stdout);
        Assert.Contains(Path.Combine(dir, "foo-service.ts"), stdout);
        Assert.DoesNotContain("dbug:", stdout);
    }

    [Fact] // L2-010 #2
    public void Run_Verbose_EmitsDebugForParsingAndPathAndWrite()
    {
        var dir = NewTempDir();

        var (stdout, _, _) = RunMain("--name", "IFooService", "--output", dir, "--verbose");

        Assert.Contains("Rendering interface=", stdout);
        Assert.Contains("Resolved directory=", stdout);
        Assert.Contains("Write force=", stdout);
    }

    [Fact] // L2-010 #3
    public void Run_InvalidName_EmitsExactlyOneErrorToStderr()
    {
        var (_, stderr, _) = RunMain("--name", "1Bad");

        Assert.Equal(1, CountOccurrences(stderr, "fail:"));
    }

    [Fact] // L2-010 #3
    public void Run_FileCollisionWithoutForce_EmitsExactlyOneErrorToStderr()
    {
        var dir = NewTempDir();
        File.WriteAllText(Path.Combine(dir, "foo-service.ts"), "x");

        var (_, stderr, _) = RunMain("--name", "IFooService", "--output", dir);

        Assert.Equal(1, CountOccurrences(stderr, "fail:"));
    }

    [Fact] // L2-010 #4
    public void Run_AnyFailure_NeverLogsFileContentOrEnvVars()
    {
        var canary = "leak-canary-" + Guid.NewGuid().ToString("N");
        Environment.SetEnvironmentVariable("TOKENQ_LEAK_TEST", canary);
        try
        {
            var (stdout, stderr, _) = RunMain("--name", "1Bad");
            var combined = stdout + stderr;
            Assert.DoesNotContain(canary, combined);
            Assert.DoesNotContain("export interface", combined);
        }
        finally { Environment.SetEnvironmentVariable("TOKENQ_LEAK_TEST", null); }
    }

    [Fact] // L2-011 #3
    public void Run_Failure_NoStackInOutputUnlessVerbose()
    {
        var (_, stderrPlain, _) = RunMain("--name", "1Bad");
        var (_, stderrVerbose, _) = RunMain("--name", "1Bad", "--verbose");

        Assert.DoesNotContain("at TokenQ.", stderrPlain);
        Assert.Contains("at TokenQ.", stderrVerbose);
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        if (string.IsNullOrEmpty(haystack) || string.IsNullOrEmpty(needle)) return 0;
        var count = 0;
        var idx = 0;
        while ((idx = haystack.IndexOf(needle, idx, StringComparison.Ordinal)) != -1)
        {
            count++;
            idx += needle.Length;
        }
        return count;
    }

    private static (string stdout, string stderr, int exitCode) RunMain(params string[] args)
    {
        var origOut = Console.Out;
        var origErr = Console.Error;
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        Console.SetOut(stdout);
        Console.SetError(stderr);
        try
        {
            var exitCode = Program.Main(args);
            return (stdout.ToString(), stderr.ToString(), exitCode);
        }
        finally
        {
            Console.SetOut(origOut);
            Console.SetError(origErr);
        }
    }
}

// Acceptance Test
// Traces to: L2-003, L2-004, L2-008, L2-011, L2-016
// Description: tokenq CLI shell parses options, routes to generator + writer, maps exit codes

using Xunit;

namespace TokenQ.Tests;

public class ProgramTests : IDisposable
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

    [Fact] // L2-003 #1, L2-011 #1
    public void Main_WithValidName_WritesGeneratedContentAndReturnsZero()
    {
        var dir = NewTempDir();

        var (_, _, exitCode) = RunMain("--name", "IFooService", "--output", dir);

        Assert.Equal(0, exitCode);
        var content = File.ReadAllText(Path.Combine(dir, "foo.service.contract.ts"));
        Assert.Contains("export interface IFooService {", content);
        Assert.Contains("FOO_SERVICE", content);
    }

    [Fact] // L2-003 #2
    public void Main_WithShortNameAlias_WritesGeneratedContent()
    {
        var dir = NewTempDir();

        var (_, _, exitCode) = RunMain("-n", "IFooService", "-o", dir);

        Assert.Equal(0, exitCode);
        Assert.Contains("export interface IFooService {",
            File.ReadAllText(Path.Combine(dir, "foo.service.contract.ts")));
    }

    [Fact] // L2-003 #3, L2-016 #3
    public void Main_WithoutName_ReturnsNonZeroAndWritesParserErrorToStderr()
    {
        var (_, stderr, exitCode) = RunMain();

        Assert.NotEqual(0, exitCode);
        Assert.False(string.IsNullOrWhiteSpace(stderr));
    }

    [Fact] // L2-003 #4, L2-016 #1
    public void Main_WithHelpFlag_ListsAllOptionsAndReturnsZero()
    {
        var (stdout, _, exitCode) = RunMain("--help");

        Assert.Equal(0, exitCode);
        Assert.Contains("--name", stdout);
        Assert.Contains("--output", stdout);
        Assert.Contains("--force", stdout);
        Assert.Contains("--verbose", stdout);
        Assert.Contains("--help", stdout);
        Assert.Contains("--version", stdout);
    }

    [Fact] // L2-016 #2
    public void Main_WithVersionFlag_PrintsVersionAndReturnsZero()
    {
        var (stdout, _, exitCode) = RunMain("--version");

        Assert.Equal(0, exitCode);
        Assert.False(string.IsNullOrWhiteSpace(stdout));
    }

    [Fact] // L2-007 (integration), L2-011 #2
    public void Main_WithInvalidName_ReturnsOneAndWritesErrorToStderr()
    {
        var (_, stderr, exitCode) = RunMain("--name", "1Bad");

        Assert.Equal(1, exitCode);
        Assert.False(string.IsNullOrWhiteSpace(stderr));
    }

    [Fact] // L2-008 #3, L2-011 #2
    public void Main_WithIllegalOutputPath_ReturnsOneAndWritesErrorToStderr()
    {
        var (_, stderr, exitCode) = RunMain("--name", "IFooService", "--output", "\0bad");

        Assert.Equal(1, exitCode);
        Assert.False(string.IsNullOrWhiteSpace(stderr));
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

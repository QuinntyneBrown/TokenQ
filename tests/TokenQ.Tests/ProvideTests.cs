// Acceptance Test
// Traces to: L2-019, L2-020, L2-021, L2-029, L2-030
// Description: tokenq provide sub-command - directory scan, barrel render, file write end-to-end

using Xunit;

namespace TokenQ.Tests;

public class ProvideTests : IDisposable
{
    private readonly List<string> _tempRoots = [];

    public void Dispose()
    {
        foreach (var dir in _tempRoots)
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
    }

    private string NewFolderNamed(string folderName)
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var dir = Path.Combine(root, folderName);
        Directory.CreateDirectory(dir);
        _tempRoots.Add(root);
        return dir;
    }

    private static void Touch(string dir, params string[] fileNames)
    {
        foreach (var name in fileNames)
            File.WriteAllText(Path.Combine(dir, name), "");
    }

    [Fact] // L2-019 #1, end-to-end happy path matching slice 07 worked example
    public void Provide_FolderWithStorePair_WritesExpectedIndexTs()
    {
        var dir = NewFolderNamed("dashboard-state");
        Touch(dir,
            "dashboard-state-store.contract.ts",
            "dashboard-state.model.ts",
            "dashboard-state.store.ts",
            "wall-clock-tick-service.contract.ts",
            "wall-clock-tick.service.ts",
            "chart-visible-window.ts");

        var (_, _, exitCode) = RunMain("provide", "--path", dir);

        Assert.Equal(0, exitCode);
        var expected =
            "import { Provider } from '@angular/core';\n" +
            "\n" +
            "export { DashboardStateStore } from './dashboard-state.store';\n" +
            "export { WallClockTickService } from './wall-clock-tick.service';\n" +
            "export { DASHBOARD_STATE_STORE } from './dashboard-state-store.contract';\n" +
            "export { WALL_CLOCK_TICK_SERVICE } from './wall-clock-tick-service.contract';\n" +
            "export type { IDashboardStateStore } from './dashboard-state-store.contract';\n" +
            "export type { IWallClockTickService } from './wall-clock-tick-service.contract';\n" +
            "export type * from './dashboard-state.model';\n" +
            "\n" +
            "export function provideDashboardState(): Provider[] {\n" +
            "  return [\n" +
            "    DashboardStateStore,\n" +
            "    WallClockTickService,\n" +
            "    { provide: DASHBOARD_STATE_STORE, useExisting: DashboardStateStore },\n" +
            "    { provide: WALL_CLOCK_TICK_SERVICE, useExisting: WallClockTickService }\n" +
            "  ];\n" +
            "}\n";
        Assert.Equal(expected, File.ReadAllText(Path.Combine(dir, "index.ts")));
    }

    [Fact] // L2-019 #2
    public void Provide_NoPath_UsesCurrentWorkingDirectory()
    {
        var dir = NewFolderNamed("auth");
        Touch(dir, "user.store.ts", "user.store.contract.ts");
        var origCwd = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(dir);
        try
        {
            var (_, _, exitCode) = RunMain("provide");
            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(Path.Combine(dir, "index.ts")));
        }
        finally { Directory.SetCurrentDirectory(origCwd); }
    }

    [Fact] // L2-019 #3
    public void Provide_WithPath_UsesSuppliedPath()
    {
        var dir = NewFolderNamed("auth");
        Touch(dir, "user.store.ts", "user.store.contract.ts");

        var (_, _, exitCode) = RunMain("provide", "--path", dir);

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(Path.Combine(dir, "index.ts")));
    }

    [Fact] // L2-019 #1 (provide --help)
    public void Provide_HelpFlag_ListsOptionsAndReturnsZero()
    {
        var (stdout, _, exitCode) = RunMain("provide", "--help");

        Assert.Equal(0, exitCode);
        Assert.Contains("--path", stdout);
        Assert.Contains("--force", stdout);
        Assert.Contains("--verbose", stdout);
    }

    [Fact] // L2-019 #4
    public void Provide_RootHelp_ListsProvideAsSubcommand()
    {
        var (stdout, _, exitCode) = RunMain("--help");

        Assert.Equal(0, exitCode);
        Assert.Contains("provide", stdout);
    }

    [Fact] // L2-020 #1
    public void Provide_NonexistentDirectory_ReturnsOneAndWritesError()
    {
        var bogusPath = Path.Combine(Path.GetTempPath(),
            "does-not-exist-" + Guid.NewGuid().ToString("N"));

        var (_, stderr, exitCode) = RunMain("provide", "--path", bogusPath);

        Assert.Equal(1, exitCode);
        Assert.False(string.IsNullOrWhiteSpace(stderr));
        Assert.False(Directory.Exists(bogusPath));
    }

    [Fact] // L2-020 #2
    public void Provide_PathIsFile_ReturnsOne()
    {
        var dir = NewFolderNamed("temp");
        var filePath = Path.Combine(dir, "blocker.txt");
        File.WriteAllText(filePath, "x");

        var (_, _, exitCode) = RunMain("provide", "--path", filePath);

        Assert.Equal(1, exitCode);
    }

    [Fact] // L2-020 #3
    public void Provide_TraversalSegments_ResolvedAndUsed()
    {
        var dir = NewFolderNamed("dashboard");
        Touch(dir, "user.store.ts");
        var traversal = Path.Combine(dir, "..", "dashboard");

        var (_, _, exitCode) = RunMain("provide", "--path", traversal);

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(Path.Combine(dir, "index.ts")));
    }

    [Fact]
    public void Provide_EmptyFolder_WritesEmptyBarrel()
    {
        var dir = NewFolderNamed("empty");

        var (_, _, exitCode) = RunMain("provide", "--path", dir);

        Assert.Equal(0, exitCode);
        var content = File.ReadAllText(Path.Combine(dir, "index.ts"));
        Assert.Contains("provideEmpty(): Provider[]", content);
        Assert.Contains("return [];", content);
    }

    [Fact] // L2-021 #1, #2, #4
    public void Provide_IgnoresUnknownFilesAndSubdirectories()
    {
        var dir = NewFolderNamed("auth");
        Touch(dir, "user.store.ts", "user.store.contract.ts", "chart.ts", "README.md");
        var subDir = Path.Combine(dir, "sub");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "ignored.contract.ts"), "");

        var (_, _, exitCode) = RunMain("provide", "--path", dir);

        Assert.Equal(0, exitCode);
        var content = File.ReadAllText(Path.Combine(dir, "index.ts"));
        Assert.Contains("UserStore", content);
        Assert.DoesNotContain("chart", content);
        Assert.DoesNotContain("README", content);
        Assert.DoesNotContain("ignored", content);
    }

    [Fact] // L2-023 #6
    public void Provide_StoreSpecTsIgnored()
    {
        var dir = NewFolderNamed("auth");
        Touch(dir, "user.store.ts", "user.store.contract.ts", "user.store.spec.ts");

        var (_, _, exitCode) = RunMain("provide", "--path", dir);

        Assert.Equal(0, exitCode);
        var content = File.ReadAllText(Path.Combine(dir, "index.ts"));
        Assert.DoesNotContain("spec", content);
    }

    [Fact] // L2-029 #1
    public void Provide_ExistingIndexTsWithoutForce_ReturnsOneAndLeavesFileUnchanged()
    {
        var dir = NewFolderNamed("auth");
        Touch(dir, "user.store.ts");
        var indexPath = Path.Combine(dir, "index.ts");
        File.WriteAllText(indexPath, "// pre-existing");

        var (_, _, exitCode) = RunMain("provide", "--path", dir);

        Assert.Equal(1, exitCode);
        Assert.Equal("// pre-existing", File.ReadAllText(indexPath));
    }

    [Fact] // L2-029 #2
    public void Provide_ExistingIndexTsWithForce_Replaces()
    {
        var dir = NewFolderNamed("auth");
        Touch(dir, "user.store.ts");
        var indexPath = Path.Combine(dir, "index.ts");
        File.WriteAllText(indexPath, "// pre-existing");

        var (_, _, exitCode) = RunMain("provide", "--path", dir, "--force");

        Assert.Equal(0, exitCode);
        Assert.Contains("UserStore", File.ReadAllText(indexPath));
    }

    [Fact] // L2-030 #1 end-to-end
    public void Provide_TwoRunsWithForce_ProduceIdenticalBytes()
    {
        var dir = NewFolderNamed("auth");
        Touch(dir, "user.store.ts", "user.store.contract.ts");

        RunMain("provide", "--path", dir);
        var first = File.ReadAllBytes(Path.Combine(dir, "index.ts"));
        RunMain("provide", "--path", dir, "--force");
        var second = File.ReadAllBytes(Path.Combine(dir, "index.ts"));

        Assert.Equal(first, second);
    }

    [Fact] // regression: existing root command still works
    public void Main_WithoutSubcommand_GenerateContractStillWorks()
    {
        var dir = NewFolderNamed("output");

        var (_, _, exitCode) = RunMain("--name", "EventStore", "--output", dir);

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(Path.Combine(dir, "event.store.contract.ts")));
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

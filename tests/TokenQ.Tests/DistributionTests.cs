// Acceptance Test
// Traces to: L2-012, L2-013, L2-014
// Description: Project targets net8.0 only; dotnet pack yields a valid tool package; pipeline is fast enough

using System.Diagnostics;
using System.IO.Compression;
using System.Xml.Linq;
using Xunit;

namespace TokenQ.Tests;

public sealed class DotnetPackFixture : IDisposable
{
    public string PackagePath { get; }
    private readonly string _tempDir;

    public DotnetPackFixture()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        var psi = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        psi.ArgumentList.Add("pack");
        psi.ArgumentList.Add(ProjectPath);
        psi.ArgumentList.Add("-c");
        psi.ArgumentList.Add("Release");
        psi.ArgumentList.Add("-o");
        psi.ArgumentList.Add(_tempDir);
        psi.ArgumentList.Add("--nologo");

        using var proc = Process.Start(psi)!;
        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        if (proc.ExitCode != 0)
            throw new InvalidOperationException($"dotnet pack failed (exit {proc.ExitCode}):\n{stdout}\n{stderr}");

        PackagePath = Directory.GetFiles(_tempDir, "*.nupkg").Single();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
    }

    public static string ProjectPath
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !dir.GetFiles("TokenQ.sln").Any())
                dir = dir.Parent;
            if (dir is null) throw new InvalidOperationException("Cannot find repo root");
            return Path.Combine(dir.FullName, "src", "TokenQ", "TokenQ.csproj");
        }
    }
}

public class DistributionTests : IClassFixture<DotnetPackFixture>
{
    private readonly DotnetPackFixture _pack;
    public DistributionTests(DotnetPackFixture pack) { _pack = pack; }

    [Fact] // L2-012 #3
    public void Csproj_TargetsNet8Only()
    {
        var doc = XDocument.Load(DotnetPackFixture.ProjectPath);
        var tf = doc.Descendants("TargetFramework").Single().Value;
        Assert.Equal("net8.0", tf);
    }

    [Fact] // L2-013 #1
    public void DotnetPack_ProducesPackageWithRequiredMetadata()
    {
        using var zip = ZipFile.OpenRead(_pack.PackagePath);
        var nuspec = zip.Entries.Single(e => e.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));
        var doc = XDocument.Load(nuspec.Open());
        var ns = doc.Root!.GetDefaultNamespace();
        var meta = doc.Root!.Element(ns + "metadata")!;

        var dotnetTool = meta.Element(ns + "packageTypes")?
            .Elements(ns + "packageType")
            .FirstOrDefault(t => t.Attribute("name")?.Value == "DotnetTool");
        Assert.NotNull(dotnetTool);
        Assert.False(string.IsNullOrEmpty(meta.Element(ns + "id")?.Value));
        Assert.False(string.IsNullOrEmpty(meta.Element(ns + "version")?.Value));
        Assert.False(string.IsNullOrEmpty(meta.Element(ns + "authors")?.Value));
        Assert.False(string.IsNullOrEmpty(meta.Element(ns + "description")?.Value));
        Assert.NotNull(meta.Element(ns + "license"));
        Assert.Contains(zip.Entries, e => e.FullName.Contains("DotnetToolSettings.xml", StringComparison.OrdinalIgnoreCase));
    }

    [Fact] // L2-013 #3
    public void DotnetPack_PackageContainsOnlyToolsAndMetadata()
    {
        using var zip = ZipFile.OpenRead(_pack.PackagePath);
        var allowedPrefixes = new[] { "tools/", "_rels/", "package/" };
        var allowedRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "[Content_Types].xml", "README.md", ".signature.p7s" };

        foreach (var entry in zip.Entries)
        {
            var path = entry.FullName;
            Assert.False(path.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase),
                $".pdb file in package: {path}");

            if (path.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase)) continue;
            if (allowedPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase))) continue;
            if (allowedRoots.Contains(path)) continue;
            Assert.Fail($"Unexpected entry outside tools/+metadata: {path}");
        }

        Assert.Contains(zip.Entries, e => e.FullName.StartsWith("tools/", StringComparison.OrdinalIgnoreCase));
    }

    [Fact, Trait("Category", "Performance")] // L2-014 #1
    public void Performance_WarmPipeline_Under100ms_Median()
    {
        var dir = NewPerfTempDir();
        try
        {
            Program.Main(["--name", "IWarmUp", "--output", dir, "--force"]);
            var times = new long[100];
            for (int i = 0; i < 100; i++)
            {
                var sw = Stopwatch.StartNew();
                Program.Main(["--name", $"IFooService{i}", "--output", dir, "--force"]);
                sw.Stop();
                times[i] = sw.ElapsedMilliseconds;
            }
            Array.Sort(times);
            var median = times[50];
            Assert.True(median < 100, $"Median was {median}ms");
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); }
    }

    [Fact(Skip = "Cold start requires published binary outside test runner; verified manually before each release"),
     Trait("Category", "Performance")] // L2-014 #2
    public void Performance_ColdInvocation_Under2s() { }

    [Fact, Trait("Category", "Performance")] // L2-014 #3
    public void Performance_PeakWorkingSet_Under100MB()
    {
        var dir = NewPerfTempDir();
        try
        {
            for (int i = 0; i < 100; i++)
                Program.Main(["--name", $"IPerf{i}", "--output", dir, "--force"]);
            var peak = Process.GetCurrentProcess().PeakWorkingSet64;
            Assert.True(peak < 100L * 1024 * 1024, $"Peak was {peak / (1024 * 1024)}MB");
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); }
    }

    private static string NewPerfTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}

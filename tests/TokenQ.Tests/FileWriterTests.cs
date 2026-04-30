// Acceptance Test
// Traces to: L2-004, L2-006, L2-008, L2-009
// Description: FileWriter persists GeneratedFile under overwrite + traversal policies

using System.Text;
using Xunit;

namespace TokenQ.Tests;

public class FileWriterTests : IDisposable
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

    private static GeneratedFile File(string content = "hello\n") => new("foo.ts", content);

    [Fact] // L2-004 #2
    public void Write_NewFileInExistingDir_WritesContent()
    {
        var dir = NewTempDir();

        var written = new FileWriter().Write(dir, File("abc\n"), force: false);

        Assert.Equal(Path.Combine(dir, "foo.ts"), written);
        Assert.Equal("abc\n", System.IO.File.ReadAllText(written));
    }

    [Fact] // L2-004 #1
    public void Write_NoOutput_UsesCurrentWorkingDirectory()
    {
        var dir = NewTempDir();
        var origCwd = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(dir);
        try
        {
            var written = new FileWriter().Write(null, File("x\n"), force: false);

            Assert.Equal(Path.GetFullPath(Path.Combine(dir, "foo.ts")), Path.GetFullPath(written));
            Assert.True(System.IO.File.Exists(written));
        }
        finally { Directory.SetCurrentDirectory(origCwd); }
    }

    [Fact] // L2-004 #3
    public void Write_NestedNonexistentDir_CreatesParents()
    {
        var root = NewTempDir();
        var nested = Path.Combine(root, "a", "b", "c");

        var written = new FileWriter().Write(nested, File(), force: false);

        Assert.True(Directory.Exists(nested));
        Assert.True(System.IO.File.Exists(written));
    }

    [Fact] // L2-004 #4
    public void Write_DirectoryPathIsExistingFile_Throws()
    {
        var dir = NewTempDir();
        var blocker = Path.Combine(dir, "blocker");
        System.IO.File.WriteAllText(blocker, "x");

        Assert.Throws<OutputPathIsFileException>(
            () => new FileWriter().Write(blocker, File(), force: false));
    }

    [Fact] // L2-006 #1
    public void Write_ExistingFileWithoutForce_Throws_NotModified()
    {
        var dir = NewTempDir();
        var target = Path.Combine(dir, "foo.ts");
        System.IO.File.WriteAllText(target, "original");

        Assert.Throws<FileAlreadyExistsException>(
            () => new FileWriter().Write(dir, File("new"), force: false));
        Assert.Equal("original", System.IO.File.ReadAllText(target));
    }

    [Fact] // L2-006 #2
    public void Write_ExistingFileWithForce_Replaces()
    {
        var dir = NewTempDir();
        var target = Path.Combine(dir, "foo.ts");
        System.IO.File.WriteAllText(target, "original");

        new FileWriter().Write(dir, File("replaced\n"), force: true);

        Assert.Equal("replaced\n", System.IO.File.ReadAllText(target));
    }

    [Fact] // L2-006 #3
    public void Write_NonExistingFileWithForce_Writes()
    {
        var dir = NewTempDir();

        var written = new FileWriter().Write(dir, File("ok\n"), force: true);

        Assert.Equal("ok\n", System.IO.File.ReadAllText(written));
    }

    [Fact] // L2-008 #1
    public void Write_TraversalSegmentsResolvedAndContained()
    {
        var root = NewTempDir();
        var sub = Path.Combine(root, "sub");
        Directory.CreateDirectory(sub);
        var traversal = Path.Combine(sub, "..", "sub");

        var written = new FileWriter().Write(traversal, File(), force: false);

        Assert.Equal(
            Path.GetFullPath(Path.Combine(sub, "foo.ts")),
            Path.GetFullPath(written));
    }

    [Fact] // L2-008 #3
    public void Write_PathWithIllegalChars_Throws()
    {
        Assert.Throws<InvalidOutputPathException>(
            () => new FileWriter().Write("\0bad", File(), force: false));
    }

    [Fact(Skip = "ACL/permission setup is platform-specific; verified manually")] // L2-008 #2
    public void Write_DirectoryNotWritable_PropagatesUnauthorizedAccess() { }

    [Fact] // L2-009 #1
    public void Write_BytesIdenticalAcrossInvocations()
    {
        var dir = NewTempDir();
        var content = "import { X } from 'y';\nexport interface X {}\n";
        var writer = new FileWriter();

        var p1 = writer.Write(dir, new GeneratedFile("foo.ts", content), force: false);
        var b1 = System.IO.File.ReadAllBytes(p1);
        writer.Write(dir, new GeneratedFile("foo.ts", content), force: true);
        var b2 = System.IO.File.ReadAllBytes(p1);

        Assert.Equal(b1, b2);
        Assert.Equal(Encoding.UTF8.GetBytes(content), b1);
    }
}

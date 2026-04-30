using System.Text;

namespace TokenQ;

public sealed class FileWriter
{
    public string Write(string? outputDirectory, GeneratedFile file, bool force)
    {
        string dirAbsolute, fileAbsolute;
        try
        {
            dirAbsolute = string.IsNullOrEmpty(outputDirectory)
                ? Directory.GetCurrentDirectory()
                : Path.GetFullPath(outputDirectory);
            fileAbsolute = Path.GetFullPath(Path.Combine(dirAbsolute, file.Filename));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        {
            throw new InvalidOutputPathException(outputDirectory ?? string.Empty, ex);
        }

        var sep = Path.DirectorySeparatorChar.ToString();
        var dirWithSep = dirAbsolute.EndsWith(sep) ? dirAbsolute : dirAbsolute + sep;
        var cmp = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!fileAbsolute.StartsWith(dirWithSep, cmp))
            throw new OutputPathOutsideDirectoryException(fileAbsolute);

        if (System.IO.File.Exists(fileAbsolute) && !force)
            throw new FileAlreadyExistsException(fileAbsolute);

        if (System.IO.File.Exists(dirAbsolute) && !Directory.Exists(dirAbsolute))
            throw new OutputPathIsFileException(dirAbsolute);

        Directory.CreateDirectory(dirAbsolute);
        System.IO.File.WriteAllBytes(fileAbsolute, Encoding.UTF8.GetBytes(file.Content));
        return fileAbsolute;
    }
}

public sealed class FileAlreadyExistsException(string path)
    : IOException($"File already exists: {path}. Use --force to overwrite.");

public sealed class OutputPathOutsideDirectoryException(string path)
    : IOException($"Resolved file path escapes output directory: {path}");

public sealed class OutputPathIsFileException(string path)
    : IOException($"Output path exists but is a file, not a directory: {path}");

public sealed class InvalidOutputPathException(string path, Exception inner)
    : IOException($"Output path is not valid for this operating system: {path}", inner);

namespace TokenQ;

public sealed class FileWriter
{
    public string Write(string? outputDirectory, GeneratedFile file, bool force) =>
        throw new NotImplementedException();
}

public sealed class FileAlreadyExistsException(string path)
    : IOException($"File already exists: {path}. Use --force to overwrite.");

public sealed class OutputPathOutsideDirectoryException(string path)
    : IOException($"Resolved file path escapes output directory: {path}");

public sealed class OutputPathIsFileException(string path)
    : IOException($"Output path exists but is a file, not a directory: {path}");

public sealed class InvalidOutputPathException(string path, Exception inner)
    : IOException($"Output path is not valid for this operating system: {path}", inner);

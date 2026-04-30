namespace TokenQ;

public sealed record GeneratedFile(string Filename, string Content);

public sealed class InvalidNameException(string message) : Exception(message);

public sealed class Generator
{
    public GeneratedFile Render(string interfaceName) =>
        throw new NotImplementedException();
}

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace TokenQ;

public sealed class BarrelGenerator(ILogger<BarrelGenerator>? logger = null)
{
    private readonly ILogger _logger = logger ?? NullLogger<BarrelGenerator>.Instance;

    public GeneratedFile Render(string folderName, IReadOnlyList<string> fileNames) =>
        throw new NotImplementedException();
}

public sealed class InvalidFolderNameException(string folderName)
    : Exception($"Folder name '{folderName}' is not a valid kebab-case identifier.");

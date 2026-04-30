using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace TokenQ;

public sealed record GeneratedFile(string Filename, string Content);

public sealed class InvalidNameException(string message) : Exception(message);

public sealed class Generator(ILogger<Generator>? logger = null)
{
    private readonly ILogger _logger = logger ?? NullLogger<Generator>.Instance;

    private const string Template =
        "import { InjectionToken } from '@angular/core';\n" +
        "\n" +
        "export interface {INTERFACE} {\n" +
        "}\n" +
        "\n" +
        "export const {TOKEN} = new InjectionToken<{INTERFACE}>('{TOKEN}');\n";

    public GeneratedFile Render(string interfaceName)
    {
        if (NameValidator.Validate(interfaceName) is ValidationResult.Failed failed)
            throw new InvalidNameException(failed.Message);

        var bare = StripLeadingI(interfaceName);
        var words = SplitWords(bare);
        var stem = string.Join('-', words).ToLowerInvariant();
        var token = string.Join('_', words).ToUpperInvariant();

        var content = Template
            .Replace("{INTERFACE}", interfaceName)
            .Replace("{TOKEN}", token);

        return new GeneratedFile(stem + ".ts", content);
    }

    private static string StripLeadingI(string name) =>
        name.Length >= 2 && name[0] == 'I' && name[1] is >= 'A' and <= 'Z'
            ? name[1..]
            : name;

    internal static string[] SplitWords(string name) =>
        Regex.Split(name, "(?<=[a-z0-9])(?=[A-Z])");
}

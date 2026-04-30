using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace TokenQ;

public sealed record GeneratedFile(string Filename, string Content);

public sealed class InvalidNameException(string message) : Exception(message);

public sealed class Generator(ILogger<Generator>? logger = null)
{
    private readonly ILogger _logger = logger ?? NullLogger<Generator>.Instance;

    internal static readonly string[] FileTypes = ["Store", "Service"];

    private const string Template =
        "import { InjectionToken } from '@angular/core';\n" +
        "\n" +
        "export interface {INTERFACE} {\n" +
        "}\n" +
        "\n" +
        "export const {TOKEN} = new InjectionToken<{INTERFACE}>('{TOKEN}');\n";

    public GeneratedFile Render(string name)
    {
        if (NameValidator.Validate(name) is ValidationResult.Failed failed)
            throw new InvalidNameException(failed.Message);

        var pascal = ToPascalCase(name);
        var bare = StripLeadingI(pascal);
        var words = SplitWords(bare);

        string? fileType = null;
        var baseWords = words;
        if (words.Length >= 2 && FileTypes.Contains(words[^1]))
        {
            fileType = words[^1];
            baseWords = words[..^1];
        }

        var iface = StartsWithIUpper(pascal) ? pascal : "I" + pascal;
        var token = string.Join('_', words).ToUpperInvariant();
        var filename = fileType is null
            ? string.Join('-', words).ToLowerInvariant() + ".contract.ts"
            : string.Join('-', baseWords).ToLowerInvariant() + "." + fileType.ToLowerInvariant() + ".contract.ts";

        _logger.LogDebug(
            "Rendering interface={Interface} bare={Bare} kebab={Kebab} screaming={Screaming}",
            iface, bare, filename, token);

        var content = Template
            .Replace("{INTERFACE}", iface)
            .Replace("{TOKEN}", token);

        return new GeneratedFile(filename, content);
    }

    private static bool StartsWithIUpper(string s) =>
        s.Length >= 2 && s[0] == 'I' && s[1] is >= 'A' and <= 'Z';

    private static string StripLeadingI(string name) =>
        StartsWithIUpper(name) ? name[1..] : name;

    internal static string[] SplitWords(string name) =>
        Regex.Split(name, "(?<=[a-z0-9])(?=[A-Z])");

    internal static string ToPascalCase(string name)
    {
        if (!name.Contains('-'))
            return char.ToUpperInvariant(name[0]) + name[1..];

        var sb = new StringBuilder(name.Length);
        foreach (var seg in name.Split('-'))
        {
            if (seg.Length == 0) continue;
            sb.Append(char.ToUpperInvariant(seg[0]));
            sb.Append(seg[1..].ToLowerInvariant());
        }
        return sb.ToString();
    }
}

using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace TokenQ;

public sealed class BarrelGenerator(ILogger<BarrelGenerator>? logger = null)
{
    private readonly ILogger _logger = logger ?? NullLogger<BarrelGenerator>.Instance;

    private static readonly Regex FolderRegex =
        new(@"^[a-z][a-z0-9]*(-[a-z0-9]+)*$", RegexOptions.Compiled);

    private sealed record Contract(string FileName, string ImportPath, string TokenName, string InterfaceName, string CommonBase);
    private sealed record Implementation(string FileName, string ImportPath, string ClassName, string CommonBase);

    public GeneratedFile Render(string folderName, IReadOnlyList<string> fileNames)
    {
        var folder = folderName ?? string.Empty;
        if (!FolderRegex.IsMatch(folder.ToLowerInvariant()))
            throw new InvalidFolderNameException(folder);

        var contracts = new List<Contract>();
        var impls = new List<Implementation>();
        var models = new List<string>();

        foreach (var name in fileNames)
        {
            if (name.EndsWith(".store.contract.ts", StringComparison.Ordinal) ||
                name.EndsWith(".service.contract.ts", StringComparison.Ordinal) ||
                name.EndsWith(".contract.ts", StringComparison.Ordinal))
                contracts.Add(MakeContract(name));
            else if (name.EndsWith(".store.ts", StringComparison.Ordinal))
                impls.Add(MakeImpl(name, ".store.ts", "Store"));
            else if (name.EndsWith(".service.ts", StringComparison.Ordinal))
                impls.Add(MakeImpl(name, ".service.ts", "Service"));
            else if (name.EndsWith(".model.ts", StringComparison.Ordinal))
                models.Add(name);
        }

        contracts = [.. contracts.OrderBy(c => c.FileName, StringComparer.Ordinal)];
        impls = [.. impls.OrderBy(i => i.FileName, StringComparer.Ordinal)];
        models = [.. models.OrderBy(m => m, StringComparer.Ordinal)];

        var implByBase = new Dictionary<string, Implementation>(StringComparer.Ordinal);
        foreach (var impl in impls)
        {
            if (!implByBase.TryAdd(impl.CommonBase, impl))
                _logger.LogWarning("Duplicate implementation base: {File}", impl.FileName);
        }

        var pairs = new List<(Contract Contract, Implementation Implementation)>();
        var pairedImpls = new HashSet<string>(StringComparer.Ordinal);
        foreach (var c in contracts)
        {
            if (implByBase.TryGetValue(c.CommonBase, out var impl))
            {
                pairs.Add((c, impl));
                pairedImpls.Add(impl.FileName);
            }
            else
            {
                _logger.LogWarning("Unpaired contract token: {Token}", c.TokenName);
            }
        }

        foreach (var impl in impls)
            if (!pairedImpls.Contains(impl.FileName))
                _logger.LogWarning("Unpaired implementation class: {Class}", impl.ClassName);

        var sb = new StringBuilder();
        sb.Append("import { Provider } from '@angular/core';\n\n");

        var hasReexports = impls.Count + contracts.Count + models.Count > 0;

        foreach (var impl in impls)
            sb.Append($"export {{ {impl.ClassName} }} from '{impl.ImportPath}';\n");
        foreach (var c in contracts)
            sb.Append($"export {{ {c.TokenName} }} from '{c.ImportPath}';\n");
        foreach (var c in contracts)
            sb.Append($"export type {{ {c.InterfaceName} }} from '{c.ImportPath}';\n");
        foreach (var m in models)
            sb.Append($"export type * from './{m[..^3]}';\n");

        if (hasReexports) sb.Append('\n');

        var fnName = "provide" + WordsToPascal(folder.Split('-'));
        sb.Append($"export function {fnName}(): Provider[] {{\n");

        var bodyLines = new List<string>();
        foreach (var impl in impls.OrderBy(i => i.ClassName, StringComparer.Ordinal))
            bodyLines.Add(impl.ClassName);
        foreach (var p in pairs.OrderBy(p => p.Contract.TokenName, StringComparer.Ordinal))
            bodyLines.Add($"{{ provide: {p.Contract.TokenName}, useExisting: {p.Implementation.ClassName} }}");

        if (bodyLines.Count == 0)
        {
            sb.Append("  return [];\n");
        }
        else
        {
            sb.Append("  return [\n");
            for (int i = 0; i < bodyLines.Count; i++)
            {
                sb.Append("    ").Append(bodyLines[i]);
                if (i < bodyLines.Count - 1) sb.Append(',');
                sb.Append('\n');
            }
            sb.Append("  ];\n");
        }
        sb.Append("}\n");

        return new GeneratedFile("index.ts", sb.ToString());
    }

    private static Contract MakeContract(string fileName)
    {
        const string suffix = ".contract.ts";
        var baseSeg = fileName[..^suffix.Length];
        var words = baseSeg.Split('.', '-');
        var pascal = WordsToPascal(words);
        var interfaceName = StartsWithIUpper(pascal) ? pascal : "I" + pascal;

        var wordsBare = words.Length > 0 && words[0].Equals("I", StringComparison.OrdinalIgnoreCase)
            ? words[1..]
            : words;
        var tokenName = string.Join('_', wordsBare).ToUpperInvariant();

        var lastLower = words.Length > 0 ? words[^1].ToLowerInvariant() : "";
        var commonBase = (lastLower == "store" || lastLower == "service") && words.Length >= 2
            ? string.Join('-', words[..^1]).ToLowerInvariant()
            : string.Join('-', words).ToLowerInvariant();

        return new Contract(fileName, "./" + fileName[..^3], tokenName, interfaceName, commonBase);
    }

    private static Implementation MakeImpl(string fileName, string suffix, string typePascal)
    {
        var baseSeg = fileName[..^suffix.Length];
        var words = baseSeg.Split('.', '-');
        var className = WordsToPascal(words) + typePascal;
        var commonBase = string.Join('-', words).ToLowerInvariant();
        return new Implementation(fileName, "./" + fileName[..^3], className, commonBase);
    }

    private static string WordsToPascal(string[] words) =>
        string.Concat(words.Select(WordToPascal));

    private static string WordToPascal(string word) =>
        word.Length == 0 ? word : char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant();

    private static bool StartsWithIUpper(string s) =>
        s.Length >= 2 && s[0] == 'I' && s[1] is >= 'A' and <= 'Z';
}

public sealed class InvalidFolderNameException(string folderName)
    : Exception($"Folder name '{folderName}' is not a valid kebab-case identifier.");

public sealed class TargetDirectoryNotFoundException(string path)
    : IOException($"Target directory not found: {path}");

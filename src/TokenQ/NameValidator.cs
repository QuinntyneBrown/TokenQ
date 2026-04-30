using System.Text.RegularExpressions;

namespace TokenQ;

internal abstract record ValidationResult
{
    public sealed record Ok : ValidationResult;
    public sealed record Failed(string Message) : ValidationResult;
}

internal static class NameValidator
{
    private static readonly Regex Identifier =
        new(@"^[A-Za-z_$][A-Za-z0-9_$]*$", RegexOptions.Compiled);

    private static readonly HashSet<string> Reserved = new(StringComparer.Ordinal)
    {
        "break", "case", "catch", "class", "const", "continue", "debugger", "default",
        "delete", "do", "else", "enum", "export", "extends", "false", "finally", "for",
        "function", "if", "import", "in", "instanceof", "new", "null", "return", "super",
        "switch", "this", "throw", "true", "try", "typeof", "var", "void", "while", "with",
        "as", "implements", "interface", "let", "package", "private", "protected", "public",
        "static", "yield"
    };

    public static ValidationResult Validate(string? name)
    {
        if (string.IsNullOrEmpty(name))
            return new ValidationResult.Failed("name must not be empty");
        if (name.Length > 200)
            return new ValidationResult.Failed("name must be 200 characters or fewer");
        if (name.Any(char.IsWhiteSpace))
            return new ValidationResult.Failed("name must not contain whitespace");
        if (!Identifier.IsMatch(name))
            return new ValidationResult.Failed($"'{name}' is not a valid TypeScript identifier");
        if (Reserved.Contains(name))
            return new ValidationResult.Failed($"'{name}' is a TypeScript reserved word");
        return new ValidationResult.Ok();
    }
}

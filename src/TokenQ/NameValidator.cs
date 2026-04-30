namespace TokenQ;

internal abstract record ValidationResult
{
    public sealed record Ok : ValidationResult;
    public sealed record Failed(string Message) : ValidationResult;
}

internal static class NameValidator
{
    public static ValidationResult Validate(string? name) => new ValidationResult.Ok();
}

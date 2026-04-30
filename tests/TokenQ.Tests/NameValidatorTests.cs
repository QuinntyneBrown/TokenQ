// Acceptance Test
// Traces to: L2-007
// Description: NameValidator rejects every class of invalid TypeScript identifier

using Xunit;

namespace TokenQ.Tests;

public class NameValidatorTests
{
    [Fact] public void Validate_RejectsLeadingDigit() => AssertFails("1Foo");
    [Fact] public void Validate_RejectsWhitespace()    => AssertFails("Foo Service");
    [Fact] public void Validate_RejectsReservedWord()  => AssertFails("interface");
    [Fact] public void Validate_RejectsEmpty()         => AssertFails("");
    [Fact] public void Validate_RejectsTooLong()       => AssertFails(new string('A', 201));

    private static void AssertFails(string name)
    {
        var result = NameValidator.Validate(name);
        Assert.IsType<ValidationResult.Failed>(result);
    }
}

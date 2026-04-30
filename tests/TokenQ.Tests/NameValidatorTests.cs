// Acceptance Test
// Traces to: L2-007, L2-017
// Description: NameValidator rejects every class of invalid input under the slice-06 ruleset

using Xunit;

namespace TokenQ.Tests;

public class NameValidatorTests
{
    [Fact] public void Validate_RejectsLeadingDigit()        => AssertFails("1Foo");
    [Fact] public void Validate_RejectsWhitespace()          => AssertFails("Foo Service");
    [Fact] public void Validate_RejectsReservedWord()        => AssertFails("interface");
    [Fact] public void Validate_RejectsEmpty()               => AssertFails("");
    [Fact] public void Validate_RejectsTooLong()             => AssertFails(new string('A', 201));
    [Fact] public void Validate_RejectsUnderscore()          => AssertFails("event_store");          // L2-007 #6
    [Fact] public void Validate_RejectsLeadingDash()         => AssertFails("-leading-dash");        // L2-007 #7
    [Fact] public void Validate_RejectsTrailingDash()        => AssertFails("trailing-dash-");       // L2-007 #7
    [Fact] public void Validate_RejectsConsecutiveDashes()   => AssertFails("double--dash");         // L2-007 #8
    [Fact] public void Validate_RejectsBareFileTypeStore()   => AssertFails("Store");                // L2-017 #5
    [Fact] public void Validate_RejectsBareFileTypeService() => AssertFails("Service");              // L2-017 #5

    private static void AssertFails(string name)
    {
        var result = NameValidator.Validate(name);
        Assert.IsType<ValidationResult.Failed>(result);
    }
}

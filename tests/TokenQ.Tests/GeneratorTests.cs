// Acceptance Test
// Traces to: L2-001, L2-002, L2-005, L2-009, L2-015, L2-017, L2-018
// Description: Generator normalises name + derives I-prefixed interface, screaming token, contract filename

using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace TokenQ.Tests;

public class GeneratorTests
{
    private static GeneratedFile Render(string name) => new Generator().Render(name);

    [Fact] // L2-001 #1, L2-002 #1, L2-005 #1, L2-017 #1, L2-018 #1
    public void Render_EventStore_DerivesIPrefixedInterfaceAndStoreFilename()
    {
        var f = Render("EventStore");

        Assert.Equal("event.store.contract.ts", f.Filename);
        Assert.Contains("export interface IEventStore {", f.Content);
        Assert.Contains("export const EVENT_STORE = new InjectionToken<IEventStore>('EVENT_STORE');", f.Content);
    }

    [Fact] // L2-001 #2, L2-002 #2, L2-005 #2, L2-017 #2, L2-018 #2
    public void Render_CommandService_NormalisesCamelCaseAndPromotesServiceSegment()
    {
        var f = Render("commandService");

        Assert.Equal("command.service.contract.ts", f.Filename);
        Assert.Contains("export interface ICommandService {", f.Content);
        Assert.Contains("export const COMMAND_SERVICE = new InjectionToken<ICommandService>('COMMAND_SERVICE');", f.Content);
    }

    [Fact] // L2-001 #3, L2-002 #3, L2-005 #3, L2-018 #3
    public void Render_DataModeController_NormalisesKebabAndOmitsTypeSegment()
    {
        var f = Render("data-mode-controller");

        Assert.Equal("data-mode-controller.contract.ts", f.Filename);
        Assert.Contains("export interface IDataModeController {", f.Content);
        Assert.Contains("DATA_MODE_CONTROLLER", f.Content);
    }

    [Fact] // L2-001 #4, L2-005 #5
    public void Render_AlreadyIPrefixed_DoesNotDoubleI()
    {
        var f = Render("IFooService");

        Assert.Contains("export interface IFooService {", f.Content);
        Assert.DoesNotContain("IIFooService", f.Content);
        Assert.Equal("foo.service.contract.ts", f.Filename);
    }

    [Fact] // L2-005 #4
    public void Render_UnrecognisedTrailingWord_UsesFlatContractFilename()
    {
        Assert.Equal("user-account-manager.contract.ts", Render("UserAccountManager").Filename);
        Assert.Equal("foo-controller.contract.ts", Render("FooController").Filename);
        Assert.Equal("logger.contract.ts", Render("Logger").Filename);
    }

    [Fact] // L2-009 #1
    public void Render_TwoCallsProduceIdenticalBytes()
    {
        var a = Render("EventStore");
        var b = Render("EventStore");

        Assert.Equal(a.Filename, b.Filename);
        Assert.Equal(a.Content, b.Content);
    }

    [Fact] // L2-015 #1
    public void Generator_CanBeResolvedFromServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTransient<Generator>();
        using var provider = services.BuildServiceProvider();

        var generator = provider.GetRequiredService<Generator>();
        var f = generator.Render("EventStore");

        Assert.Equal("event.store.contract.ts", f.Filename);
    }

    [Fact] // L2-018 #4
    public void ToPascalCase_IsDeterministic()
    {
        var a = Generator.ToPascalCase("data-mode-controller");
        var b = Generator.ToPascalCase("data-mode-controller");

        Assert.Equal(a, b);
        Assert.Equal("DataModeController", a);
    }

    [Fact] // L2-018 #6
    public void ToPascalCase_HandlesMixedCaseKebab()
    {
        Assert.Equal("DataModeController", Generator.ToPascalCase("Data-Mode-Controller"));
    }
}

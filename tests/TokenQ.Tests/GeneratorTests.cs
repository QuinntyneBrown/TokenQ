// Acceptance Test
// Traces to: L2-001, L2-002, L2-005, L2-009, L2-015
// Description: Generator.Render returns deterministic filename + TypeScript content

using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace TokenQ.Tests;

public class GeneratorTests
{
    private static GeneratedFile Render(string name) => new Generator().Render(name);

    [Fact] // L2-005 #1
    public void Render_IFooService_ProducesExpectedFilename()
    {
        Assert.Equal("foo-service.ts", Render("IFooService").Filename);
    }

    [Fact] // L2-001 #1, L2-002 #1, L2-009 #3
    public void Render_IFooService_ProducesExpectedContent()
    {
        const string expected =
            "import { InjectionToken } from '@angular/core';\n" +
            "\n" +
            "export interface IFooService {\n" +
            "}\n" +
            "\n" +
            "export const FOO_SERVICE = new InjectionToken<IFooService>('FOO_SERVICE');\n";

        Assert.Equal(expected, Render("IFooService").Content);
    }

    [Fact] // L2-001 #2, L2-002 #3, L2-005 #3
    public void Render_PreservesNameWithoutLeadingI()
    {
        var file = Render("Logger");

        Assert.Equal("logger.ts", file.Filename);
        Assert.Contains("export interface Logger {", file.Content);
        Assert.Contains("export const LOGGER = new InjectionToken<Logger>('LOGGER');", file.Content);
    }

    [Fact] // L2-002 #2, L2-005 #2
    public void Render_UserAccountManager_KebabAndScreamingForms()
    {
        var file = Render("IUserAccountManager");

        Assert.Equal("user-account-manager.ts", file.Filename);
        Assert.Contains("export const USER_ACCOUNT_MANAGER = new InjectionToken<IUserAccountManager>('USER_ACCOUNT_MANAGER');", file.Content);
    }

    [Fact] // L2-009 #1
    public void Render_TwoCallsProduceIdenticalBytes()
    {
        var a = Render("IFooService");
        var b = Render("IFooService");

        Assert.Equal(a.Filename, b.Filename);
        Assert.Equal(a.Content, b.Content);
    }

    [Fact] // L2-015 #1
    public void Generator_CanBeResolvedFromServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddTransient<Generator>();
        using var provider = services.BuildServiceProvider();

        var generator = provider.GetRequiredService<Generator>();
        var file = generator.Render("IFooService");

        Assert.Equal("foo-service.ts", file.Filename);
    }
}

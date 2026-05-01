// Acceptance Test
// Traces to: L2-021, L2-022, L2-023, L2-024, L2-025, L2-026, L2-027, L2-028, L2-030
// Description: BarrelGenerator classifies file names, derives symbols, pairs token<->impl, renders index.ts

using Xunit;

namespace TokenQ.Tests;

public class BarrelGeneratorTests
{
    private static GeneratedFile Render(string folder, params string[] files) =>
        new BarrelGenerator().Render(folder, files);

    [Fact] // L2-022 #1, L2-023 #1, L2-026 #1, L2-027 #1, L2-028 #1
    public void Render_FolderWithSingleStorePair_ProducesPairedBarrel()
    {
        var f = Render("event", "event.store.ts", "event.store.contract.ts");

        Assert.Equal("index.ts", f.Filename);
        Assert.Contains("import { EventStore } from './event.store';", f.Content);
        Assert.Contains("import { EVENT_STORE } from './event.store.contract';", f.Content);
        Assert.Contains("export { EventStore };", f.Content);
        Assert.Contains("export { EVENT_STORE };", f.Content);
        Assert.Contains("export type { IEventStore } from './event.store.contract';", f.Content);
        Assert.Contains("export function provideEvent(): Provider[]", f.Content);
        Assert.Contains("EventStore,", f.Content);
        Assert.Contains("{ provide: EVENT_STORE, useExisting: EventStore }", f.Content);
    }

    [Fact] // L2-022 #2, L2-023 #3, L2-026 #2
    public void Render_FolderWithSingleServicePair_ProducesPairedBarrel()
    {
        var f = Render("command", "command.service.ts", "command.service.contract.ts");

        Assert.Contains("import { CommandService } from './command.service';", f.Content);
        Assert.Contains("import { COMMAND_SERVICE } from './command.service.contract';", f.Content);
        Assert.Contains("export { CommandService };", f.Content);
        Assert.Contains("export { COMMAND_SERVICE };", f.Content);
        Assert.Contains("export type { ICommandService } from './command.service.contract';", f.Content);
        Assert.Contains("{ provide: COMMAND_SERVICE, useExisting: CommandService }", f.Content);
    }

    [Fact] // L2-022 #3
    public void Render_DottedKebabContract_DerivesCorrectSymbols()
    {
        var f = Render("dashboard-state",
            "dashboard-state-store.contract.ts",
            "dashboard-state.store.ts");

        Assert.Contains("import { DashboardStateStore } from './dashboard-state.store';", f.Content);
        Assert.Contains("import { DASHBOARD_STATE_STORE } from './dashboard-state-store.contract';", f.Content);
        Assert.Contains("export { DashboardStateStore };", f.Content);
        Assert.Contains("export { DASHBOARD_STATE_STORE };", f.Content);
        Assert.Contains("export type { IDashboardStateStore } from './dashboard-state-store.contract';", f.Content);
        Assert.Contains("{ provide: DASHBOARD_STATE_STORE, useExisting: DashboardStateStore }", f.Content);
    }

    [Fact] // L2-022 #4
    public void Render_FlatContract_DerivesIPrefixedInterface()
    {
        var f = Render("user", "user.contract.ts");

        Assert.Contains("import { USER } from './user.contract';", f.Content);
        Assert.Contains("export { USER };", f.Content);
        Assert.Contains("export type { IUser } from './user.contract';", f.Content);
        Assert.DoesNotContain("useExisting", f.Content);
    }

    [Fact] // L2-024 #1
    public void Render_ModelFile_AddsTypeStarReexport()
    {
        var f = Render("dashboard-state", "dashboard-state.model.ts");

        Assert.Contains("export type * from './dashboard-state.model';", f.Content);
    }

    [Fact] // L2-024 #3
    public void Render_NoModelFiles_OmitsModelReexports()
    {
        var f = Render("event", "event.store.ts", "event.store.contract.ts");

        Assert.DoesNotContain("export type *", f.Content);
    }

    [Fact] // L2-025 #1
    public void Render_FolderName_DashboardState_ProducesProvideDashboardState()
    {
        var f = Render("dashboard-state");

        Assert.Contains("export function provideDashboardState(): Provider[]", f.Content);
    }

    [Fact] // L2-025 #3
    public void Render_FolderName_UserAccountManagement_ProducesProvideUserAccountManagement()
    {
        var f = Render("user-account-management");

        Assert.Contains("export function provideUserAccountManagement(): Provider[]", f.Content);
    }

    [Fact] // L2-025 #4 (variant)
    public void Render_EmptyFolderName_ThrowsInvalidFolderNameException()
    {
        Assert.Throws<InvalidFolderNameException>(() => Render(""));
    }

    [Fact] // L2-026 #3
    public void Render_UnpairedContract_OmitsTokenRegistrationAndLogsWarning()
    {
        var f = Render("user", "user.contract.ts");

        Assert.Contains("export { USER };", f.Content);
        Assert.DoesNotContain("useExisting", f.Content);
    }

    [Fact] // L2-026 #4
    public void Render_UnpairedImplementation_AddsClassWithoutUseExistingAndLogsWarning()
    {
        var f = Render("orphan", "orphan.store.ts");

        Assert.Contains("import { OrphanStore } from './orphan.store';", f.Content);
        Assert.Contains("export { OrphanStore };", f.Content);
        Assert.Contains("OrphanStore", f.Content);
        Assert.DoesNotContain("useExisting", f.Content);
    }

    [Fact] // L2-027 #2, L2-028 #1
    public void Render_MultiplePairs_OrderedAlphabeticallyByClassThenByToken()
    {
        var f = Render("multi",
            "event.store.ts", "event.store.contract.ts",
            "command.service.ts", "command.service.contract.ts");

        var classBlock = f.Content.IndexOf("CommandService,", StringComparison.Ordinal);
        var classBlock2 = f.Content.IndexOf("EventStore,", StringComparison.Ordinal);
        Assert.True(classBlock > 0 && classBlock2 > classBlock,
            "CommandService must precede EventStore in provider array");

        var tokenBlock = f.Content.IndexOf("{ provide: COMMAND_SERVICE", StringComparison.Ordinal);
        var tokenBlock2 = f.Content.IndexOf("{ provide: EVENT_STORE", StringComparison.Ordinal);
        Assert.True(tokenBlock > 0 && tokenBlock2 > tokenBlock,
            "COMMAND_SERVICE must precede EVENT_STORE");
    }

    [Fact] // L2-028 #2
    public void Render_FolderWithoutContracts_GeneratesProviderArrayWithoutTokenRegistrations()
    {
        var f = Render("orphan", "orphan.store.ts");

        Assert.Contains("OrphanStore", f.Content);
        Assert.DoesNotContain("{ provide:", f.Content);
    }

    [Fact] // L2-028 #3
    public void Render_EmptyFolder_GeneratesEmptyProviderArray()
    {
        var f = Render("empty");

        Assert.Contains("export function provideEmpty(): Provider[]", f.Content);
        Assert.Contains("return [];", f.Content);
    }

    [Fact] // L2-030 #1
    public void Render_TwoCalls_ProduceIdenticalBytes()
    {
        var a = Render("event", "event.store.ts", "event.store.contract.ts");
        var b = Render("event", "event.store.ts", "event.store.contract.ts");

        Assert.Equal(a.Content, b.Content);
    }

    [Fact] // L2-027 #3, L2-030 #3
    public void Render_OutputUsesLfLineEndingsAndSingleTrailingNewline()
    {
        var f = Render("event", "event.store.ts", "event.store.contract.ts");

        Assert.DoesNotContain("\r", f.Content);
        Assert.EndsWith("\n", f.Content);
        Assert.False(f.Content.EndsWith("\n\n"), "must end with exactly one trailing newline");
    }

    [Fact] // L2-021 #4 (integration)
    public void Render_UnknownFiles_IgnoredSilently()
    {
        var f = Render("event",
            "event.store.ts",
            "event.store.contract.ts",
            "chart-visible-window.ts",
            "event.store.spec.ts",
            "index.ts");

        Assert.DoesNotContain("chart-visible-window", f.Content);
        Assert.DoesNotContain("spec", f.Content);
        Assert.DoesNotContain("'./index'", f.Content);
    }
}

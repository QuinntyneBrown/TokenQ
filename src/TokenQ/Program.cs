using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace TokenQ;

public static class Program
{
    public static int Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddSingleton<Generator>();
        services.AddSingleton<FileWriter>();
        using var provider = services.BuildServiceProvider();
        return BuildRootCommand(provider).InvokeAsync(args).GetAwaiter().GetResult();
    }

    private static RootCommand BuildRootCommand(IServiceProvider provider)
    {
        var nameOption = new Option<string>("--name", "TypeScript interface name") { IsRequired = true };
        nameOption.AddAlias("-n");
        var outputOption = new Option<string?>("--output", "Output directory");
        outputOption.AddAlias("-o");
        var forceOption = new Option<bool>("--force", "Overwrite existing file");
        forceOption.AddAlias("-f");
        var verboseOption = new Option<bool>("--verbose", "Enable debug logging");
        verboseOption.AddAlias("-v");

        var root = new RootCommand("Generate a TypeScript interface and Angular InjectionToken.")
        {
            nameOption, outputOption, forceOption, verboseOption,
        };

        root.SetHandler(context =>
        {
            var name = context.ParseResult.GetValueForOption(nameOption)!;
            var output = context.ParseResult.GetValueForOption(outputOption);
            var force = context.ParseResult.GetValueForOption(forceOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);
            try
            {
                var generated = provider.GetRequiredService<Generator>().Render(name);
                provider.GetRequiredService<FileWriter>().Write(output, generated, force);
            }
            catch (InvalidNameException ex)
            {
                Console.Error.WriteLine(ex.Message);
                context.ExitCode = 1;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Console.Error.WriteLine(ex.Message);
                context.ExitCode = 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(verbose ? ex.ToString() : ex.Message);
                context.ExitCode = 2;
            }
        });

        return root;
    }
}

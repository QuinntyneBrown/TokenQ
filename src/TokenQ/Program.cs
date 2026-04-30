using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace TokenQ;

public static class Program
{
    public static int Main(string[] args)
    {
        var verbose = args.Any(arg => arg is "--verbose" or "-v");
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.SetMinimumLevel(verbose ? LogLevel.Debug : LogLevel.Information);
            builder.AddSimpleConsole(options =>
            {
                options.ColorBehavior = LoggerColorBehavior.Disabled;
                options.IncludeScopes = false;
                options.SingleLine = true;
            });
            builder.Services.Configure<ConsoleLoggerOptions>(options =>
                options.LogToStandardErrorThreshold = LogLevel.Error);
        });
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
            var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("TokenQ");
            try
            {
                var generated = provider.GetRequiredService<Generator>().Render(name);
                var written = provider.GetRequiredService<FileWriter>().Write(output, generated, force);
                logger.LogInformation("Wrote {Path}", written);
            }
            catch (InvalidNameException ex)
            {
                LogFailure(logger, ex, verbose);
                context.ExitCode = 1;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                LogFailure(logger, ex, verbose);
                context.ExitCode = 1;
            }
            catch (Exception ex)
            {
                LogFailure(logger, ex, verbose);
                context.ExitCode = 2;
            }
        });

        return root;
    }

    private static void LogFailure(ILogger logger, Exception ex, bool verbose)
    {
        if (verbose)
            logger.LogError(ex, "{Message}", ex.Message);
        else
            logger.LogError("{Message}", ex.Message);
    }
}

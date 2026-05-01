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
        services.AddSingleton<BarrelGenerator>();
        using var provider = services.BuildServiceProvider();
        var root = BuildRootCommand(provider);
        root.AddCommand(BuildProvideCommand(provider));
        return root.InvokeAsync(args).GetAwaiter().GetResult();
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
            catch (Exception ex) when (ex is InvalidNameException or IOException or UnauthorizedAccessException)
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

    private static Command BuildProvideCommand(IServiceProvider provider)
    {
        var pathOption = new Option<string?>("--path", "Target directory");
        pathOption.AddAlias("-p");
        var forceOption = new Option<bool>("--force", "Overwrite existing index.ts");
        forceOption.AddAlias("-f");
        var verboseOption = new Option<bool>("--verbose", "Enable debug logging");
        verboseOption.AddAlias("-v");

        var cmd = new Command("provide", "Generate an Angular barrel index.ts for a folder")
        {
            pathOption, forceOption, verboseOption,
        };

        cmd.SetHandler(context =>
        {
            var path = context.ParseResult.GetValueForOption(pathOption);
            var force = context.ParseResult.GetValueForOption(forceOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);
            var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("TokenQ");
            try
            {
                var (target, folderName, fileNames) = ResolveAndScan(path);
                var generated = provider.GetRequiredService<BarrelGenerator>().Render(folderName, fileNames);
                var written = provider.GetRequiredService<FileWriter>().Write(target, generated, force);
                logger.LogInformation("Wrote {Path}", written);
            }
            catch (Exception ex) when (ex is InvalidFolderNameException or IOException or UnauthorizedAccessException)
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

        return cmd;
    }

    private static (string Target, string FolderName, IReadOnlyList<string> FileNames)
        ResolveAndScan(string? pathOption)
    {
        string target;
        try
        {
            target = string.IsNullOrEmpty(pathOption)
                ? Directory.GetCurrentDirectory()
                : Path.GetFullPath(pathOption);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        {
            throw new InvalidOutputPathException(pathOption ?? string.Empty, ex);
        }

        if (System.IO.File.Exists(target) && !Directory.Exists(target))
            throw new OutputPathIsFileException(target);
        if (!Directory.Exists(target))
            throw new TargetDirectoryNotFoundException(target);

        var folderName = Path.GetFileName(target.TrimEnd(
            Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrEmpty(folderName))
            throw new InvalidFolderNameException(target);

        var fileNames = Directory
            .EnumerateFiles(target, "*", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(n => !string.IsNullOrEmpty(n))
            .Cast<string>()
            .ToArray();

        return (target, folderName, fileNames);
    }

    private static void LogFailure(ILogger logger, Exception ex, bool verbose)
    {
        if (verbose)
            logger.LogError(ex, "{Message}", ex.Message);
        else
            logger.LogError("{Message}", ex.Message);
    }
}

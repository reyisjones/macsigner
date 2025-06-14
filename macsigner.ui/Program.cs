using Avalonia;
using MacSigner.Commands;
using System;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;

namespace MacSigner;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static async Task<int> Main(string[] args)
    {
        try
        {
            // Check if running in CLI mode
            if (args.Length > 0 && !args.Contains("--avalonia-args"))
            {
                return await RunCliMode(args);
            }

            // Run GUI mode
            var app = BuildAvaloniaApp();
            return app.StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Application failed to start: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return -1;
        }
    }

    private static async Task<int> RunCliMode(string[] args)
    {
        var rootCommand = new RootCommand("MacSigner - Digital Code Signing Tool")
        {
            new SignCommand()
        };

        var versionCommand = new Command("version", "Show version information");
        versionCommand.SetHandler(() =>
        {
            Console.WriteLine("MacSigner v1.0.0");
            Console.WriteLine("Digital Code Signing Tool for macOS");
            Console.WriteLine("Uses Azure Trusted Signing service");
        });
        
        rootCommand.AddCommand(versionCommand);

        return await rootCommand.InvokeAsync(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}

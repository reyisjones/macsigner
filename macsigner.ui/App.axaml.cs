using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MacSigner.Models;
using MacSigner.Services;
using MacSigner.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using System;
using System.Reactive.Concurrency;

namespace MacSigner;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        ConfigureServices();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            Console.WriteLine("OnFrameworkInitializationCompleted called");
            
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                Console.WriteLine("Desktop lifetime detected");
                
                try
                {
                    Console.WriteLine("Attempting to get MainWindowViewModel...");
                    Console.WriteLine("Getting service provider...");
                    var serviceProvider = _serviceProvider;
                    Console.WriteLine($"Service provider: {serviceProvider != null}");
                    
                    Console.WriteLine("Getting IFileService...");
                    var fileService = serviceProvider?.GetRequiredService<IFileService>();
                    Console.WriteLine($"FileService: {fileService != null}");
                    
                    Console.WriteLine("Getting ITrustedSigningService...");
                    var trustedSigningService = serviceProvider?.GetRequiredService<ITrustedSigningService>();
                    Console.WriteLine($"TrustedSigningService: {trustedSigningService != null}");
                    
                    Console.WriteLine("Getting ISettingsService...");
                    var settingsService = serviceProvider?.GetRequiredService<ISettingsService>();
                    Console.WriteLine($"SettingsService: {settingsService != null}");
                    
                    Console.WriteLine("Getting ILogger<MainWindowViewModel>...");
                    var logger = serviceProvider?.GetRequiredService<ILogger<MainWindowViewModel>>();
                    Console.WriteLine($"Logger: {logger != null}");
                    
                    Console.WriteLine("Creating MainWindowViewModel directly...");
                    var mainWindowViewModel = new MainWindowViewModel(fileService!, trustedSigningService!, settingsService!, logger!);
                    Console.WriteLine($"MainWindowViewModel created: {mainWindowViewModel != null}");
                    
                    Console.WriteLine("Creating MainWindow...");
                    var mainWindow = new MainWindow
                    {
                        DataContext = mainWindowViewModel
                    };
                    Console.WriteLine("MainWindow instance created");
                    
                    desktop.MainWindow = mainWindow;
                    Console.WriteLine("MainWindow assigned to desktop.MainWindow");
                    
                    // For macOS, we need to ensure the window is properly activated
                    mainWindow.Activate();
                    Console.WriteLine("MainWindow.Activate() called");
                }
                catch (Exception vmEx)
                {
                    Console.WriteLine($"Error creating MainWindow or ViewModel: {vmEx.Message}");
                    Console.WriteLine($"Stack trace: {vmEx.StackTrace}");
                    throw;
                }
            }
            else
            {
                Console.WriteLine($"Unexpected ApplicationLifetime type: {ApplicationLifetime?.GetType().Name ?? "null"}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in OnFrameworkInitializationCompleted: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }

        base.OnFrameworkInitializationCompleted();
        Console.WriteLine("OnFrameworkInitializationCompleted completed");
    }

    private void ConfigureServices()
    {
        Console.WriteLine("ConfigureServices called");
        var services = new ServiceCollection();

        // Configure logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        Console.WriteLine("Logging configured");

        // Register services
        services.AddHttpClient<AzureTrustedSigningService>();
        services.AddSingleton<ITrustedSigningService, AzureTrustedSigningService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IFileService, FileService>();
        Console.WriteLine("Services registered");

        // Register settings as lazy-loaded
        services.AddSingleton<AppSettings>(provider => new AppSettings());
        Console.WriteLine("Settings registered");

        // Register ViewModels
        services.AddTransient<MainWindowViewModel>();
        Console.WriteLine("ViewModels registered");

        _serviceProvider = services.BuildServiceProvider();
        Console.WriteLine("ServiceProvider built");
    }
}
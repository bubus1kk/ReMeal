using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using ReMeal.Application;
using ReMeal.Desktop.ViewModels;
using ReMeal.Infrastructure;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ReMeal.Desktop;

internal sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        var serviceProvider = services.BuildServiceProvider();

        Task.Run(() => serviceProvider.InitializeDatabaseAsync())
            .GetAwaiter()
            .GetResult();

        BuildAvaloniaApp(serviceProvider)
            .StartWithClassicDesktopLifetime(args);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ReMeal");

        Directory.CreateDirectory(appDataPath);

        var connectionString = $"Data Source={Path.Combine(appDataPath, "remeal.db")}";

        services.AddSingleton<NavigationState>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddApplication();
        services.AddInfrastructure(connectionString);
    }

    public static AppBuilder BuildAvaloniaApp(IServiceProvider serviceProvider)
        => AppBuilder.Configure(() => new App(serviceProvider))
            .UsePlatformDetect()
#if DEBUG
            .WithInterFont()
            .LogToTrace();
#else
            .WithInterFont()
            .LogToTrace();
#endif
}

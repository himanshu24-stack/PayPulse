using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PayPulse.Data;
using PayPulse.Services;
using PayPulse.ViewModels;

namespace PayPulse;

/// <summary>
/// Application entry point, Exception handling, and Dependency Injection configuration.
/// </summary>
public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        // Global Unhandled Exception Handlers to safeguard application stability
        DispatcherUnhandledException += (sender, args) =>
        {
            args.Handled = true;
            MessageBox.Show(
                $"A UI exception was caught and handled safely:\n\n{args.Exception.Message}",
                "PayPulse Notification",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        };

        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                File.AppendAllText("error_crash.log", $"[{DateTime.Now}] Domain Crash: {ex}\n");
            }
        };

        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            args.SetObserved();
        };

        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // EF Core SQLite DbContext with Factory
        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlite(AppDbContext.GetConnectionString()));

        services.AddTransient<AppDbContext>(sp =>
            sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());

        // Business Logic Services
        services.AddSingleton<InvoiceCalculationService>();
        services.AddSingleton<RiskAnalysisService>();
        services.AddSingleton<CsvExportService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<CustomersViewModel>();
        services.AddTransient<InvoicesViewModel>();
        services.AddTransient<PaymentsViewModel>();
        services.AddTransient<RiskAnalysisViewModel>();
        services.AddTransient<ReportsViewModel>();
        services.AddTransient<SettingsViewModel>();

        // Views / Windows
        services.AddTransient<MainWindow>();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            // Ensure database exists and seed initial demo data
            using (var scope = Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await db.Database.EnsureCreatedAsync();
                await DatabaseSeeder.SeedSampleDataAsync(db);
            }

            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error initializing application startup:\n\n{ex.Message}",
                "Startup Initialization Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DiarioLivre.Core.Interfaces;
using DiarioLivre.Core.Services;
using DiarioLivre.Desktop.Services;
using DiarioLivre.Desktop.ViewModels;
using DiarioLivre.Desktop.Views;
using DiarioLivre.Infrastructure;
using DiarioLivre.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace DiarioLivre.Desktop;

public partial class App : Application
{
    public IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        ServiceCollection serviceCollection = new();
        ConfigureServices(serviceCollection);
        Services = serviceCollection.BuildServiceProvider();

        Services.GetRequiredService<ThemeService>().Initialize();

        DatabaseInitializer initializer = Services.GetRequiredService<DatabaseInitializer>();
        Task.Run(async () => await initializer.InitializeAsync()).GetAwaiter().GetResult();

        UserPreferencesStore prefsStore = Services.GetRequiredService<UserPreferencesStore>();
        if (prefsStore.Load().AutoBackupOnStartup)
        {
            IBackupService backupService = Services.GetRequiredService<IBackupService>();
            _ = Task.Run(async () =>
            {
                try
                {
                    await backupService.CreateRollingAutoBackupAsync(maxSnapshots: 5);
                }
                catch
                {
                    // Silent background auto-backup should never interrupt startup.
                }
            });
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            MainWindowViewModel mainViewModel = Services.GetRequiredService<MainWindowViewModel>();
            var mainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };

            mainWindow.Opened += async (_, _) =>
            {
                try
                {
                    await mainViewModel.LoadAsync();
                }
                catch (Exception ex)
                {
                    Program.LogFatal("LoadAsync", ex);
                }
            };

            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        string appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DiarioLivre");

        string databasePath = Path.Combine(appDataDir, "diariolivre.db");
        string avatarsDirectory = Path.Combine(appDataDir, "avatars");

        services.AddDiarioLivreServices(databasePath, avatarsDirectory);

        services.AddSingleton<UserPreferencesStore>();
        services.AddSingleton<AppStateService>();
        services.AddSingleton<ThemeService>();
        services.AddSingleton<NavigationService>();
        services.AddSingleton<ToastService>();
        services.AddSingleton<StudentBatchParser>();

        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<GradebookViewModel>();
        services.AddSingleton<AttendanceViewModel>();
        services.AddSingleton<StudentDossierViewModel>();
        services.AddSingleton<PlannerViewModel>();
        services.AddSingleton<CalendarViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<MainWindowViewModel>();
    }
}

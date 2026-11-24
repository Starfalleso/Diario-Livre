using Avalonia;

namespace DiarioLivre.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            LogFatal("UnhandledException", e.ExceptionObject as Exception);

        TaskScheduler.UnobservedTaskException += (_, e) =>
            LogFatal("UnobservedTaskException", e.Exception);

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            LogFatal("MainFatal", ex);
            throw;
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    internal static void LogFatal(string context, Exception? ex)
    {
        if (ex is null)
        {
            return;
        }

        try
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DiarioLivre");
            Directory.CreateDirectory(dir);
            File.AppendAllText(
                Path.Combine(dir, "startup.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ERROR ({context}): {ex}{Environment.NewLine}");
        }
        catch
        {
            // Ignore logging failures during error reporting.
        }

        Console.Error.WriteLine($"[{context}] {ex}");
    }
}

using System.Text.Json;

namespace DiarioLivre.Desktop.Services;

public sealed record UserPreferencesData(
    bool IsDarkTheme = false,
    string CultureCode = "pt-BR",
    string? PinHash = null,
    bool AutoBackupOnStartup = true);

/// <summary>
/// Thread-safe persistence store for local user UI, language, auto-backup, and LGPD PIN preferences.
/// </summary>
public sealed class UserPreferencesStore
{
    private static readonly object SyncLock = new();
    private static readonly string PreferencesFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DiarioLivre",
        "ui-preferences.json");

    public event EventHandler? LockRequested;

    public event EventHandler? PinConfigurationChanged;

    public UserPreferencesData Load() => LoadStatic();

    public void Update(Func<UserPreferencesData, UserPreferencesData> updater)
    {
        ArgumentNullException.ThrowIfNull(updater);
        UserPreferencesData updated = UpdateStatic(updater);
        PinConfigurationChanged?.Invoke(this, EventArgs.Empty);
        _ = updated;
    }

    public void RequestLock() => LockRequested?.Invoke(this, EventArgs.Empty);

    public static UserPreferencesData LoadStatic()
    {
        lock (SyncLock)
        {
            try
            {
                if (!File.Exists(PreferencesFilePath))
                {
                    return new UserPreferencesData();
                }

                string json = File.ReadAllText(PreferencesFilePath);
                return JsonSerializer.Deserialize<UserPreferencesData>(json) ?? new UserPreferencesData();
            }
            catch
            {
                return new UserPreferencesData();
            }
        }
    }

    public static UserPreferencesData UpdateStatic(Func<UserPreferencesData, UserPreferencesData> updater)
    {
        lock (SyncLock)
        {
            UserPreferencesData current = LoadStatic();
            UserPreferencesData next = updater(current);
            try
            {
                string? dir = Path.GetDirectoryName(PreferencesFilePath);
                if (!string.IsNullOrWhiteSpace(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string json = JsonSerializer.Serialize(next, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(PreferencesFilePath, json);
            }
            catch
            {
                // Ignore non-critical UI preference write errors.
            }

            return next;
        }
    }
}

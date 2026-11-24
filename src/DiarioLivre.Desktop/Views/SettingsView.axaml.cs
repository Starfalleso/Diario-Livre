using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using DiarioLivre.Desktop.ViewModels;

namespace DiarioLivre.Desktop.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private async void OnExportBackupClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel vm || TopLevel.GetTopLevel(this) is not { } topLevel)
        {
            return;
        }

        IStorageFile? file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            DefaultExtension = "zip",
            SuggestedFileName = vm.GetSuggestedBackupFileName(),
            FileTypeChoices = new[]
            {
                new FilePickerFileType("ZIP Archive") { Patterns = new[] { "*.zip" } }
            }
        });

        string? localPath = file?.TryGetLocalPath();
        if (!string.IsNullOrWhiteSpace(localPath))
        {
            await vm.ExportBackupZipAsync(localPath);
        }
    }

    private async void OnRestoreBackupClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel vm || TopLevel.GetTopLevel(this) is not { } topLevel)
        {
            return;
        }

        IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("ZIP Archive") { Patterns = new[] { "*.zip" } }
            }
        });

        if (files.Count == 0)
        {
            return;
        }

        string? localPath = files[0].TryGetLocalPath();
        if (!string.IsNullOrWhiteSpace(localPath))
        {
            await vm.RestoreBackupZipAsync(localPath);
        }
    }
}

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using DiarioLivre.Desktop.ViewModels;

namespace DiarioLivre.Desktop.Views;

public partial class AttendanceView : UserControl
{
    public AttendanceView()
    {
        InitializeComponent();
    }

    private async void OnExportPdfClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not AttendanceViewModel vm || TopLevel.GetTopLevel(this) is not { } topLevel)
        {
            return;
        }

        IStorageFile? file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            DefaultExtension = "pdf",
            SuggestedFileName = vm.GetSuggestedPdfFileName(),
            FileTypeChoices = new[]
            {
                new FilePickerFileType("PDF Document") { Patterns = new[] { "*.pdf" } }
            }
        });

        string? localPath = file?.TryGetLocalPath();
        if (!string.IsNullOrWhiteSpace(localPath))
        {
            await vm.ExportAttendancePdfAsync(localPath);
        }
    }
}

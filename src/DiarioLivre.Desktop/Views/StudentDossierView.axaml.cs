using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using DiarioLivre.Desktop.ViewModels;

namespace DiarioLivre.Desktop.Views;

public partial class StudentDossierView : UserControl
{
    public StudentDossierView()
    {
        InitializeComponent();
    }

    private async void OnLoadBatchFileClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not StudentDossierViewModel vm || TopLevel.GetTopLevel(this) is not { } topLevel)
        {
            return;
        }

        IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Text / CSV") { Patterns = new[] { "*.txt", "*.csv" } }
            }
        });

        if (files.Count == 0)
        {
            return;
        }

        string? localPath = files[0].TryGetLocalPath();
        if (!string.IsNullOrWhiteSpace(localPath))
        {
            await vm.LoadBatchFromFileAsync(localPath);
        }
    }

    private async void OnChangePhotoClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not StudentDossierViewModel vm)
        {
            return;
        }

        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = new[] { FilePickerFileTypes.ImageAll }
        });

        if (files.Count == 0)
        {
            return;
        }

        string? localPath = files[0].TryGetLocalPath();
        if (!string.IsNullOrWhiteSpace(localPath))
        {
            await vm.UpdatePhotoFromFileAsync(localPath);
        }
    }

    private async void OnExportPdfClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not StudentDossierViewModel vm)
        {
            return;
        }

        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
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
            await vm.ExportPdfAsync(localPath);
        }
    }

    private async void OnExportCouncilPdfClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not StudentDossierViewModel vm || TopLevel.GetTopLevel(this) is not { } topLevel)
        {
            return;
        }

        IStorageFile? file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            DefaultExtension = "pdf",
            SuggestedFileName = vm.GetSuggestedCouncilPdfFileName(),
            FileTypeChoices = new[]
            {
                new FilePickerFileType("PDF Document") { Patterns = new[] { "*.pdf" } }
            }
        });

        string? localPath = file?.TryGetLocalPath();
        if (!string.IsNullOrWhiteSpace(localPath))
        {
            await vm.ExportClassCouncilPdfAsync(localPath);
        }
    }

    private async void OnExportBatchDossiersClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not StudentDossierViewModel vm || TopLevel.GetTopLevel(this) is not { } topLevel)
        {
            return;
        }

        IStorageFile? file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            DefaultExtension = "pdf",
            SuggestedFileName = vm.GetSuggestedBatchPdfFileName(),
            FileTypeChoices = new[]
            {
                new FilePickerFileType("PDF Document") { Patterns = new[] { "*.pdf" } }
            }
        });

        string? localPath = file?.TryGetLocalPath();
        if (!string.IsNullOrWhiteSpace(localPath))
        {
            await vm.ExportBatchReportCardsPdfAsync(localPath);
        }
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using DiarioLivre.Core.Entities;
using DiarioLivre.Desktop.Converters;
using DiarioLivre.Desktop.Localization;
using DiarioLivre.Desktop.ViewModels;

namespace DiarioLivre.Desktop.Views;

public partial class GradebookView : UserControl
{
    private GradebookViewModel? _subscribedVm;

    public GradebookView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        AttachedToVisualTree += OnAttachedToVisualTree;
        DetachedFromVisualTree += OnDetachedFromVisualTree;
    }

    private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        LocalizationService.Instance.CultureChanged += OnCultureChanged;
        if (Application.Current is not null)
        {
            Application.Current.ActualThemeVariantChanged += OnActualThemeVariantChanged;
        }
        RebuildColumns();
    }

    private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        LocalizationService.Instance.CultureChanged -= OnCultureChanged;
        if (Application.Current is not null)
        {
            Application.Current.ActualThemeVariantChanged -= OnActualThemeVariantChanged;
        }
    }

    private void OnActualThemeVariantChanged(object? sender, EventArgs e)
    {
        RebuildColumns();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_subscribedVm is not null)
        {
            _subscribedVm.AssessmentsReloaded -= OnAssessmentsReloaded;
        }

        _subscribedVm = DataContext as GradebookViewModel;
        if (_subscribedVm is not null)
        {
            _subscribedVm.AssessmentsReloaded += OnAssessmentsReloaded;
        }

        RebuildColumns();
    }

    private void OnCultureChanged(object? sender, EventArgs e)
    {
        RebuildColumns();
    }

    private void OnAssessmentsReloaded(object? sender, EventArgs e)
    {
        RebuildColumns();
    }

    private async void OnExportCsvClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not GradebookViewModel vm || TopLevel.GetTopLevel(this) is not { } topLevel)
        {
            return;
        }

        IStorageFile? file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            DefaultExtension = "csv",
            SuggestedFileName = vm.GetSuggestedExportFileName("csv"),
            FileTypeChoices = new[]
            {
                new FilePickerFileType("CSV") { Patterns = new[] { "*.csv" } }
            }
        });

        string? localPath = file?.TryGetLocalPath();
        if (!string.IsNullOrWhiteSpace(localPath))
        {
            await vm.ExportCsvAsync(localPath);
        }
    }

    private async void OnExportClassPdfClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not GradebookViewModel vm || TopLevel.GetTopLevel(this) is not { } topLevel)
        {
            return;
        }

        IStorageFile? file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            DefaultExtension = "pdf",
            SuggestedFileName = vm.GetSuggestedExportFileName("pdf"),
            FileTypeChoices = new[]
            {
                new FilePickerFileType("PDF Document") { Patterns = new[] { "*.pdf" } }
            }
        });

        string? localPath = file?.TryGetLocalPath();
        if (!string.IsNullOrWhiteSpace(localPath))
        {
            await vm.ExportClassPdfAsync(localPath);
        }
    }

    private void RebuildColumns()
    {
        if (GradesDataGrid.Columns.Count > 0)
        {
            GradesDataGrid.Columns[0].Header = LocalizationService.Instance.GetString("StudentColumnHeader");
        }

        while (GradesDataGrid.Columns.Count > 1)
        {
            GradesDataGrid.Columns.RemoveAt(1);
        }

        if (_subscribedVm is null)
        {
            return;
        }

        for (int i = 0; i < _subscribedVm.Assessments.Count; i++)
        {
            int cellIndex = i;
            Assessment assessment = _subscribedVm.Assessments[cellIndex];
            string header = assessment.IsRecovery
                ? $"{assessment.Title} (Rec / {assessment.MaxScore:0.#})"
                : $"{assessment.Title} ({assessment.MaxScore:0.#})";

            GradesDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = header,
                Width = new DataGridLength(145),
                Binding = new Binding($"Cells[{cellIndex}].ScoreText", BindingMode.TwoWay)
            });
        }

        GradesDataGrid.Columns.Add(new DataGridTemplateColumn
        {
            Header = LocalizationService.Instance.GetString("TermScoreColumnHeader"),
            Width = new DataGridLength(150),
            IsReadOnly = true,
            CellTemplate = new FuncDataTemplate<StudentGradeRowViewModel>((_, _) =>
            {
                var scoreText = new TextBlock
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = FontWeight.Bold,
                    FontSize = 13
                };
                scoreText.Bind(TextBlock.TextProperty, new Binding(nameof(StudentGradeRowViewModel.TermScoreText)));
                scoreText.Bind(TextBlock.ForegroundProperty, new Binding(nameof(StudentGradeRowViewModel.BelowPassingState))
                {
                    Converter = BelowPassingToBrushConverter.Instance
                });

                var scoreBadge = new Border
                {
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(10, 4),
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = scoreText
                };
                scoreBadge.Bind(Border.BackgroundProperty, new Binding(nameof(StudentGradeRowViewModel.BelowPassingState))
                {
                    Converter = BelowPassingToBrushConverter.Instance,
                    ConverterParameter = "bg"
                });

                var recTagText = new TextBlock
                {
                    Text = LocalizationService.Instance.GetString("RecoveryAppliedTag"),
                    FontSize = 10,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.White
                };

                var recTag = new Border
                {
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(5, 2),
                    VerticalAlignment = VerticalAlignment.Center,
                    Background = new SolidColorBrush(Color.Parse("#4F46E5")),
                    Child = recTagText
                };
                recTag.Bind(Visual.IsVisibleProperty, new Binding(nameof(StudentGradeRowViewModel.RecoveryApplied)));

                return new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 6,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Children = { scoreBadge, recTag }
                };
            })
        });

        GradesDataGrid.Columns.Add(new DataGridTemplateColumn
        {
            Header = LocalizationService.Instance.GetString("FinalScoreColumnHeader"),
            Width = new DataGridLength(115),
            IsReadOnly = true,
            CellTemplate = new FuncDataTemplate<StudentGradeRowViewModel>((_, _) =>
            {
                var finalText = new TextBlock
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = FontWeight.Bold,
                    FontSize = 13
                };
                finalText.Bind(TextBlock.TextProperty, new Binding(nameof(StudentGradeRowViewModel.FinalScoreText)));
                finalText.Bind(TextBlock.ForegroundProperty, new Binding(nameof(StudentGradeRowViewModel.FinalBelowPassingState))
                {
                    Converter = BelowPassingToBrushConverter.Instance
                });

                return finalText;
            })
        });

        GradesDataGrid.Columns.Add(new DataGridTemplateColumn
        {
            Header = LocalizationService.Instance.GetString("StatusColumnHeader"),
            Width = new DataGridLength(145),
            IsReadOnly = true,
            CellTemplate = new FuncDataTemplate<StudentGradeRowViewModel>((_, _) =>
            {
                var statusText = new TextBlock
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 12,
                    FontWeight = FontWeight.SemiBold,
                    Margin = new Thickness(8, 0)
                };
                statusText.Bind(TextBlock.TextProperty, new Binding(nameof(StudentGradeRowViewModel.StatusLabel)));
                statusText.Bind(TextBlock.ForegroundProperty, new Binding(nameof(StudentGradeRowViewModel.BelowPassingState))
                {
                    Converter = BelowPassingToBrushConverter.Instance
                });

                return statusText;
            })
        });
    }
}

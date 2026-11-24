using DiarioLivre.Core.Enums;
using DiarioLivre.Core.Interfaces;
using DiarioLivre.Core.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;

namespace DiarioLivre.Infrastructure.Services;

/// <summary>
/// Generates printable A4 student dossier, class gradebook, and class attendance PDF reports using QuestPDF.
/// </summary>
public sealed class QuestPdfReportService : IReportGenerator
{
    static QuestPdfReportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <inheritdoc />
    public async Task GenerateStudentDossierPdfAsync(
        StudentDossierReportData data,
        string outputFilePath,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        EnsureDirectory(outputFilePath);

        byte[]? avatarPngBytes = await LoadAvatarAsPngBytesAsync(data.AvatarFilePath, ct);
        PdfReportLabels labels = data.Labels ?? PdfReportLabels.Default;

        await Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken4));

                    page.Header().Element(header => ComposeHeader(header, data.SchoolName, $"{data.ReportTitle} • {data.Year} • {data.ClassroomName}"));
                    page.Content().PaddingVertical(12).Element(content => ComposeDossierContent(content, data, labels, avatarPngBytes));
                    page.Footer().Element(footer => ComposeFooter(footer, data.GeneratedAtLabel));
                });
            }).GeneratePdf(outputFilePath);
        }, ct);
    }

    /// <inheritdoc />
    public async Task GenerateClassGradebookPdfAsync(
        ClassGradebookReportData data,
        string outputFilePath,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        EnsureDirectory(outputFilePath);

        ClassGradebookReportLabels labels = data.Labels ?? ClassGradebookReportLabels.Default;

        await Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1.3f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9.5f).FontColor(Colors.Grey.Darken4));

                    page.Header().Element(header => ComposeHeader(
                        header,
                        data.SchoolName,
                        $"{labels.ReportTitle} • {data.Year} • {data.ClassroomName} • {data.SubjectName} ({data.TermLabel})"));

                    page.Content().PaddingVertical(10).Element(content => ComposeClassGradebookContent(content, data, labels));
                    page.Footer().Element(footer => ComposeFooter(footer, labels.GeneratedAtLabel));
                });
            }).GeneratePdf(outputFilePath);
        }, ct);
    }

    /// <inheritdoc />
    public async Task GenerateClassAttendancePdfAsync(
        ClassAttendanceReportData data,
        string outputFilePath,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        EnsureDirectory(outputFilePath);

        ClassAttendanceReportLabels labels = data.Labels ?? ClassAttendanceReportLabels.Default;

        await Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.4f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9.5f).FontColor(Colors.Grey.Darken4));

                    string headerSubtitle = string.IsNullOrWhiteSpace(data.PeriodDisplay)
                        ? $"{labels.ReportTitle} • {data.Year} • {data.ClassroomName} • {data.SubjectDisplay}"
                        : $"{labels.ReportTitle} • {data.Year} • {data.ClassroomName} • {data.SubjectDisplay} • {data.PeriodDisplay}";

                    page.Header().Element(header => ComposeHeader(
                        header,
                        data.SchoolName,
                        headerSubtitle));

                    page.Content().PaddingVertical(10).Element(content => ComposeClassAttendanceContent(content, data, labels));
                    page.Footer().Element(footer => ComposeFooter(footer, labels.GeneratedAtLabel));
                });
            }).GeneratePdf(outputFilePath);
        }, ct);
    }

    /// <inheritdoc />
    public async Task GenerateBatchStudentDossiersPdfAsync(
        IReadOnlyList<StudentDossierReportData> dossiers,
        string outputFilePath,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dossiers);
        EnsureDirectory(outputFilePath);

        if (dossiers.Count == 0)
        {
            return;
        }

        var loadedAvatars = new List<byte[]?>(dossiers.Count);
        foreach (StudentDossierReportData dossier in dossiers)
        {
            loadedAvatars.Add(await LoadAvatarAsPngBytesAsync(dossier.AvatarFilePath, ct));
        }

        await Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

            Document.Create(container =>
            {
                for (int i = 0; i < dossiers.Count; i++)
                {
                    StudentDossierReportData data = dossiers[i];
                    byte[]? avatarPngBytes = loadedAvatars[i];
                    PdfReportLabels labels = data.Labels ?? PdfReportLabels.Default;

                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(1.5f, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken4));

                        page.Header().Element(header => ComposeHeader(
                            header,
                            data.SchoolName,
                            $"{data.ReportTitle} • {data.Year} • {data.ClassroomName}"));
                        page.Content().PaddingVertical(12).Element(content => ComposeDossierContent(content, data, labels, avatarPngBytes));
                        page.Footer().Element(footer => ComposeFooter(footer, data.GeneratedAtLabel));
                    });
                }
            }).GeneratePdf(outputFilePath);
        }, ct);
    }

    /// <inheritdoc />
    public async Task GenerateClassCouncilPdfAsync(
        ClassCouncilReportData data,
        string outputFilePath,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        EnsureDirectory(outputFilePath);

        ClassCouncilReportLabels labels = data.Labels ?? ClassCouncilReportLabels.Default;

        await Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1.2f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Darken4));

                    page.Header().Element(header => ComposeHeader(
                        header,
                        data.SchoolName,
                        $"{labels.ReportTitle} • {data.Year} • {data.ClassroomName} • {data.ShiftDisplay} • {data.GradingSystemDisplay}"));

                    page.Content().PaddingVertical(10).Element(content => ComposeClassCouncilContent(content, data, labels));
                    page.Footer().Element(footer => ComposeFooter(footer, labels.GeneratedAtLabel));
                });
            }).GeneratePdf(outputFilePath);
        }, ct);
    }

    private static void EnsureDirectory(string outputFilePath)
    {
        if (string.IsNullOrWhiteSpace(outputFilePath))
        {
            throw new ArgumentException("Output PDF path cannot be empty.", nameof(outputFilePath));
        }

        string? directory = Path.GetDirectoryName(outputFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static async Task<byte[]?> LoadAvatarAsPngBytesAsync(string? avatarFilePath, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(avatarFilePath) || !File.Exists(avatarFilePath))
        {
            return null;
        }

        try
        {
            byte[] rawBytes = await File.ReadAllBytesAsync(avatarFilePath, ct);
            using SKBitmap? bitmap = SKBitmap.Decode(rawBytes);
            if (bitmap is null)
            {
                return null;
            }

            using SKImage image = SKImage.FromBitmap(bitmap);
            using SKData? pngData = image.Encode(SKEncodedImageFormat.Png, 100);
            return pngData?.ToArray();
        }
        catch
        {
            return null;
        }
    }

    private static void ComposeHeader(IContainer container, string schoolName, string subtitle)
    {
        container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(8).Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text(schoolName).FontSize(14).Bold().FontColor(Colors.Indigo.Darken2);
                col.Item().Text(subtitle).FontSize(10).FontColor(Colors.Grey.Darken1);
            });
        });
    }

    private static void ComposeFooter(IContainer container, string generatedAtLabel)
    {
        container.AlignCenter().Text(text =>
        {
            text.Span($"{generatedAtLabel}: {DateTime.Now:dd/MM/yyyy HH:mm}   •   Diário Livre   •   ");
            text.CurrentPageNumber();
            text.Span(" / ");
            text.TotalPages();
        });
    }

    private static void ComposeDossierContent(
        IContainer container,
        StudentDossierReportData data,
        PdfReportLabels labels,
        byte[]? avatarPngBytes)
    {
        container.Column(column =>
        {
            column.Spacing(14);

            column.Item().Background(Colors.Grey.Lighten4).Padding(12).Row(row =>
            {
                if (avatarPngBytes is not null)
                {
                    row.ConstantItem(64).Height(64).Image(avatarPngBytes);
                    row.ConstantItem(12);
                }

                row.RelativeItem().Column(info =>
                {
                    info.Spacing(3);
                    info.Item().Text($"#{data.Student.RollNumber:D2} — {data.Student.FullName}")
                        .FontSize(14)
                        .Bold();

                    if (data.Student.HasSpecialNeeds)
                    {
                        info.Item().Text(labels.SpecialNeedsBanner)
                            .FontSize(9)
                            .Bold()
                            .FontColor(Colors.Orange.Darken3);
                    }

                    if (!string.IsNullOrWhiteSpace(data.Student.GeneralNotes))
                    {
                        info.Item().Text(data.Student.GeneralNotes)
                            .FontSize(9.5f)
                            .FontColor(Colors.Grey.Darken2);
                    }
                });
            });

            column.Item().Element(c => ComposeAttendanceSection(c, data.AttendanceSummary, labels));
            column.Item().Element(c => ComposeGradesSection(c, data.SubjectGrades, labels));
            column.Item().Element(c => ComposeObservationsSection(c, data.Observations, labels));
        });
    }

    private static void ComposeAttendanceSection(
        IContainer container,
        AttendanceSummary summary,
        PdfReportLabels labels)
    {
        string statusColor = summary.AlertStatus switch
        {
            AttendanceAlertStatus.Critical => Colors.Red.Darken2,
            AttendanceAlertStatus.Warning => Colors.Amber.Darken3,
            _ => Colors.Green.Darken2
        };

        string alertText = string.IsNullOrWhiteSpace(labels.AlertStatusDisplay)
            ? summary.AlertStatus.ToString()
            : labels.AlertStatusDisplay;

        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
        {
            col.Spacing(8);
            col.Item().Text(labels.AttendanceSummaryTitle).FontSize(11).Bold().FontColor(Colors.Indigo.Darken2);

            col.Item().Row(row =>
            {
                row.RelativeItem().Background(Colors.Grey.Lighten5).Padding(6).Column(cell =>
                {
                    cell.Item().Text(labels.TotalLessonsLabel).FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                    cell.Item().Text($"{summary.TotalLessons}").FontSize(11).Bold();
                });
                row.ConstantItem(6);
                row.RelativeItem().Background(Colors.Grey.Lighten5).Padding(6).Column(cell =>
                {
                    cell.Item().Text(labels.PresentLabel).FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                    cell.Item().Text($"{summary.PresentLessons}").FontSize(11).Bold();
                });
                row.ConstantItem(6);
                row.RelativeItem().Background(Colors.Grey.Lighten5).Padding(6).Column(cell =>
                {
                    cell.Item().Text(labels.UnexcusedLabel).FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                    cell.Item().Text($"{summary.UnexcusedAbsences}").FontSize(11).Bold();
                });
                row.ConstantItem(6);
                row.RelativeItem().Background(Colors.Grey.Lighten5).Padding(6).Column(cell =>
                {
                    cell.Item().Text(labels.JustifiedLabel).FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                    cell.Item().Text($"{summary.JustifiedAbsences}").FontSize(11).Bold();
                });
            });

            col.Item().Row(row =>
            {
                row.RelativeItem().Text($"{labels.AttendanceRateLabel}: {summary.AttendancePercentage:F2}%").Bold();
                row.RelativeItem().Text($"{labels.AbsencesRateLabel}: {summary.AbsencePercentage:F2}%").Bold();
                row.RelativeItem().Text($"{labels.StatusLabel}: {alertText}").Bold().FontColor(statusColor);
            });
        });
    }

    private static void ComposeGradesSection(
        IContainer container,
        IReadOnlyList<SubjectGradeReportItem> subjectGrades,
        PdfReportLabels labels)
    {
        container.Column(col =>
        {
            col.Spacing(6);
            col.Item().Text(labels.AcademicPerformanceTitle).FontSize(11).Bold().FontColor(Colors.Indigo.Darken2);

            if (subjectGrades.Count == 0)
            {
                col.Item().Text(labels.NoSubjectsText).FontColor(Colors.Grey.Darken1);
                return;
            }

            int maxTerms = subjectGrades.Max(s => s.FinalResult.TermResults.Count);

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2.5f);
                    for (int i = 0; i < maxTerms; i++)
                    {
                        columns.RelativeColumn(1f);
                    }
                    columns.RelativeColumn(1.2f);
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Indigo.Lighten4).Padding(5).Text(labels.SubjectColumn).Bold();
                    for (int i = 1; i <= maxTerms; i++)
                    {
                        header.Cell().Background(Colors.Indigo.Lighten4).Padding(5).AlignCenter().Text($"{labels.TermShortPrefix}{i}").Bold();
                    }
                    header.Cell().Background(Colors.Indigo.Lighten4).Padding(5).AlignCenter().Text(labels.FinalColumn).Bold();
                });

                for (int rowIndex = 0; rowIndex < subjectGrades.Count; rowIndex++)
                {
                    SubjectGradeReportItem item = subjectGrades[rowIndex];
                    string rowBg = rowIndex % 2 == 1 ? Colors.Grey.Lighten5 : Colors.White;

                    table.Cell().Background(rowBg).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(item.SubjectName);

                    for (int i = 0; i < maxTerms; i++)
                    {
                        TermGradeResult? term = i < item.FinalResult.TermResults.Count
                            ? item.FinalResult.TermResults[i]
                            : null;

                        string scoreText = term?.Score is decimal s ? $"{s:F2}" : "-";
                        table.Cell().Background(rowBg).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text(scoreText);
                    }

                    string finalText = item.FinalResult.FinalScore is decimal f ? $"{f:F2}" : "-";
                    string finalColor = item.FinalResult.FinalScore.HasValue && !item.FinalResult.IsPassing
                        ? Colors.Red.Darken2
                        : Colors.Grey.Darken4;

                    table.Cell().Background(rowBg).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignCenter()
                        .Text(finalText).Bold().FontColor(finalColor);
                }
            });
        });
    }

    private static void ComposeObservationsSection(
        IContainer container,
        IReadOnlyList<ObservationReportItem> observations,
        PdfReportLabels labels)
    {
        container.Column(col =>
        {
            col.Spacing(6);
            col.Item().Text(labels.ObservationsTitle).FontSize(11).Bold().FontColor(Colors.Indigo.Darken2);

            if (observations.Count == 0)
            {
                col.Item().Text(labels.NoObservationsText).FontColor(Colors.Grey.Darken1);
                return;
            }

            foreach (ObservationReportItem obs in observations)
            {
                col.Item().BorderLeft(2).BorderColor(Colors.Indigo.Medium).PaddingLeft(8).PaddingVertical(3).Column(entry =>
                {
                    entry.Item().Text($"{obs.Date:dd/MM/yyyy} • {obs.CategoryLabel}")
                        .FontSize(9)
                        .Bold()
                        .FontColor(Colors.Indigo.Darken1);
                    entry.Item().Text(obs.Content).FontSize(9.5f);
                });
            }
        });
    }

    private static void ComposeClassGradebookContent(
        IContainer container,
        ClassGradebookReportData data,
        ClassGradebookReportLabels labels)
    {
        container.Column(col =>
        {
            col.Spacing(10);

            col.Item().Background(Colors.Grey.Lighten4).Padding(8).Row(row =>
            {
                row.RelativeItem().Text($"{labels.ClassAverageLabel}: {data.ClassAverageText}").Bold();
                row.RelativeItem().Text($"{labels.PassingLabel}: {data.PassingCount}").Bold().FontColor(Colors.Green.Darken2);
                row.RelativeItem().Text($"{labels.BelowPassingLabel}: {data.BelowPassingCount}").Bold().FontColor(Colors.Red.Darken2);
                row.RelativeItem().Text($"{labels.PendingLabel}: {data.PendingCount}").Bold().FontColor(Colors.Grey.Darken1);
            });

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(32);
                    columns.RelativeColumn(2.4f);
                    for (int i = 0; i < data.Assessments.Count; i++)
                    {
                        columns.RelativeColumn(1f);
                    }
                    columns.ConstantColumn(72);
                    columns.ConstantColumn(72);
                    columns.ConstantColumn(96);
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Indigo.Lighten4).Padding(5).AlignCenter().Text(labels.RollColumn).Bold();
                    header.Cell().Background(Colors.Indigo.Lighten4).Padding(5).Text(labels.StudentColumn).Bold();

                    foreach (ClassGradebookAssessmentColumn assessment in data.Assessments)
                    {
                        string title = assessment.IsRecovery
                            ? $"{assessment.Title} (Rec/{assessment.MaxScore:0.#})"
                            : $"{assessment.Title} ({assessment.MaxScore:0.#})";
                        header.Cell().Background(Colors.Indigo.Lighten4).Padding(5).AlignCenter().Text(title).Bold();
                    }

                    header.Cell().Background(Colors.Indigo.Lighten4).Padding(5).AlignCenter().Text(labels.TermScoreColumn).Bold();
                    header.Cell().Background(Colors.Indigo.Lighten4).Padding(5).AlignCenter().Text(labels.FinalScoreColumn).Bold();
                    header.Cell().Background(Colors.Indigo.Lighten4).Padding(5).AlignCenter().Text(labels.StatusColumn).Bold();
                });

                for (int r = 0; r < data.Rows.Count; r++)
                {
                    ClassGradebookStudentRow row = data.Rows[r];
                    string bg = r % 2 == 1 ? Colors.Grey.Lighten5 : Colors.White;

                    table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text($"{row.RollNumber:D2}");
                    table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(row.FullName);

                    for (int i = 0; i < data.Assessments.Count; i++)
                    {
                        string cellVal = i < row.AssessmentScores.Count ? row.AssessmentScores[i] : "—";
                        table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text(cellVal);
                    }

                    string termDisplay = row.RecoveryApplied ? $"{row.TermScoreText} *" : row.TermScoreText;
                    string color = row.IsBelowPassing ? Colors.Red.Darken2 : Colors.Grey.Darken4;

                    table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text(termDisplay).Bold().FontColor(color);
                    table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text(row.FinalScoreText).Bold();
                    table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text(row.StatusText).FontColor(color);
                }
            });
        });
    }

    private static void ComposeClassAttendanceContent(
        IContainer container,
        ClassAttendanceReportData data,
        ClassAttendanceReportLabels labels)
    {
        container.Column(col =>
        {
            col.Spacing(14);

            col.Item().Text(labels.StudentsSummaryTitle).FontSize(11).Bold().FontColor(Colors.Indigo.Darken2);
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(28);
                    columns.RelativeColumn(2.4f);
                    columns.ConstantColumn(48);
                    columns.ConstantColumn(48);
                    columns.ConstantColumn(52);
                    columns.ConstantColumn(52);
                    columns.ConstantColumn(64);
                    columns.ConstantColumn(86);
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Indigo.Lighten4).Padding(5).AlignCenter().Text(labels.RollColumn).Bold();
                    header.Cell().Background(Colors.Indigo.Lighten4).Padding(5).Text(labels.StudentColumn).Bold();
                    header.Cell().Background(Colors.Indigo.Lighten4).Padding(5).AlignCenter().Text(labels.TotalLessonsColumn).Bold();
                    header.Cell().Background(Colors.Indigo.Lighten4).Padding(5).AlignCenter().Text(labels.PresentColumn).Bold();
                    header.Cell().Background(Colors.Indigo.Lighten4).Padding(5).AlignCenter().Text(labels.UnexcusedColumn).Bold();
                    header.Cell().Background(Colors.Indigo.Lighten4).Padding(5).AlignCenter().Text(labels.JustifiedColumn).Bold();
                    header.Cell().Background(Colors.Indigo.Lighten4).Padding(5).AlignCenter().Text(labels.AttendanceRateColumn).Bold();
                    header.Cell().Background(Colors.Indigo.Lighten4).Padding(5).AlignCenter().Text(labels.StatusColumn).Bold();
                });

                for (int i = 0; i < data.Students.Count; i++)
                {
                    ClassAttendanceStudentRow s = data.Students[i];
                    string bg = i % 2 == 1 ? Colors.Grey.Lighten5 : Colors.White;
                    string statusColor = s.AlertStatus switch
                    {
                        AttendanceAlertStatus.Critical => Colors.Red.Darken2,
                        AttendanceAlertStatus.Warning => Colors.Amber.Darken3,
                        _ => Colors.Green.Darken2
                    };

                    table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text($"{s.RollNumber:D2}");
                    table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(s.FullName);
                    table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text($"{s.TotalLessons}");
                    table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text($"{s.PresentLessons}");
                    table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text($"{s.UnexcusedAbsences}");
                    table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text($"{s.JustifiedAbsences}");
                    table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text($"{s.AttendancePercentage:F1}%").Bold();
                    table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text(s.AlertLabel).Bold().FontColor(statusColor);
                }
            });

            col.Item().Text(labels.LessonLogTitle).FontSize(11).Bold().FontColor(Colors.Indigo.Darken2);
            if (data.Sessions.Count == 0)
            {
                col.Item().Text(labels.NoSessionsText).FontColor(Colors.Grey.Darken1);
                return;
            }

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(70);
                    columns.ConstantColumn(50);
                    columns.RelativeColumn(3f);
                    columns.ConstantColumn(55);
                    columns.ConstantColumn(55);
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Indigo.Lighten4).Padding(5).Text(labels.DateColumn).Bold();
                    header.Cell().Background(Colors.Indigo.Lighten4).Padding(5).AlignCenter().Text(labels.TotalLessonsColumn).Bold();
                    header.Cell().Background(Colors.Indigo.Lighten4).Padding(5).Text(labels.TopicColumn).Bold();
                    header.Cell().Background(Colors.Indigo.Lighten4).Padding(5).AlignCenter().Text(labels.PresentColumn).Bold();
                    header.Cell().Background(Colors.Indigo.Lighten4).Padding(5).AlignCenter().Text(labels.UnexcusedColumn).Bold();
                });

                for (int i = 0; i < data.Sessions.Count; i++)
                {
                    ClassAttendanceSessionItem item = data.Sessions[i];
                    string bg = i % 2 == 1 ? Colors.Grey.Lighten5 : Colors.White;

                    table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text($"{item.Date:dd/MM/yyyy}");
                    table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text($"{item.LessonCount}");
                    table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Column(cellCol =>
                    {
                        cellCol.Spacing(2);
                        cellCol.Item().Text(string.IsNullOrWhiteSpace(item.TopicCovered) ? "—" : item.TopicCovered).SemiBold();

                        if (!string.IsNullOrWhiteSpace(item.DailyActivity))
                        {
                            cellCol.Item().Text($"{labels.DailyActivityColumn}: {item.DailyActivity}")
                                .FontSize(8.5f)
                                .FontColor(Colors.Grey.Darken2);
                        }

                        if (!string.IsNullOrWhiteSpace(item.Homework))
                        {
                            cellCol.Item().Text($"{labels.HomeworkColumn}: {item.Homework}")
                                .FontSize(8.5f)
                                .FontColor(Colors.Grey.Darken1);
                        }
                    });
                    table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text($"{item.PresentCount}");
                    table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text($"{item.AbsentCount}");
                }
            });
        });
    }

    private static void ComposeClassCouncilContent(
        IContainer container,
        ClassCouncilReportData data,
        ClassCouncilReportLabels labels)
    {
        container.Column(col =>
        {
            col.Spacing(10);

            col.Item().Background(Colors.Grey.Lighten4).Padding(8).Row(row =>
            {
                row.RelativeItem().Text($"{labels.PassingRuleLabel}: {data.PassingGrade:F1}").Bold();
                row.RelativeItem().Text($"{labels.MinAttendanceLabel}: {data.MinAttendancePercentage:F0}%").Bold();
                row.RelativeItem().Text($"{data.ClassroomName} • {data.Students.Count}").Bold().AlignRight();
            });

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(26);
                    columns.RelativeColumn(1.8f);
                    columns.ConstantColumn(30);
                    for (int i = 0; i < data.SubjectNames.Count; i++)
                    {
                        columns.RelativeColumn(0.85f);
                    }
                    columns.ConstantColumn(52);
                    columns.ConstantColumn(68);
                    columns.RelativeColumn(2.2f);
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Indigo.Lighten4).Padding(5).AlignCenter().Text(labels.RollColumn).Bold();
                    header.Cell().Background(Colors.Indigo.Lighten4).Padding(5).Text(labels.StudentColumn).Bold();
                    header.Cell().Background(Colors.Indigo.Lighten4).Padding(5).AlignCenter().Text(labels.PdiColumn).Bold();

                    foreach (string subjectName in data.SubjectNames)
                    {
                        header.Cell().Background(Colors.Indigo.Lighten4).Padding(5).AlignCenter().Text(subjectName).Bold();
                    }

                    header.Cell().Background(Colors.Indigo.Lighten4).Padding(5).AlignCenter().Text(labels.OverallColumn).Bold();
                    header.Cell().Background(Colors.Indigo.Lighten4).Padding(5).AlignCenter().Text(labels.AttendanceColumn).Bold();
                    header.Cell().Background(Colors.Indigo.Lighten4).Padding(5).Text(labels.NotesColumn).Bold();
                });

                for (int r = 0; r < data.Students.Count; r++)
                {
                    ClassCouncilStudentRow student = data.Students[r];
                    string bg = r % 2 == 1 ? Colors.Grey.Lighten5 : Colors.White;

                    string attColor = student.AlertStatus switch
                    {
                        AttendanceAlertStatus.Critical => Colors.Red.Darken2,
                        AttendanceAlertStatus.Warning => Colors.Amber.Darken3,
                        _ => Colors.Green.Darken2
                    };

                    table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text($"{student.RollNumber:D2}");
                    table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(student.FullName);
                    table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignCenter()
                        .Text(student.HasSpecialNeeds ? labels.YesShort : labels.NoShort)
                        .FontColor(student.HasSpecialNeeds ? Colors.Orange.Darken3 : Colors.Grey.Darken1)
                        .Bold();

                    for (int i = 0; i < data.SubjectNames.Count; i++)
                    {
                        string score = i < student.SubjectScores.Count ? student.SubjectScores[i] : "—";
                        table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text(score);
                    }

                    string avgColor = student.IsOverallBelowPassing ? Colors.Red.Darken2 : Colors.Grey.Darken4;
                    table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignCenter()
                        .Text(student.OverallAverageText).Bold().FontColor(avgColor);

                    table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignCenter()
                        .Text($"{student.AttendancePercentage:F1}%").Bold().FontColor(attColor);

                    table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5)
                        .Text(string.IsNullOrWhiteSpace(student.NotesSummary) ? "—" : student.NotesSummary)
                        .FontSize(8.5f);
                }
            });
        });
    }
}

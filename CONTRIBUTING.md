# Contributing to Diário Livre

Thank you for helping improve **Diário Livre** for K–12 teachers and schools!

## Architecture Overview

- **`src/DiarioLivre.Core`**: Pure domain entities, enums, interfaces, and calculators (`GradeCalculator`, `AttendanceCalculator`, `SchoolCalendarCalculator`, `PinHasher`). Keep this layer free of UI and database dependencies.
- **`src/DiarioLivre.Infrastructure`**: EF Core + SQLite (`DiarioLivreDbContext`), repositories, QuestPDF report generators (`QuestPdfReportService`), CSV export, local SkiaSharp 256px WebP avatars, and `.zip` backup/auto-snapshot services (`SqliteBackupService`).
- **`src/DiarioLivre.Desktop`**: Avalonia UI (`11.2.5`) Views/ViewModels (`CommunityToolkit.Mvvm`), resource strings (`Strings.resx`, `Strings.en-US.resx`), and theme resources (`App.axaml`).
- **`tests/DiarioLivre.Core.Tests`**: xUnit test suite covering domain rules and security helpers.

## Prerequisites & Local Workflow

- Install the **.NET 10 SDK**.
- Restore, build, and verify zero warnings and passing tests before opening a PR:

```bash
dotnet restore DiarioLivre.sln
dotnet build DiarioLivre.sln
dotnet test DiarioLivre.sln
```

## UI & Code Guidelines

1. **Zero compiler warnings**: Treat warnings in modified files as bugs.
2. **Theme-aware styling**: Always use `{DynamicResource ...}` theme brushes from `src/DiarioLivre.Desktop/App.axaml` instead of hardcoded light-mode colors.
3. **Compiled bindings**: Set `x:DataType` explicitly on every View and `DataTemplate`.
4. **Bilingual localization**: Whenever adding or changing UI copy, update both `Strings.resx` (`pt-BR`) and `Strings.en-US.resx` (`en-US`).
5. **Privacy / Offline-first**: Never introduce telemetry, external network calls, or mandatory accounts.

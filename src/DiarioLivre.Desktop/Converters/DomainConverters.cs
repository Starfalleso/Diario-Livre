using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using DiarioLivre.Core.Enums;

namespace DiarioLivre.Desktop.Converters;

internal static class ThemePaletteHelper
{
    public static bool IsDark => Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
}

public sealed class AlertStatusToBrushConverter : IValueConverter
{
    public static readonly AlertStatusToBrushConverter Instance = new();

    private static readonly IBrush LightNormal = new SolidColorBrush(Color.Parse("#15803D"));
    private static readonly IBrush LightWarning = new SolidColorBrush(Color.Parse("#B45309"));
    private static readonly IBrush LightCritical = new SolidColorBrush(Color.Parse("#B91C1C"));
    private static readonly IBrush LightNormalBg = new SolidColorBrush(Color.Parse("#1F16A34A"));
    private static readonly IBrush LightWarningBg = new SolidColorBrush(Color.Parse("#1FD97706"));
    private static readonly IBrush LightCriticalBg = new SolidColorBrush(Color.Parse("#1FDC2626"));

    private static readonly IBrush DarkNormal = new SolidColorBrush(Color.Parse("#4ADE80"));
    private static readonly IBrush DarkWarning = new SolidColorBrush(Color.Parse("#FBBF24"));
    private static readonly IBrush DarkCritical = new SolidColorBrush(Color.Parse("#F87171"));
    private static readonly IBrush DarkNormalBg = new SolidColorBrush(Color.Parse("#2422C55E"));
    private static readonly IBrush DarkWarningBg = new SolidColorBrush(Color.Parse("#26F59E0B"));
    private static readonly IBrush DarkCriticalBg = new SolidColorBrush(Color.Parse("#26EF4444"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isBackground = string.Equals(parameter as string, "bg", StringComparison.OrdinalIgnoreCase);
        bool isDark = ThemePaletteHelper.IsDark;

        if (value is AttendanceAlertStatus status)
        {
            return status switch
            {
                AttendanceAlertStatus.Critical => isBackground
                    ? (isDark ? DarkCriticalBg : LightCriticalBg)
                    : (isDark ? DarkCritical : LightCritical),
                AttendanceAlertStatus.Warning => isBackground
                    ? (isDark ? DarkWarningBg : LightWarningBg)
                    : (isDark ? DarkWarning : LightWarning),
                _ => isBackground
                    ? (isDark ? DarkNormalBg : LightNormalBg)
                    : (isDark ? DarkNormal : LightNormal)
            };
        }

        return isBackground
            ? (isDark ? DarkNormalBg : LightNormalBg)
            : (isDark ? DarkNormal : LightNormal);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class BelowPassingToBrushConverter : IValueConverter
{
    public static readonly BelowPassingToBrushConverter Instance = new();

    private static readonly IBrush LightDangerText = new SolidColorBrush(Color.Parse("#DC2626"));
    private static readonly IBrush LightDangerBg = new SolidColorBrush(Color.Parse("#22DC2626"));
    private static readonly IBrush LightPassingText = new SolidColorBrush(Color.Parse("#15803D"));
    private static readonly IBrush LightPassingBg = new SolidColorBrush(Color.Parse("#1F16A34A"));
    private static readonly IBrush LightPendingText = new SolidColorBrush(Color.Parse("#64748B"));

    private static readonly IBrush DarkDangerText = new SolidColorBrush(Color.Parse("#F87171"));
    private static readonly IBrush DarkDangerBg = new SolidColorBrush(Color.Parse("#28EF4444"));
    private static readonly IBrush DarkPassingText = new SolidColorBrush(Color.Parse("#4ADE80"));
    private static readonly IBrush DarkPassingBg = new SolidColorBrush(Color.Parse("#2422C55E"));
    private static readonly IBrush DarkPendingText = new SolidColorBrush(Color.Parse("#94A3B8"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isBackground = string.Equals(parameter as string, "bg", StringComparison.OrdinalIgnoreCase);
        bool isDark = ThemePaletteHelper.IsDark;

        if (value is bool isBelowPassing)
        {
            if (isBelowPassing)
            {
                return isBackground
                    ? (isDark ? DarkDangerBg : LightDangerBg)
                    : (isDark ? DarkDangerText : LightDangerText);
            }

            return isBackground
                ? (isDark ? DarkPassingBg : LightPassingBg)
                : (isDark ? DarkPassingText : LightPassingText);
        }

        return isBackground
            ? Brushes.Transparent
            : (isDark ? DarkPendingText : LightPendingText);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class ObservationCategoryToBrushConverter : IValueConverter
{
    public static readonly ObservationCategoryToBrushConverter Instance = new();

    private static readonly IBrush LightPedagogical = new SolidColorBrush(Color.Parse("#2563EB"));
    private static readonly IBrush LightBehavioral = new SolidColorBrush(Color.Parse("#EA580C"));
    private static readonly IBrush LightPraise = new SolidColorBrush(Color.Parse("#16A34A"));
    private static readonly IBrush LightFamily = new SolidColorBrush(Color.Parse("#7C3AED"));

    private static readonly IBrush DarkPedagogical = new SolidColorBrush(Color.Parse("#60A5FA"));
    private static readonly IBrush DarkBehavioral = new SolidColorBrush(Color.Parse("#FB923C"));
    private static readonly IBrush DarkPraise = new SolidColorBrush(Color.Parse("#4ADE80"));
    private static readonly IBrush DarkFamily = new SolidColorBrush(Color.Parse("#A78BFA"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isDark = ThemePaletteHelper.IsDark;

        if (value is ObservationCategory category)
        {
            return category switch
            {
                ObservationCategory.Pedagogical => isDark ? DarkPedagogical : LightPedagogical,
                ObservationCategory.Behavioral => isDark ? DarkBehavioral : LightBehavioral,
                ObservationCategory.Praise => isDark ? DarkPraise : LightPraise,
                ObservationCategory.FamilyContact => isDark ? DarkFamily : LightFamily,
                _ => isDark ? DarkPedagogical : LightPedagogical
            };
        }

        return isDark ? DarkPedagogical : LightPedagogical;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class AttendanceStatusMatchConverter : IValueConverter
{
    public static readonly AttendanceStatusMatchConverter Instance = new();

    private static readonly IBrush PresentActiveBg = new SolidColorBrush(Color.Parse("#16A34A"));
    private static readonly IBrush AbsentActiveBg = new SolidColorBrush(Color.Parse("#DC2626"));
    private static readonly IBrush JustifiedActiveBg = new SolidColorBrush(Color.Parse("#D97706"));
    private static readonly IBrush LightInactiveBg = new SolidColorBrush(Color.Parse("#E2E8F0"));
    private static readonly IBrush DarkInactiveBg = new SolidColorBrush(Color.Parse("#222C40"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isDark = ThemePaletteHelper.IsDark;
        IBrush inactiveBg = isDark ? DarkInactiveBg : LightInactiveBg;

        if (value is AttendanceStatus status &&
            parameter is string targetName &&
            Enum.TryParse<AttendanceStatus>(targetName, out AttendanceStatus target))
        {
            if (status != target)
            {
                return inactiveBg;
            }

            return target switch
            {
                AttendanceStatus.Present => PresentActiveBg,
                AttendanceStatus.Absent => AbsentActiveBg,
                AttendanceStatus.Justified => JustifiedActiveBg,
                _ => inactiveBg
            };
        }

        return inactiveBg;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class AttendanceStatusForegroundConverter : IValueConverter
{
    public static readonly AttendanceStatusForegroundConverter Instance = new();

    private static readonly IBrush LightInactiveText = new SolidColorBrush(Color.Parse("#475569"));
    private static readonly IBrush DarkInactiveText = new SolidColorBrush(Color.Parse("#CBD5E1"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is AttendanceStatus status &&
            parameter is string targetName &&
            Enum.TryParse<AttendanceStatus>(targetName, out AttendanceStatus target) &&
            status == target)
        {
            return Brushes.White;
        }

        return ThemePaletteHelper.IsDark ? DarkInactiveText : LightInactiveText;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class AvatarPathToBitmapConverter : IValueConverter
{
    public static readonly AvatarPathToBitmapConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string path && !string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            try
            {
                return new Bitmap(path);
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class InitialsConverter : IValueConverter
{
    public static readonly InitialsConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => ExtractInitials(value as string);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    public static string ExtractInitials(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return "?";
        }

        string[] parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 1)
        {
            return parts[0].Length >= 2
                ? parts[0][..2].ToUpperInvariant()
                : parts[0].ToUpperInvariant();
        }

        return string.Concat(char.ToUpperInvariant(parts[0][0]), char.ToUpperInvariant(parts[^1][0]));
    }
}

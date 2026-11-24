using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using DiarioLivre.Desktop.Converters;

namespace DiarioLivre.Desktop.Controls;

/// <summary>
/// Circular student or classroom avatar showing a local WebP image when present or deterministic colored initials.
/// </summary>
public partial class AvatarControl : UserControl
{
    public static readonly StyledProperty<string?> PhotoPathProperty =
        AvaloniaProperty.Register<AvatarControl, string?>(nameof(PhotoPath));

    public static readonly StyledProperty<string?> FullNameProperty =
        AvaloniaProperty.Register<AvatarControl, string?>(nameof(FullName));

    public static readonly StyledProperty<double> SizeProperty =
        AvaloniaProperty.Register<AvatarControl, double>(nameof(Size), 40.0);

    private static readonly Color[] PaletteColors =
    {
        Color.Parse("#4F46E5"),
        Color.Parse("#0D9488"),
        Color.Parse("#2563EB"),
        Color.Parse("#7C3AED"),
        Color.Parse("#DB2777"),
        Color.Parse("#D97706"),
        Color.Parse("#059669"),
        Color.Parse("#0284C7")
    };

    private Bitmap? _loadedBitmap;

    public AvatarControl()
    {
        InitializeComponent();
        UpdateVisuals();
    }

    public string? PhotoPath
    {
        get => GetValue(PhotoPathProperty);
        set => SetValue(PhotoPathProperty, value);
    }

    public string? FullName
    {
        get => GetValue(FullNameProperty);
        set => SetValue(FullNameProperty, value);
    }

    public double Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == PhotoPathProperty ||
            change.Property == FullNameProperty ||
            change.Property == SizeProperty)
        {
            UpdateVisuals();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        DisposeBitmap();
    }

    private void UpdateVisuals()
    {
        double size = Size > 0 ? Size : 40.0;
        AvatarBorder.Width = size;
        AvatarBorder.Height = size;
        AvatarBorder.CornerRadius = new CornerRadius(size / 2.0);
        InitialsTextBlock.FontSize = Math.Max(11, Math.Round(size * 0.36));

        string? name = FullName;
        InitialsTextBlock.Text = InitialsConverter.ExtractInitials(name);
        AvatarBorder.Background = new SolidColorBrush(PickColor(name));

        LoadPhoto(PhotoPath);
    }

    private void LoadPhoto(string? path)
    {
        DisposeBitmap();

        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            try
            {
                _loadedBitmap = new Bitmap(path);
                PhotoImage.Source = _loadedBitmap;
                PhotoImage.IsVisible = true;
                InitialsTextBlock.IsVisible = false;
                return;
            }
            catch
            {
                DisposeBitmap();
            }
        }

        PhotoImage.Source = null;
        PhotoImage.IsVisible = false;
        InitialsTextBlock.IsVisible = true;
    }

    private void DisposeBitmap()
    {
        if (_loadedBitmap is not null)
        {
            PhotoImage.Source = null;
            _loadedBitmap.Dispose();
            _loadedBitmap = null;
        }
    }

    private static Color PickColor(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return PaletteColors[0];
        }

        uint hash = 2166136261;
        foreach (char c in name)
        {
            hash ^= c;
            hash *= 16777619;
        }

        return PaletteColors[hash % (uint)PaletteColors.Length];
    }
}

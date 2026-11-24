using Avalonia.Controls;
using Avalonia.Controls.Templates;
using DiarioLivre.Desktop.ViewModels;

namespace DiarioLivre.Desktop;

/// <summary>
/// Resolves Avalonia views matching ViewModel class names by convention.
/// </summary>
public sealed class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null)
        {
            return null;
        }

        string name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        Type? type = Type.GetType(name);

        if (type is not null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }

        return new TextBlock { Text = $"View not found: {name}" };
    }

    public bool Match(object? data) => data is ViewModelBase;
}

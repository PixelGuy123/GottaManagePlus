using Avalonia;
using Avalonia.Controls;

namespace GottaManagePlus.Views;

public partial class MultiLoadingDialogView : UserControl
{
    public MultiLoadingDialogView()
    {
        InitializeComponent();
        Status.PropertyChanged += OnStatusOnPropertyChanged;
    }

    private void OnStatusOnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs args)
    {
        if (args.Property.Name != nameof(Status.Text)) return;

        if (args.NewValue is string { Length: > 124 } text)
            Status.FontSize = MathF.Max(12, 18 - (text.Length - 123) * 0.2f);
        else
            Status.FontSize = 18d;
    }
}
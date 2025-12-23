using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GottaManagePlus.Views;

public partial class PreviewProfileDialogView : UserControl
{
    public PreviewProfileDialogView()
    {
        InitializeComponent();
        DeleteButton.PropertyChanged += ButtonOnPropertyChanged;
    }
    
    // Opacity update to not be specifically 0
    private void ButtonOnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs args)
    {
        if (sender is not Button button) return;
        // If opacity 0, change to something different
        if (args.Property.Name == nameof(Opacity) && (double?)args.NewValue <= 0.0d)
            button.Opacity = 0.25d;
    }
}
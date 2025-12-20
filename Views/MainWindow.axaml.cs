using Avalonia;
using Avalonia.Controls;

namespace GottaManagePlus.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        MyModsBtn.PropertyChanged += MyModsButtonOnPropertyChanged;
    }
    
    // Opacity update to not be specifically 0
    private static void MyModsButtonOnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs args)
    {
        if (sender is not Button button) return;
        // If opacity 0, change to something different
        if (args.Property.Name == nameof(Opacity) && (double?)args.NewValue <= 0.0d)
            button.Opacity = 0.5d;
    }
}
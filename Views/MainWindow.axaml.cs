using Avalonia;
using Avalonia.Controls;

namespace GottaManagePlus.Views;

public partial class MainWindow : Window
{
    private readonly GridLength _noBelowBar = new(200), _withBelowBar = new(300);
    public MainWindow()
    {
        InitializeComponent();
        
        // Event registration
        MyModsBtn.PropertyChanged += MyModsButtonOnPropertyChanged;
        BelowBarControl.PropertyChanged += BelowBarOnPropertyChanged;
        
        // Update on start
        BodyGrid.ColumnDefinitions[0].Width = BelowBarControl.Content == null ? _noBelowBar : _withBelowBar;
    }
    
    // Opacity update to not be specifically 0
    private void MyModsButtonOnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs args)
    {
        if (sender is not Button button) return;
        // If opacity 0, change to something different
        if (args.Property.Name == nameof(Opacity) && (double?)args.NewValue <= 0.0d)
            button.Opacity = 0.5d;
    }
    
    // Update grid size
    private void BelowBarOnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs args)
    {
        if (sender is not ContentControl) return;

        // Update body grid based on width
        if (args.Property.Name == nameof(Content))
            BodyGrid.ColumnDefinitions[0].Width = args.NewValue == null ? _noBelowBar : _withBelowBar;
    }
}
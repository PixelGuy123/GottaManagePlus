using Avalonia;
using Avalonia.Controls;

namespace GottaManagePlus.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        SavePanel.PropertyChanged += SavePanelOnPropertyChanged;
        
        // When loaded, display the end of string, not start
        GamePathText.PropertyChanged += (s, e) =>
        {
            if (s == null || e.Property.Name != nameof(TextBox.Text)) return;
            
            var textBox = (TextBox)s;
            textBox.CaretIndex = ((string)e.NewValue!).Length;
        };
    }

    // Opacity update to not be specifically 0
    private void SavePanelOnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs args)
    {
        if (sender is not StackPanel stackPanel) return;
        // If opacity 0, change to something different
        if (args.Property.Name == nameof(Opacity) && (double?)args.NewValue <= 0.0d)
            stackPanel.Opacity = 0.25d;
    }
    
    
}
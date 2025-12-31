using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using GottaManagePlus.ViewModels;

namespace GottaManagePlus.Views;

public partial class MainWindow : Window
{
    private readonly GridLength _noBelowBar = new(200), _withBelowBar = new(300);
    private bool _canClose = false;
    
    public MainWindow()
    {
        InitializeComponent();
        
        // Event registration
        MyModsBtn.PropertyChanged += MyModsButtonOnPropertyChanged;
    }
    // Opacity update to not be specifically 0
    private void MyModsButtonOnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs args)
    {
        if (sender is not Button button) return;
        // If opacity 0, change to something different
        if (args.Property.Name == nameof(Opacity) && (double?)args.NewValue <= 0.0d)
            button.Opacity = 0.5d;
    }

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        try
        {
            if (_canClose)
            {
                base.OnClosing(e);
                return;
            }
            
            if (DataContext is not MainWindowViewModel viewModel) return;
        
            e.Cancel = true; // Cancels the closing event

            if (!await viewModel.HandleSettingsSave()) return;
        
            _canClose = true;
            Close(); // Manually close
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString(), Constants.DebugError);
            _canClose = true;
            Close();
        }
    }
}
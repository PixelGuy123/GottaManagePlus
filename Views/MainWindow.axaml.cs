using System;
using Avalonia.Controls;
using GottaManagePlus.ViewModels;
using Serilog;

namespace GottaManagePlus.Views;

public partial class MainWindow : Window
{
    private bool _canClose = false;
    
    public MainWindow()
    {
        InitializeComponent();
    }

    // Main Window Close
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
            Log.Logger.Error("{exception}", ex);
            _canClose = true;
            Close();
        }
    }
}
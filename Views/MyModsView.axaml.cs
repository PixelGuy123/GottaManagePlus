using System;
using System.Diagnostics;
using Avalonia.Controls;

namespace GottaManagePlus.Views;

public partial class MyModsView : UserControl
{
    public MyModsView()
    {
        InitializeComponent();
        // When detached, we dispose the profiles view
        DetachedFromVisualTree += (_, _) => 
        {
            if (DataContext is IDisposable disposable)
                disposable.Dispose();
        };
        
        // No Mod Text Fontsize Update
        ModsScrollViewer.SizeChanged += ModsScrollViewerOnSizeChanged;
    }

    private void ModsScrollViewerOnSizeChanged(object? _, SizeChangedEventArgs e)
    {
        // If no text, ignore
        if (string.IsNullOrEmpty(NoModTextIndicator.Text))
        {
            NoModTextIndicator.FontSize = 1;
            return;
        }
        
        NoModTextIndicator.FontSize = Math.Min(110, 
            (e.NewSize.Width + e.NewSize.Height) / NoModTextIndicator.Text.Length
            );
    }
}
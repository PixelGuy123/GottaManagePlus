using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;

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
        
        // Update the Manifest Visualizer Properties
        ManifestVisualizer.PaneOpened += OnManifestVisualizerOnPaneOpened;
    }

    private void OnManifestVisualizerOnPaneOpened(object? sender, RoutedEventArgs e)
    {
        ManifestVisualizerDescription.FontSize = !string.IsNullOrEmpty(ManifestVisualizerDescription.Text) ? 
            Math.Max(12, 18d - ManifestVisualizerDescription.Text.Length * 0.15d) : 12d;
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
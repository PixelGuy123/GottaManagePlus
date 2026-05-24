using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using GottaManagePlus.Models.UI;
using GottaManagePlus.ViewModels;

namespace GottaManagePlus.Views;

public partial class ModSelectionDialogView : DisposableUserControl
{
    public ModSelectionDialogView()
    {
        InitializeComponent();
        SelectInstallModButton.IsCheckedChanged += OnSelectInstallModButtonOnIsCheckedChanged;
        ModList.Loaded += (_, _) => 
            ModList.FindDescendantOfType<ScrollViewer>()?.ScrollChanged += OnModListScrollViewerOnScrollChanged;
        // No Mod Text Fontsize Update
        ModVisualizer.SizeChanged += ModVisualizerOnSizeChanged;
    }

    private void ModVisualizerOnSizeChanged(object? sender, SizeChangedEventArgs e)
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

    private void OnSelectInstallModButtonOnIsCheckedChanged(object? sender, RoutedEventArgs args)
    {
        // TODO: Add localization here
        SelectInstallModButton.Content =
            $"{(SelectInstallModButton.IsChecked == true ? "Unselect" : "Select")} mod for Download";
    }
    
    // --- Collection Changed Update ---
    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        if (sender is not ObservableCollection<ModItem> collection) return;
        
        // TODO: Add localization here
        InstallButton.Content = $"Install ({collection.Count})";
    }
    
    protected override void OnInitialized()
    {
        base.OnInitialized();
        // Subscribe for property changes
        if (DataContext is ModSelectionDialogViewModel vm)
            vm.EnqueuedModsToInstall.CollectionChanged += OnCollectionChanged;
    }
    
    // --- Mod List's Scroll
    private void OnModListScrollViewerOnScrollChanged(object? sender, ScrollChangedEventArgs args)
    {
        // Check if we're at the bottom, with a small threshold
        if (sender is not ScrollViewer scrollViewer) return;
        
        // Check bottom
        var isAtBottom = Math.Abs(scrollViewer.Offset.Y - (scrollViewer.Extent.Height - scrollViewer.Viewport.Height)) < 1;

        // If it is at bottom and data context is right, load more
        if (isAtBottom && DataContext is ModSelectionDialogViewModel viewModel)
        {
            scrollViewer.Dispatcher.Invoke(viewModel.AddModBunch);
        }
    }
    
    // --- Dispose Pattern
    protected override void OnDispose()
    {
        base.OnDispose();
        if (DataContext is not ModSelectionDialogViewModel vm) return;
        
        // Dispose all ModItems in it.
        foreach (var mod in vm.AllMods)
            mod.Dispose();
    }
}

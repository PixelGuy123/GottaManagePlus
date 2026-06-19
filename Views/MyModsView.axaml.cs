using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;
using GottaManagePlus.Models;
using GottaManagePlus.Models.UI;
using GottaManagePlus.ViewModels;

namespace GottaManagePlus.Views;

public partial class MyModsView : DisposableUserControl
{
    public MyModsView()
    {
        InitializeComponent();

        // No Mod Text Fontsize Update
        ModsScrollViewer.SizeChanged += ModsScrollViewerOnSizeChanged;

        // Update the Manifest Visualizer Properties
        ManifestVisualizer.PaneOpened += OnManifestVisualizerOnPaneOpened;
    }

    // --- On Property Change Update ---
    private void OnVmOnPropertyChanged(object? _, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(MyModsViewModel.ManifestInPreview) or nameof(MyModsViewModel.ObservableMods))
            UpdateAllBrushes();
    }

    // --- Manifest Visualizer Font Update ---
    private void OnManifestVisualizerOnPaneOpened(object? sender, RoutedEventArgs e)
    {
        ManifestVisualizerDescription.FontSize = !string.IsNullOrEmpty(ManifestVisualizerDescription.Text)
            ? Math.Max(12, 18d - ManifestVisualizerDescription.Text.Length * 0.15d)
            : 12d;
    }

    // --- Popup Toggle ---
    private void CloseAddModFlyout(object? sender, RoutedEventArgs e) => AddModButton.Dispatcher.Post(AddModButton.Flyout!.Hide); // Waits one frame

    // --- Scroll Viewer Font Change ---
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

    // ===== Mod List Methods =====
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        // Trigger brush updates for ItemsRepeater.
        UpdateAllBrushes();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        // Subscribe for property changes
        if (DataContext is MyModsViewModel vm)
            vm.PropertyChanged += OnVmOnPropertyChanged;
    }

    private void UpdateAllBrushes()
    {
        // Get the current preview manifest from the ViewModel
        var vm = DataContext as MyModsViewModel;
        var previewManifest = vm?.ManifestInPreview;

        // Update PreviewBorderButton
        UpdateBrushesForClass("PreviewBorderButton", dc =>
            GetPreviewButtonBrush(dc, vm?.IsManifestPreviewOpen == true ? previewManifest : null));

        // Update ActivationToggleButton
        UpdateBrushesForClassTemplate("ActivationToggleButton", dc =>
            dc is ObservableModManifest manifest ? GetBoolBrush(manifest.IsActivated) : null);

        // Update ModNameBorder
        UpdateBrushesForClass("ModNameBorder", dc =>
            dc is ObservableModManifest manifest ? GetBoolBrush(manifest.InnerManifest.SupportsCurrentVersion) : null);
    }

    private void UpdateBrushesForClass(string className, Func<object?, IBrush?> brushResolver)

    {
        // Get all visual descendants of ModList and find those with the given class.
        var targets = ModList.GetVisualDescendants().OfType<ContentControl>()
            .Where(ctrl => ctrl.Classes.Contains(className));

        // Update targets
        foreach (var target in targets)
        {
            var brush = brushResolver(target.DataContext);
            if (brush == null) continue;
            target.BorderBrush = brush;
        }
    }

    private void UpdateBrushesForClassTemplate(string className, Func<object?, IBrush?> brushResolver)
    {
        // Get all visual descendants of ModList and find those with the given class.
        var targets = ModList.GetVisualDescendants().OfType<ContentControl>()
            .Where(ctrl => ctrl.Classes.Contains(className));

        // Update targets
        foreach (var target in targets)
        {
            var brush = brushResolver(target.DataContext);
            if (brush == null) continue;

            // By default, look for ContentPresenter
            var contentPresenter = target.FindDescendantOfType<Border>();
            contentPresenter?.BorderBrush = brush;
        }
    }

    private IBrush? GetPreviewButtonBrush(object? dataContext, object? previewManifest) =>
        // If the item is the one currently being previewed, highlight it.
        previewManifest != null && dataContext != null && dataContext == previewManifest
            ? this.FindResource("YctpBg") as IBrush
            : this.FindResource("YctpBg-Warning") as IBrush;

    private IBrush? GetBoolBrush(bool condition) =>
        condition
            ? this.FindResource("YctpBg") as IBrush
            : this.FindResource("YctpBg-Error") as IBrush;
}
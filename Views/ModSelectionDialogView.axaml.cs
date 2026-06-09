using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using GottaManagePlus.Models.UI;
using GottaManagePlus.ViewModels;

namespace GottaManagePlus.Views;

public partial class ModSelectionDialogView : DisposableUserControl
{
    #region Constants

    private const string HTML_DISPLAY_STYLESHEET = """
                                              <style>
                                                  * {
                                                      font-family: 'Segoe UI', Arial, sans-serif;
                                                      font-size: 14px;
                                                      color: #F0F0F0;
                                                      margin: 0;
                                                      padding: 8px;
                                                  }
                                                  h1, h2, h3 {
                                                      color: #6AB344;
                                                      margin-bottom: 2px;
                                                      font-weight: normal;
                                                  }
                                                  p, li, span, body {
                                                      color: #AAAAAA;
                                                      line-height: 1.4;
                                                  }
                                                  a {
                                                      color: #D3D304;
                                                      text-decoration: underline;
                                                      cursor: default;
                                                  }
                                                  a:hover {
                                                      color: #7AC44D;
                                                      text-decoration: underline;
                                                      cursor: pointer;
                                                  }
                                                  code, pre {
                                                      background-color: #2A2A2A;
                                                      color: #F0F0F0;
                                                      border: 1px solid #3A3A3A;
                                                      border-radius: 4px;
                                                  }
                                                  hr {
                                                      border-color: #3A3A3A;
                                                  }
                                                  
                                                  /* GameBanana-style formatting classes */
                                                  .RedColor { color: #FF4E4E; }
                                                  .BlueColor { color: #6CB1E1; }
                                                  .GreenColor { color: #6EE16C; }
                                                  .OrangeColor { color: #FF7238; }
                                                  .GreyColor { color: #999999; }
                                                  .PurpleColor { color: #FF5E9D; }
                                              </style>
                                              """;

    private const string InstallButtonFileUnavailability = "No file available to install this mod.";

    #endregion

    #region Constructor

    public ModSelectionDialogView()
    {
        InitializeComponent();
        DescriptionHtmlDisplay.BaseStylesheet = HTML_DISPLAY_STYLESHEET;
        
        // HTML Display events
        DescriptionHtmlDisplay.PropertyChanged += OnDescriptionHtmlDisplayOnPropertyChanged;
        DescriptionHtmlDisplay.Loaded += (_, _) => CleanUpHtmlDisplayText(DescriptionHtmlDisplay.Text);

        // Button events
        SelectInstallModButton.IsCheckedChanged += OnSelectInstallModButtonIsCheckedChanged;
        SelectInstallModButton.PropertyChanged += OnSelectInstallModButtonPropertyChanged;
        SelectInstallModButton.Loaded += OnSelectInstallModButtonIsCheckedChanged;

        // Selection events
        SelectVersionModBox.SelectionChanged += OnSelectVersionModBoxSelectionChanged;

        // Scroll event
        ModList.Loaded += (_, _) =>
            ModList.FindDescendantOfType<ScrollViewer>()?.ScrollChanged += OnModListScrollViewerScrollChanged;

        // Resize event for no-mod text
        ModVisualizer.SizeChanged += ModVisualizerOnSizeChanged;
    }


    #endregion
    
    #region Private Methods

    private void CleanUpHtmlDisplayText(string? text)
    {
        // Remove some characters that are not supported by the renderer.
        if (string.IsNullOrEmpty(text)) return;
            
        // Remove \n and \r
        text = text
            .Replace("\\n", "")
            .Replace("\\r", "");

        // If the string is different from the text, update
        if (text == DescriptionHtmlDisplay.Text) return;
        
        DescriptionHtmlDisplay.Text = text;
    }
    
    #endregion

    #region Event Handlers
    
    // HTML Display
    private void OnDescriptionHtmlDisplayOnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs args)
    {
        if (args.Property.Name != nameof(DescriptionHtmlDisplay.Text)) return;
        CleanUpHtmlDisplayText((string?)args.NewValue);
    }
    
    // Install Button Label Update
    private void DisplayInstallStringOrUnavailableString(string myString)
    {
        const string NoFileWarning = "This mod has no file available for the current version of the game.";
        if (SelectInstallModButton.IsEnabled)
        {
            ToolTip.SetTip(SelectInstallModButton, null);
            ToolTip.SetTip(SelectVersionModBox, null);
            SelectInstallModButton.Content = myString;
            return;
        }

        SelectInstallModButton.Content = InstallButtonFileUnavailability;
        ToolTip.SetTip(SelectInstallModButton, NoFileWarning);
        ToolTip.SetTip(SelectVersionModBox, NoFileWarning);
    }

    // --- Mod visualizer (no mod text font sizing) ---
    private void ModVisualizerOnSizeChanged(object? sender, SizeChangedEventArgs e) =>
        NoModTextIndicator.FontSize = Math.Min(45, (e.NewSize.Width + e.NewSize.Height) / (NoModTextIndicator.Text?.Length ?? 1));
    

    // --- Select/Install Mod button ---
    private void OnSelectInstallModButtonIsCheckedChanged(object? sender, RoutedEventArgs args)
    {
        // TODO: Add localization here
        DisplayInstallStringOrUnavailableString($"{(SelectInstallModButton.IsChecked == true ? "Unselect" : "Select")} mod for Download");
    }

    private void OnSelectInstallModButtonPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs args)
    {
        if (args.Property.Name != nameof(SelectInstallModButton.IsEnabled)) return;

        if (args.NewValue is false)
            SelectInstallModButton.Content = InstallButtonFileUnavailability;
    }

    // --- Version selection ---
    private void OnSelectVersionModBoxSelectionChanged(object? sender, SelectionChangedEventArgs args)
    {
        if (SelectInstallModButton?.IsChecked == false && DataContext is ModSelectionDialogViewModel vm)
            vm.ToggleModToInstallQueue(vm.SelectedMod!);
    }

    // --- Collection changed (enqueued mods) ---
    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        if (sender is not ObservableCollection<ModItem> collection) return;

        // TODO: Add localization here
        InstallButton.Content = $"Install ({collection.Count})";
    }

    // --- Mod list infinite scroll ---
    private void OnModListScrollViewerScrollChanged(object? sender, ScrollChangedEventArgs args)
    {
        if (sender is not ScrollViewer scrollViewer) return;

        var isAtBottom = Math.Abs(scrollViewer.Offset.Y - (scrollViewer.Extent.Height - scrollViewer.Viewport.Height)) < 1;

        if (isAtBottom && DataContext is ModSelectionDialogViewModel viewModel)
        {
            scrollViewer.Dispatcher.Invoke(viewModel.AddModBunch);
        }
    }

    // --- Search box lost focus ---
    private void InputElement_KeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not ModSelectionDialogViewModel vm || e.Key != Key.Enter || sender is not AutoCompleteBox box) return;
        
        e.Handled = true;
        box.Dispatcher.Invoke(async () => await vm.SearchByTerm(vm.SearchModText));
    }

    #endregion

    #region Overrides

    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (DataContext is ModSelectionDialogViewModel vm)
            vm.EnqueuedModsToInstall.CollectionChanged += OnCollectionChanged;
    }

    protected override void OnDispose()
    {
        base.OnDispose();

        if (DataContext is not ModSelectionDialogViewModel vm) return;

        foreach (var mod in vm.AllMods)
            mod.Dispose();
    }

    #endregion
}
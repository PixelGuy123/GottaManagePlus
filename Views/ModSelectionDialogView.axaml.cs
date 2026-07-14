/*
This file is part of GottaManagePlus (https://github.com/PixelGuy123/GottaManagePlus)

    Copyright (C) 2026 PixelGuy123

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.

*/

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Collections;
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

    private const string HtmlDisplayStylesheet = """
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
        DescriptionHtmlDisplay.BaseStylesheet = HtmlDisplayStylesheet;
        
        // HTML Display events
        DescriptionHtmlDisplay.PropertyChanged += OnDescriptionHtmlDisplayOnPropertyChanged;
        DescriptionHtmlDisplay.Loaded += (_, _) => CleanUpHtmlDisplayText(DescriptionHtmlDisplay.Text);

        // Button events
        SelectInstallModButton.IsCheckedChanged += OnSelectInstallModButtonIsCheckedChanged;
        SelectInstallModButton.PropertyChanged += OnSelectInstallModButtonPropertyChanged;
        SelectInstallModButton.Loaded += OnSelectInstallModButtonIsCheckedChanged;

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
        const string noFileWarning = "This mod has no file available for the current version of the game.";
        if (SelectInstallModButton.IsEnabled)
        {
            ToolTip.SetTip(SelectInstallModButton, null);
            ToolTip.SetTip(SelectVersionModBox, null);
            SelectInstallModButton.Content = myString;
            return;
        }

        SelectInstallModButton.Content = InstallButtonFileUnavailability;
        ToolTip.SetTip(SelectInstallModButton, noFileWarning);
        ToolTip.SetTip(SelectVersionModBox, noFileWarning);
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

    protected override void OnDispose()
    {
        base.OnDispose();

        if (DataContext is not ModSelectionDialogViewModel vm) return;

        foreach (var mod in vm.AllMods)
            mod.Dispose();
    }

    #endregion
}
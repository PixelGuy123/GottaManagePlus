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

using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GottaManagePlus.Models.DialogManagement;
using GottaManagePlus.Models.UI;

namespace GottaManagePlus.ViewModels;

public partial class ConfirmDialogViewModel : DialogViewModel
{
    public ConfirmDialogViewModel()
    {
        if (!Design.IsDesignMode) return;
    
        // Set defaults for the dialog
        Title = "Design Preview";
        Message = "This is how th";

        // Create a sample log container
        var sampleLogs = new LogContainer();
        sampleLogs.AddWarning("Low disk space", "Only 500 MB left on drive C:");
        sampleLogs.AddWarning("Deprecated API usage", "Method 'OldMethod' will be removed in v3.0");
        sampleLogs.AddError("Database connection failed", "Connection timeout after 30 seconds");
        sampleLogs.AddError("Missing configuration key", "Key 'ApiKey' not found in AppSettings.json");
        sampleLogs.AddInformation("User logged in", "User 'admin' logged in at 10:32 AM");
        sampleLogs.AddInformation("Background sync completed", "Synced 150 records successfully");
        
        // Assign the container
        LogContainer = sampleLogs;
    }
    
    [ObservableProperty]
    public partial string Title { get; set; } = "Confirm?";

    [ObservableProperty]
    public partial string Message { get; set; } = "Are you sure?";

    [ObservableProperty]
    public partial string ConfirmText { get; set; } = "Confirm";

    [ObservableProperty]
    public partial string CancelText { get; set; } = "Cancel";

    [ObservableProperty]
    public partial bool OnlyConfirmButton { get; set; }

    [ObservableProperty]
    public partial TextAlignment DescriptionAlignment { get; set; } = TextAlignment.Center;

    /// <summary>
    /// The log container holding categorized logs (Warnings, Errors, Information).
    /// When set, updates the TreeDataGrid source for hierarchical display.
    /// </summary>
    [ObservableProperty]
    public partial LogContainer? LogContainer { get; set; }

    /// <summary>
    /// The container for the logs tree. Provides the TreeDataGrid source.
    /// This is a simple data container, not a view model.
    /// </summary>
    [ObservableProperty]
    public partial LogsTreeContainer? LogsTreeContainer { get; set; }

    /// <summary>
    /// Gets the TreeDataGrid source for displaying logs hierarchically.
    /// Returns null if no LogContainer is set or if it has no logs.
    /// </summary>
    public HierarchicalTreeDataGridSource<LogTreeNode>? LogsTreeSource => LogsTreeContainer?.Source;

    protected override async Task<object?> OnShow(DialogContext? context)
    {
        var confirmDialogContext = ExpectContext<ConfirmDialogContext>(context);

        // Fill up data
        (Title, Message, ConfirmText, CancelText) =
            (confirmDialogContext.Title ?? Title,
                confirmDialogContext.Message ?? Message,
                confirmDialogContext.ConfirmText ?? ConfirmText,
                confirmDialogContext.CancelText ?? CancelText);
        DescriptionAlignment = confirmDialogContext.DescriptionAlignment;
        LogContainer = confirmDialogContext.LogContainer;
        
        return await WaitForCompletionAsync();
    }

    /// <summary>
    /// Partial method called when LogContainer property changes.
    /// Creates a new LogsTreeContainer instance to reflect the new log container.
    /// </summary>
    partial void OnLogContainerChanged(LogContainer? value)
    {
        // Create a new instance directly here
        LogsTreeContainer = new LogsTreeContainer();
        LogsTreeContainer.Prepare(value);
        LogsTreeSource?.ExpandAll();

        OnPropertyChanged(nameof(LogsTreeSource));
    }
}

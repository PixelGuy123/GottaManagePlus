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
using GottaManagePlus.Services;
using GottaManagePlus.Services.ExplorerServices;
using GottaManagePlus.Utils;

namespace GottaManagePlus.ViewModels;

public partial class LicenseDialogViewModel(FileLauncher fileLauncher, DialogService dialogService) : DialogViewModel
{
    private readonly DialogService _dialogService = dialogService;
    private readonly FileLauncher _fileLauncher = fileLauncher;
    public LicenseDialogViewModel() : this(null!, null!)
    {
        if (!Design.IsDesignMode) return;
    
        // Set defaults for the dialog
        Title = "Design Preview";
        Message = "This is how th";
    }
    
    [ObservableProperty]
    public partial string Title { get; set; } = "Confirm?";

    [ObservableProperty]
    public partial string Message { get; set; } = Constants.LicenseDialogDescription;

    [RelayCommand]
    public async Task OpenLicense()
    {
        if (!await _fileLauncher.TryLaunchFileInfo(new FileInfo(Constants.LicensePath)))
        {
            var context = new ConfirmDialogContext(
                Title: Constants.WarningDialog,
                Message: "Failed to open the license file!",
                CancelText: null
                );
            await _dialogService.ShowDialog<ConfirmDialogViewModel>(context);
        }
    }

    protected override async Task<object?> OnShow(DialogContext? context) =>
        await WaitForCompletionAsync();
}

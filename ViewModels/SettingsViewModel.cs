using System;

using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GottaManagePlus.Models.UI;
using GottaManagePlus.Services;
using GottaManagePlus.Services.ExplorerServices;
using GottaManagePlus.Services.GameEnvironmentServices;
using Serilog;

namespace GottaManagePlus.ViewModels;

public partial class SettingsViewModel : PageViewModel
{
    public SettingsViewModel() : base(PageNames.Settings)
    {
        // For designer
    }

    public SettingsViewModel(FilePicker filePicker, FileLauncher fileLauncher, SettingsService settingsService,
        GameEnvironmentController gameEnvironmentController, DialogService dialogService) : base(PageNames.Settings)
    {
        _filePicker = filePicker;
        _fileLauncher = fileLauncher;
        _settingsService = settingsService;
        _gameEnvironmentController = gameEnvironmentController;
        _dialogService = dialogService;
        CurrentSaveState = Models.UI.SaveState.InitializeState(settingsService);

        // Update this index
        UpdateNumberOfRowsPerIndex();
    }


    internal void DisplayGameFolderRequirementFolder()
    {
        // Display the warning dialog to remind the user
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            var dialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
            dialog.Prepare(true, Constants.WarningDialog, """
                      Looks like the BB+ folder is not set or is not valid.
                      If this is your first time using the tool, just select the executable of Baldi's Basics Plus inside Settings.
                      You cannot interact with "My Mods" section while under this condition.
                      """);
            return _dialogService.ShowDialog(dialog);
        });
    }
    
    // Private members
    private readonly FilePicker _filePicker = null!;
    private readonly FileLauncher _fileLauncher = null!;
    private readonly SettingsService _settingsService = null!;
    private readonly GameEnvironmentController _gameEnvironmentController = null!;
    private readonly DialogService _dialogService = null!;
    
    // Readonly collections
    public int[] PossibleRowsPerModStates { get; } = [4, 5, 6];
    
    // Observable Members
    [ObservableProperty] 
    private SaveState _currentSaveState = null!;
    [ObservableProperty] 
    private int _numberOfRowsPerModIndex;
    
    // Property changes
    partial void OnNumberOfRowsPerModIndexChanged(int value) => CurrentSaveState.NumberOfModsPerRow = PossibleRowsPerModStates[value];
    
    
    // Commands
    [RelayCommand]
    public async Task SetFilePathForPlusFolder()
    {
        var file = await _filePicker.OpenSingleFileAsync(title: "Select BB+ executable file:",
            preselectedPath: Constants.BaldiPlusFolderSteamPath);

        // If the file is null, leave
        if (file == null) return;

        // Get local path
        var fileLocalPath = file.TryGetLocalPath();

        // The path must obviously not be null
        if (!string.IsNullOrEmpty(fileLocalPath) && _gameEnvironmentController.IsEnvironmentValid) // Do not set path until confirmed by Save action
        {
            CurrentSaveState.GameExecutablePath = fileLocalPath;
            return;
        }

        var dialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
        dialog.Prepare(true, Constants.FailDialog, "Failed to locate the executable file or the directory, where this executable may be located, is invalid.");
        await _dialogService.ShowDialog(dialog);
        Log.Logger.Warning("Failed to set the folder!");
    }

    [RelayCommand]
    public async Task OpenExecutablePath()
    {
        if (!string.IsNullOrEmpty(CurrentSaveState.GameExecutablePath) && 
            !await _fileLauncher.OpenFileInfo(new FileInfo(CurrentSaveState.GameExecutablePath)))
        {
            var dialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
            dialog.Prepare(true, Constants.FailDialog, $"Failed to open the path to the executable due to an unknown error!");
            await _dialogService.ShowDialog(dialog);
        }
    }

    [RelayCommand]
    public async Task SaveState()
    {
        // Serialize saved state
        CurrentSaveState.UpdateSavedState();

        var settings = _settingsService.CurrentSettings;
        // Saving executable path
        if (!string.IsNullOrEmpty(CurrentSaveState.GameExecutablePath))
            settings.BaldiPlusExecutablePath = CurrentSaveState.GameExecutablePath;
        // Saving number of rows
        settings.NumberOfRowsPerMod = CurrentSaveState.NumberOfModsPerRow;

        // Saving executable path to the folder validator
        _gameEnvironmentController.SetNewEnvironment(settings.BaldiPlusExecutablePath);

        // Saving dialog
        var loadingDialog = _dialogService.GetDialog<LoadingDialogViewModel>();
        loadingDialog.Prepare("Saving settings...", "Saving...", (Delegate)_settingsService.Save);
        
        var status = await _dialogService.ShowDialog(loadingDialog);
        if (status) return;

        var confirmDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
        confirmDialog.Prepare(true, Constants.FailDialog, $"""
                       Failed to save the settings. You can try again.
                       If it doesn't work, you can try:
                       {Constants.SolutionFilePermissions}
                       """);
        await _dialogService.ShowDialog(confirmDialog);
    }

    [RelayCommand]
    public void CancelSaveState()
    {
        CurrentSaveState = CurrentSaveState.LastSavedState; // Revert to a previous reference
        
        // Update manually a few values
        UpdateNumberOfRowsPerIndex();
    }
    
    // Private members
    private void UpdateNumberOfRowsPerIndex()
    {
        for (var i = 0; i < PossibleRowsPerModStates.Length; i++)
        {
            if (PossibleRowsPerModStates[i] != CurrentSaveState.NumberOfModsPerRow) continue;
            
            NumberOfRowsPerModIndex = i;
            return;
        }

        NumberOfRowsPerModIndex = 0;
    }
}

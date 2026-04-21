using System;

using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GottaManagePlus.Models.UI;
using GottaManagePlus.Services;
using GottaManagePlus.Services.ExplorerServices;
using GottaManagePlus.Services.GameEnvironmentServices;
using GottaManagePlus.Utils;
using Serilog;

namespace GottaManagePlus.ViewModels;

public partial class SettingsViewModel : PageViewModel
{
    public SettingsViewModel() : base(PageNames.Settings)
    {
        // For designer
    }

    public SettingsViewModel(FilePicker filePicker, FileLauncher fileLauncher, SettingsService settingsService,
        GameEnvironmentController gameEnvironmentController, DialogService dialogService, ILogger logger) : base(PageNames.Settings)
    {
        if (Design.IsDesignMode) return;
        
        _filePicker = filePicker;
        _fileLauncher = fileLauncher;
        _settingsService = settingsService;
        _gameEnvironmentController = gameEnvironmentController;
        _dialogService = dialogService;
        _logger = logger;
        CurrentSaveState = Models.UI.SaveState.InitializeState(settingsService);

        // Update this index
        UpdateSettingsDisplays();
    }


    internal void DisplayGameFolderRequirementFolder()
    {
        // Display the warning dialog to remind the user
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await _dialogService.NotifyUser(Constants.WarningDialog, """
                      Looks like the BB+ folder is not set or is not valid.
                      If this is your first time using the tool, just select the executable of Baldi's Basics Plus inside Settings.
                      You cannot go to the Home Page while under this condition.
                      """);
        });
    }
    
    // Private members
    private readonly FilePicker _filePicker = null!;
    private readonly FileLauncher _fileLauncher = null!;
    private readonly SettingsService _settingsService = null!;
    private readonly GameEnvironmentController _gameEnvironmentController = null!;
    private readonly DialogService _dialogService = null!;
    private readonly ILogger _logger = null!;

    // Readonly collections
    public int[] PossibleRowsPerModStates { get; } = [4, 5, 6];
    
    // Observable Members
    [ObservableProperty] 
    private SaveState _currentSaveState = null!;
    [ObservableProperty] 
    private int _numberOfRowsPerModIndex;
    [ObservableProperty] 
    private string? _executablePath;
    
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
        if (!string.IsNullOrEmpty(fileLocalPath)) // Do not set path until confirmed by Save action
        {
            if (!_gameEnvironmentController.IsEnvironmentValid(fileLocalPath))
            {
                _logger.Warning("Failed to set '{FileLocalPath}' as executable path.", fileLocalPath);
                await _dialogService.NotifyUser("File Not Found", "The executable file is invalid or hasn't been found!");
                return;
            }
            CurrentSaveState.GameExecutablePath = fileLocalPath;
            ExecutablePath = fileLocalPath;
            return;
        }

        // Fail Dialog
        await _dialogService.NotifyUser(Constants.FailDialog, "Failed to locate the executable file or the directory, where this executable may be located, is invalid.");
        _logger.Warning("Failed to set the folder!");
    }

    [RelayCommand]
    public async Task OpenExecutablePath()
    {
        if (!string.IsNullOrEmpty(CurrentSaveState.GameExecutablePath) && 
            !_fileLauncher.OpenFileInfo(new FileInfo(CurrentSaveState.GameExecutablePath)))
        {
            await _dialogService.NotifyUser(Constants.FailDialog, "Failed to open the path to the executable due to an unknown error!");
        }
    }

    [RelayCommand]
    public async Task SaveState()
    {
        // Serialize saved state
        CurrentSaveState.UpdateSavedState();

        // Update through mutable settings
        _settingsService.Update(settings =>
        {
            // Saving executable path
            if (!string.IsNullOrEmpty(CurrentSaveState.GameExecutablePath))
                settings.BaldiPlusExecutablePath = CurrentSaveState.GameExecutablePath;
            // Saving number of rows
            settings.NumberOfRowsPerMod = CurrentSaveState.NumberOfModsPerRow;

            // Saving executable path to the folder validator
            _gameEnvironmentController.SetNewEnvironment(settings.BaldiPlusExecutablePath);
        });
        
        // Saving dialog
        await _dialogService.GenerateLoadingProcess(
            $"""
             Failed to save the settings. You can try again.
             If it doesn't work, you can try:
             {Constants.CommonIssuesSolution}
             """,
            null,
            "Saving settings...", "Saving...", (Delegate)_settingsService.SaveAsync);
    }

    [RelayCommand]
    public void CancelSaveState()
    {
        CurrentSaveState = CurrentSaveState.LastSavedState; // Revert to a previous reference
        
        // Update manually a few values
        UpdateSettingsDisplays();
    }
    
    // Private members
    private void UpdateSettingsDisplays()
    {
        // Update number oof rows
        NumberOfRowsPerModIndex = 0;
        for (var i = 0; i < PossibleRowsPerModStates.Length; i++)
        {
            if (PossibleRowsPerModStates[i] != CurrentSaveState.NumberOfModsPerRow) continue;
            
            NumberOfRowsPerModIndex = i;
            break;
        }
        
        // Update Executable's Path
        ExecutablePath = CurrentSaveState.GameExecutablePath;
    }
}

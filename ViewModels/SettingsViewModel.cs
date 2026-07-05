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
        GameEnvironmentController gameEnvironmentController, DialogService dialogService, ApplicationManager appManager,
        ILogger logger, DirectoryLauncher directoryLauncher) : base(PageNames.Settings)
    {
        if (Design.IsDesignMode) return;

        _filePicker = filePicker;
        _fileLauncher = fileLauncher;
        _settingsService = settingsService;
        _gameEnvironmentController = gameEnvironmentController;
        _dialogService = dialogService;
        _appManager = appManager;
        _logger = logger;
        _directoryLauncher = directoryLauncher;
        CurrentSaveState = Models.UI.SaveState.InitializeState(settingsService);

        // Update this index
        UpdateSettingsDisplays();
        
        // Check if Unity log folder is available
        UnityLogFolderPath = Constants.GetUnityLogFolderPath();
        CanOpenUnityLogFolder = !string.IsNullOrEmpty(UnityLogFolderPath);
    }


    internal void DisplayGameFolderRequirementFolder()
    {
        // Display the warning dialog to remind the user
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await _dialogService.NotifyUser(Constants.WarningDialog, """
                                                                     The Baldi's Basics Plus (BB+) folder is not set.
                                                                     If this is your first time using the tool, proceed by selecting the game's executable file.
                                                                     You cannot go to the Home Page while the path is unset.
                                                                     """);
        });
    }

    // Private members
    private readonly FilePicker _filePicker = null!;
    private readonly FileLauncher _fileLauncher = null!;
    private readonly SettingsService _settingsService = null!;
    private readonly GameEnvironmentController _gameEnvironmentController = null!;
    private readonly DialogService _dialogService = null!;
    private readonly ApplicationManager _appManager = null!;
    private readonly ILogger _logger = null!;
    private readonly DirectoryLauncher _directoryLauncher = null!;

    // Readonly collections
    public int[] PossibleRowsPerModStates { get; } = [4, 5, 6];
    public string[] PossibleThemes { get; } = ["Light", "Dark", "System Default"];

    // Observable Members
    [ObservableProperty] public partial SaveState CurrentSaveState { get; set; } = null!;

    [ObservableProperty] public partial int NumberOfRowsPerModIndex { get; set; }

    [ObservableProperty] public partial string? ExecutablePath { get; set; }

    [ObservableProperty] public partial string? Theme { get; set; } = "Light";

    [ObservableProperty] public partial bool CancelOnSecurityIssues { get; set; }

    [ObservableProperty] public partial string? UnityLogFolderPath { get; set; }

    [ObservableProperty] public partial bool CanOpenUnityLogFolder { get; set; }

    // Property changes
    partial void OnNumberOfRowsPerModIndexChanged(int value) =>
        CurrentSaveState.NumberOfModsPerRow = PossibleRowsPerModStates[value];

    partial void OnThemeChanged(string? value) { CurrentSaveState.Theme = value!; } // Update the settings to show the new theme

    partial void OnCancelOnSecurityIssuesChanged(bool value) => CurrentSaveState.CancelOnSecurityIssues = value;


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
    public async Task OpenUnityLogFolder()
    {
        if (string.IsNullOrEmpty(UnityLogFolderPath))
        {
            await _dialogService.NotifyUser(Constants.WarningDialog, 
                "The Unity log folder for Baldi's Basics Plus could not be found.\nThis may happen if the game hasn't been launched yet or on Linux if the folder doesn't exist.");
            return;
        }

        try
        {
            var directoryInfo = new DirectoryInfo(UnityLogFolderPath);
            if (!directoryInfo.Exists)
            {
                await _dialogService.NotifyUser(Constants.WarningDialog, 
                    "The Unity log folder does not exist. This may happen if the game hasn't been launched yet.");
                return;
            }

            if (!await _directoryLauncher.OpenDirectoryInfo(directoryInfo))
            {
                await _dialogService.NotifyUser(Constants.FailDialog, "Failed to open the Unity log folder due to an unknown error!");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to open Unity log folder at '{UnityLogFolderPath}'.", UnityLogFolderPath);
            await _dialogService.NotifyUser(Constants.FailDialog, $"Failed to open the Unity log folder: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task SaveState()
    {
        // Gets the snapshot for the end script.
        var previousSettings = _settingsService.CurrentSettings;
        
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
            // Saving theme
            settings.Theme = CurrentSaveState.Theme;
            // Saving cancel on security issues setting
            settings.CancelOnSecurityIssues = CurrentSaveState.CancelOnSecurityIssues;

            // Saving executable path to the folder validator
            _gameEnvironmentController.SetNewEnvironment(settings.BaldiPlusExecutablePath);
        });
        
        // Saving dialog
        await _dialogService.GenerateBooleanLoadingProcess(
            $"""
             Failed to save the settings. You can try again.
             If it doesn't work, you can try:
             {Constants.CommonIssuesSolution}
             """,
            null,
            "Saving settings...", "Saving...", (Delegate)_settingsService.SaveAsync);
        
        // Quit if necessary dialog
        if (CurrentSaveState.HasChangesThatRequiresRestart(previousSettings) && await _dialogService.PromptUserQuestion("Restart Required", "To see the changes, you'll need to restart the application.\nYou can still use the mod manager normally.", DialogServiceUtils.QuestionAnswerType.ProceedOrCancel))
            _appManager.Exit();
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
        
        // Update Theme
        Theme = CurrentSaveState.Theme;
        
        // Update CancelOnSecurityIssues
        CancelOnSecurityIssues = CurrentSaveState.CancelOnSecurityIssues;
    }
}

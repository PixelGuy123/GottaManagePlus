using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GottaManagePlus.Interfaces;
using GottaManagePlus.Models;
using GottaManagePlus.Services;

namespace GottaManagePlus.ViewModels;

public partial class SettingsViewModel : PageViewModel
{
    public SettingsViewModel() : base(PageNames.Settings)
    {
        // For designer
    }

    public SettingsViewModel(FilesService filesService, SettingsService settingsService,
        PlusFolderViewer gameFolderViewer, DialogService dialogService) : base(PageNames.Settings)
    {
        _filesService = filesService;
        _settingsService = settingsService;
        _dialogService = dialogService;
        CurrentSaveState = Models.SaveState.InitializeState(settingsService);
        _gameFolderViewer = gameFolderViewer;
    }


    internal void DisplayGameFolderRequirementFolder()
    {
        // Display the warning dialog to remind the user
        Dispatcher.UIThread.InvokeAsync(() => _dialogService.ShowDialog(new ConfirmDialogViewModel(true, true)
        {
            Title = Constants.WarningDialog,
            Message = """
                      Looks like the BB+ folder is not set or is not valid.
                      If this is your first time using the tool, just select the executable of Baldi's Basics Plus inside Settings.
                      You cannot interact with "My Mods" section while under this condition.
                      """
        }));
    }
    
    // Private members
    private readonly FilesService _filesService = null!;
    private readonly SettingsService _settingsService = null!;
    private readonly DialogService _dialogService = null!;
    private readonly IGameFolderViewer _gameFolderViewer = null!;
    
    // Observable Members
    [ObservableProperty] 
    private SaveState _currentSaveState = null!;
    
    // Commands
    [RelayCommand]
    public async Task SetFilePathForPlusFolder()
    {
            var file = await _filesService.OpenFileAsync(title: "Select the game\'s executable.",
                preselectedPath: Constants.BaldiPlusFolderSteamPath);

            // If the file is null, leave
            if (file == null) return;

            // Get local path
            var fileLocalPath = file.TryGetLocalPath();

            // The path must obviously not be null
            if (!string.IsNullOrEmpty(fileLocalPath) &&
                _gameFolderViewer.ValidateFolder(fileLocalPath,
                    setPathIfTrue: false)) // Do not set path until confirmed by Save action
            {
                CurrentSaveState.GameExecutablePath = fileLocalPath;
                return;
            }

            await _dialogService.ShowDialog(new ConfirmDialogViewModel(true)
            {
                Title = Constants.FailDialog,
                Message =
                    "Failed to locate the executable file or the directory, where this executable may be located, is invalid."
            });
            Debug.WriteLine("Failed to set the folder!", Constants.DebugWarning);
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

        // Saving executable path to the folder validator
        _gameFolderViewer.ValidateFolder(settings.BaldiPlusExecutablePath);

        // Saving dialog
        var loadingDialog = new LoadingDialogViewModel(_settingsService.Save)
        {
            Title = "Saving settings...",
            Status = "Saving..."
        };
        var status = await _dialogService.ShowLoadingDialog(loadingDialog);
        if (status) return;

        await _dialogService.ShowDialog(new ConfirmDialogViewModel(true)
        {
            Title = Constants.FailDialog,
            Message = $"""
                       Failed to save the settings. You can try again.
                       If it doesn't work, you can try:
                       {Constants.SolutionFilePermissions}
                       """
        });
    }

    [RelayCommand]
    public void CancelSaveState() => CurrentSaveState = CurrentSaveState.LastSavedState; // Revert to a previous reference
}

using System.Threading.Tasks;
using Avalonia.Platform.Storage;
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

    public SettingsViewModel(FilesService filesService, SettingsService settingsService, IGameFolderViewer gameFolderViewer) : base(PageNames.Settings)
    {
        _filesService = filesService;
        _settingsService = settingsService;
        CurrentSaveState = Models.SaveState.InitializeState(settingsService);
        _gameFolderViewer = gameFolderViewer;
    }
    
    // Private members
    private readonly FilesService _filesService = null!;
    private readonly SettingsService _settingsService = null!;
    private readonly IGameFolderViewer _gameFolderViewer = null!;
    
    // Observable Members
    [ObservableProperty] 
    private SaveState _currentSaveState = null!;
    
    // Commands
    [RelayCommand]
    public async Task SetFilePathForPlusFolder()
    {
        var file = await _filesService.OpenFileAsync();
        
        // If the file is null, leave
        if (file == null) return;
        
        // Get local path
        var fileLocalPath = file.TryGetLocalPath();

        if (!string.IsNullOrEmpty(fileLocalPath) &&
            _gameFolderViewer.ValidateFolder(fileLocalPath, setPathIfTrue: false)) // Do not set path until confirmed by Save action
        {
            CurrentSaveState.GameExecutablePath = fileLocalPath;
            return;
        }
        
        // TODO: Display dialog for failure
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
        
        // TODO: Add and save bookmark
        
        // TODO: Display Saving... Popup
        var status = await _settingsService.Save();
        if (status) return;
        
        // TODO: Display save failed POPUP
    }

    [RelayCommand]
    public void CancelSaveState() => CurrentSaveState = CurrentSaveState.LastSavedState; // Revert to a previous reference
}

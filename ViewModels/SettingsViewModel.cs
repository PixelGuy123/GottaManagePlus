using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GottaManagePlus.Models;
using GottaManagePlus.Services;

namespace GottaManagePlus.ViewModels;

public partial class SettingsViewModel : PageViewModel
{
    public SettingsViewModel() : base(PageNames.Settings)
    {
        // For designer
    }

    public SettingsViewModel(FilesService filesService, SettingsService settingsService) : base(PageNames.Settings)
    {
        _filesService = filesService;
        _settingsService = settingsService;
        CurrentSaveState = Models.SaveState.InitializeState(settingsService);
    }
    
    // Private members
    
    private readonly FilesService _filesService = null!;
    private readonly SettingsService _settingsService = null!;
    // private readonly IPlusService = null!; // Service to communicate with the BB+ Folder in general
    
    // Observable Members
    [ObservableProperty] 
    private SaveState _currentSaveState = null!;
    
    // Commands
    [RelayCommand]
    public async Task SetFilePathForPlusFolder()
    {
        // TODO: Communicate with the IPlusService to validate the Plus folder & bookmark it
        var folder = await _filesService.OpenFolderAsync();
        
        // TEMP ASSIGNMENT
        if (folder != null)
            CurrentSaveState.GameFolderPath = folder.Path.AbsolutePath;
    }

    [RelayCommand]
    public async Task SaveState()
    {
        CurrentSaveState.UpdateSavedState();
        
        // Actually save settings
        var settings = _settingsService.CurrentSettings;
        if (!string.IsNullOrEmpty(CurrentSaveState.GameFolderPath))
            settings.BaldiPlusFilePath = CurrentSaveState.GameFolderPath;
        // TODO: Save bookmark
        
        // TODO: Display Saving... Popup
        var status = await _settingsService.Save();
        if (status) return;
        
        // TODO: Display save failed POPUP
    }

    [RelayCommand]
    public void CancelSaveState() => CurrentSaveState = CurrentSaveState.LastSavedState; // Revert to a previous reference
}

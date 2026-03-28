using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GottaManagePlus.Factories;
using GottaManagePlus.Interfaces;
using GottaManagePlus.Services;
using System;
using GottaManagePlus.Services.PlusFolderServices;
using GottaManagePlus.Services.ProfileServices;
using GottaManagePlus.Utils;

namespace GottaManagePlus.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDialogProvider
{
    private readonly PageFactory? _pageFactory;
    private readonly DialogService _dialogService = null!;
    private readonly PlusFolderDb _plusFolderDb = null!;
    private readonly SettingsService _settingsService = null!;
    private readonly ProfileStorage _profileStorage = null!;
    private readonly ProfileManager _profileManager = null!;
    
    [ObservableProperty] 
    private bool _executablePathSet;
    
    [ObservableProperty]
    private PageViewModel? _currentPage;

    [ObservableProperty] 
    private DialogViewModel? _dialog;
    
    [RelayCommand]
    public void GoToHome()
    {
        if (ExecutablePathSet)
            GoTo<MyModsViewModel>();
    }

    [RelayCommand]
    public void GoToSettings() => GoTo<SettingsViewModel>();


    [RelayCommand]
    public async Task RevealAboutSection() => await RevealAboutSectionUi();
    
    // For Designer only
    public MainWindowViewModel()
    {
        if (!Design.IsDesignMode) return;
        
        CurrentPage = new MyModsViewModel(); // Default page
        ExecutablePathSet = true;
    }
    
    // Constructor
    public MainWindowViewModel(
        PageFactory pageFactory, 
        DialogService dialogService, 
        PlusFolderDb plusFolderDb, 
        SettingsService settingsService, 
        ProfileStorage profileStorage,
        ProfileManager profileManager)
    {
        _pageFactory = pageFactory;
        _dialogService = dialogService;
        _plusFolderDb = plusFolderDb;
        _settingsService = settingsService;
        _profileStorage = profileStorage;
        _profileManager = profileManager;
        
        // Cache on start
        _dialogService.GetDialog<AppInfoDialogViewModel>();

        _settingsService.OnSaveSettings += UpdateExecutablePathValidation;

        // If the executable is all set, then the manager should visualize the mods
        if (_plusFolderDb.ValidateGameFolder(_settingsService.CurrentSettings.BaldiPlusExecutablePath))
            CurrentPage = _pageFactory.GetPageViewModel<MyModsViewModel>();
        else // Otherwise, force the user to set that manually
        {
            var settings = _pageFactory.GetPageViewModel<SettingsViewModel>();
            CurrentPage = settings;
            
            // Display that one needed dialog
            settings.DisplayGameFolderRequirementFolder();
        }

        UpdateExecutablePathValidation();
    }
    
    // public methods
    public async Task<bool> HandleSettingsSave()
    {
        // Loading dialog for saving active profile
        var loadingDialog = _dialogService.GetDialog<LoadingDialogViewModel>();
        loadingDialog.Prepare("Saving current active profile...", 
            _profileManager.ActiveProfile, null, 
            (Delegate)_profileStorage.SaveEnvironmentDataToProfile);

        if (_profileStorage.ProfileMemoryDb.IsEmpty || // Or, if there are no profiles to save, skip this dialog
            await _dialogService.ShowDialog(loadingDialog))
        {
            // Then, one for saving settings
            loadingDialog = _dialogService.GetDialog<LoadingDialogViewModel>();
            loadingDialog.Prepare("Saving settings...", null, (Delegate)_settingsService.Save);
            
            if (await _dialogService.ShowDialog(loadingDialog))
                return true;
        }
        
        // If one of them fail, go here
        var confirmViewModel = _dialogService.GetDialog<ConfirmDialogViewModel>();
        confirmViewModel.Prepare(
            null,
            "Failed to save settings!",
            "Are you sure you still want to leave the application without saving changes?",
            "Yes",
            "No"
        );

        // Show confirmation dialog
        await _dialogService.ShowDialog(confirmViewModel);
        return confirmViewModel.Confirmed;
    }
    
    // Private methods
    private void GoTo<TVm>()
        where TVm : PageViewModel
    {
        if (CurrentPage is not TVm)
            CurrentPage = _pageFactory!.GetPageViewModel<TVm>();
    }
    private async Task RevealAboutSectionUi()
    {
        var dialog = _dialogService.GetDialog<AppInfoDialogViewModel>();
        dialog.Prepare();
        await _dialogService.ShowDialog(dialog);
    }
    
    private void UpdateExecutablePathValidation() => 
        ExecutablePathSet = 
            _plusFolderDb.ValidateGameFolder(_settingsService.CurrentSettings.BaldiPlusExecutablePath, 
                                            updateDatabaseIfPathIsValid: false);
}

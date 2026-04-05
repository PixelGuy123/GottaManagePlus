using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GottaManagePlus.Factories;
using GottaManagePlus.Interfaces;
using GottaManagePlus.Services;
using System;
using GottaManagePlus.Models;
using GottaManagePlus.Models.GameEnvironments;
using GottaManagePlus.Services.GameEnvironmentServices;
using GottaManagePlus.Services.ProfileServices;
using GottaManagePlus.Utils;

namespace GottaManagePlus.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDialogProvider
{
    private readonly PageFactory? _pageFactory;
    private readonly DialogService _dialogService = null!;
    private readonly SettingsService _settingsService = null!;
    private readonly ProfileManager _profileManager = null!;
    private readonly ProfileRepository _profileRepository = null!;
    
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
        GameEnvironmentController gameEnvironmentController, 
        SettingsService settingsService,
        ProfileRepository profileRepository,
        ProfileManager profileManager)
    {
        _pageFactory = pageFactory;
        _dialogService = dialogService;
        _settingsService = settingsService;
        _profileManager = profileManager;
        _profileRepository = profileRepository;
        
        // Cache on start
        _dialogService.GetDialog<AppInfoDialogViewModel>();

        // Add the save settings callback to ensure the path is always updated
        _settingsService.OnSaveSettings += () => 
            ExecutablePathSet = gameEnvironmentController.IsEnvironmentValid;

        // If the executable is all set, then the manager should visualize the mods
        gameEnvironmentController.SetNewEnvironment(_settingsService.CurrentSettings.BaldiPlusExecutablePath);
        if (gameEnvironmentController.CurrentEnvironment != null)
        {
            CurrentPage = _pageFactory.GetPageViewModel<MyModsViewModel>();
            ExecutablePathSet = true;
        }
        else // Otherwise, force the user to set that manually
        {
            var settings = _pageFactory.GetPageViewModel<SettingsViewModel>();
            CurrentPage = settings;
            
            // Display that one needed dialog
            settings.DisplayGameFolderRequirementFolder();
        }
    }
    
    // public methods
    public async Task<bool> HandleSettingsSave()
    {
        // Loading dialog for saving active profile
        var loadingDialog = _dialogService.GetDialog<LoadingDialogViewModel>();
        loadingDialog.Prepare("Saving current active profile...", 
            _profileManager.ActiveProfile, null, 
            (Delegate)_profileManager.SaveActiveProfile);

        if (_profileRepository.IsEmpty || // Or, if there are no profiles to save, skip this dialog
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
}

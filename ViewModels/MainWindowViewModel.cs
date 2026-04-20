using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GottaManagePlus.Factories;
using GottaManagePlus.Interfaces;
using GottaManagePlus.Services;
using System;
using GottaManagePlus.Models;
using GottaManagePlus.Services.GameEnvironmentServices;
using GottaManagePlus.Services.ProfileServices;
using GottaManagePlus.Utils;

namespace GottaManagePlus.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDialogProvider
{
    private readonly PageFactory? _pageFactory;
    private readonly DialogService _dialogService = null!;
    private readonly GameEnvironmentController _gameEnvironmentController = null!;
    private readonly SettingsService _settingsService = null!;
    private readonly ProfileManager _profileManager = null!;
    private readonly ProfileRepository _profileRepository = null!;
    
    // Public Getters
    public bool ExecutablePathSet => Design.IsDesignMode || _gameEnvironmentController.IsEnvironmentValid;
    
    
    // Observable Properties
    [ObservableProperty]
    private PageViewModel? _currentPage;

    [ObservableProperty] 
    private DialogViewModel? _dialog;

    [ObservableProperty]
    private bool _sideMenuOpen = Design.IsDesignMode;

    [RelayCommand]
    public void ToggleSideMenu() => SideMenuOpen = !SideMenuOpen;
    
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
        _gameEnvironmentController = gameEnvironmentController;
        _settingsService = settingsService;
        _profileManager = profileManager;
        _profileRepository = profileRepository;
        
        // Cache on start
        _dialogService.GetDialog<AppInfoDialogViewModel>();
        
        // **** Settings Setup ****

        // If the executable is all set, then the manager should visualize the mods
        gameEnvironmentController.SetNewEnvironment(_settingsService.CurrentSettings.BaldiPlusExecutablePath);
        if (gameEnvironmentController.CurrentEnvironment != null)
        {
            CurrentPage = _pageFactory.GetPageViewModel<MyModsViewModel>();
        }
        else // Otherwise, force the user to set that manually
        {
            var settings = _pageFactory.GetPageViewModel<SettingsViewModel>();
            CurrentPage = settings;
            
            // Display that one needed dialog
            settings.DisplayGameFolderRequirementFolder();
        }
        
        // Update the Profile Selection for the settings.
        _profileManager.OnActiveProfileUpdate += 
            newProfile => _settingsService.CurrentSettings.CurrentProfileSet = 
                newProfile?.Name ?? ProfileMetadata.DefaultName;
    }
    
    // public methods
    public async Task<bool> HandleSettingsSave()
    {
        // Loading dialog for saving active profile
        if (!(_profileRepository.IsEmpty || // Or, if there are no profiles to save, skip this dialog
            await _dialogService.GenerateLoadingProcess(
                "Failed to save the active profile!",
                null,
                "Saving current active profile...", null,
                (Delegate)_profileManager.SaveActiveProfile)))
            return await _dialogService.PromptUserQuestion(
                "Failed to save settings!",
                "Are you sure you still want to leave the application without saving changes?");

        // Then, one for saving settings
        return await _dialogService.GenerateLoadingProcess(
            "Failed to save the settings. You can try again.",
            null,
            "Saving settings...", null, (Delegate)_settingsService.Save
        );
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

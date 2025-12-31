using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GottaManagePlus.Factories;
using GottaManagePlus.Interfaces;
using GottaManagePlus.Services;
using System;
using Avalonia.Dialogs;

namespace GottaManagePlus.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDialogProvider
{
    private readonly PageFactory? _pageFactory;
    private readonly DialogService _dialogService = null!;
    private readonly IGameFolderViewer _gameFolderViewer = null!;
    private readonly SettingsService _settingsService = null!;
    private readonly IProfileProvider _profileProvider = null!;
    
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
        PlusFolderViewer gameFolderViewer, 
        SettingsService settingsService, 
        ProfileProvider profileProvider)
    {
        _pageFactory = pageFactory;
        _dialogService = dialogService;
        _gameFolderViewer = gameFolderViewer;
        _settingsService = settingsService;
        _profileProvider = profileProvider;
        
        // Cache on start
        _dialogService.GetDialog<AppInfoDialogViewModel>();

        _settingsService.OnSaveSettings += UpdateExecutablePathValidation;

        // If the executable is all set, then the manager should visualize the mods
        if (_gameFolderViewer.ValidateFolder(_settingsService.CurrentSettings.BaldiPlusExecutablePath, setPathIfTrue: true))
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
        loadingDialog.Prepare("Saving current active profile...", null, (Delegate)_profileProvider.SaveActiveProfile);

        if (_profileProvider.GetLoadedProfiles().Count == 0 || // Or, if there are no profiles to save, skip this dialog
            await _dialogService.ShowLoadingDialog(loadingDialog))
        {
            // Then, one for saving settings
            loadingDialog = _dialogService.GetDialog<LoadingDialogViewModel>();
            loadingDialog.Prepare("Saving settings...", null, (Delegate)_settingsService.Save);
            
            if (await _dialogService.ShowLoadingDialog(loadingDialog))
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
            _gameFolderViewer.ValidateFolder(_settingsService.CurrentSettings.BaldiPlusExecutablePath, 
                                            setPathIfTrue: false);
}

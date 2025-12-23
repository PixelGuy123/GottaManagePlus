using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GottaManagePlus.Factories;
using GottaManagePlus.Interfaces;
using GottaManagePlus.Services;

namespace GottaManagePlus.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDialogProvider
{
    private readonly PageFactory? _pageFactory;
    private readonly DialogService _dialogService = null!;
    private readonly IGameFolderViewer _gameFolderViewer = null!;
    private readonly SettingsService _settingsService = null!;
    
    
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
            CurrentPage = _pageFactory!.GetPageViewModel<MyModsViewModel>();
    }

    [RelayCommand]
    public void GoToSettings()
    {
        CurrentPage = _pageFactory!.GetPageViewModel<SettingsViewModel>();
    }

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
    public MainWindowViewModel(PageFactory pageFactory, DialogService dialogService, PlusFolderViewer gameFolderViewer, SettingsService settingsService)
    {
        _pageFactory = pageFactory;
        _dialogService = dialogService;
        _gameFolderViewer = gameFolderViewer;
        _settingsService = settingsService;

        _settingsService.OnSaveSettings += UpdateExecutablePathValidation;

        // If the executable is all set, then the manager should visualize the mods
        if (_gameFolderViewer.ValidateFolder(_settingsService.CurrentSettings.BaldiPlusExecutablePath, setPathIfTrue: true))
            CurrentPage = _pageFactory.GetPageViewModel<MyModsViewModel>();
        else // Otherwise, force the user to set that manually
        {
            // TODO: Add dialog to explicitly ask the user to set a game executable path
            CurrentPage = _pageFactory.GetPageViewModel<SettingsViewModel>();
        }
    }
    
    // Private methods
    private async Task RevealAboutSectionUi() => await _dialogService.ShowDialog(new AppInfoDialogViewModel());
    
    private void UpdateExecutablePathValidation() => 
        ExecutablePathSet = 
            _gameFolderViewer.ValidateFolder(_settingsService.CurrentSettings.BaldiPlusExecutablePath, 
                                            setPathIfTrue: false);
}

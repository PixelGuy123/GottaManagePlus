using System;
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

    [ObservableProperty]
    private PageViewModel? _currentPage;

    [ObservableProperty] 
    private DialogViewModel? _dialog;
    
    [RelayCommand]
    public void GoToHome()
    {
        CurrentPage = _pageFactory!.GetPageViewModel(PageNames.Home);
    }

    [RelayCommand]
    public void GoToSettings()
    {
        CurrentPage = _pageFactory!.GetPageViewModel(PageNames.Settings);
    }

    [RelayCommand]
    public async Task RevealAboutSection() => await RevealAboutSectionUi();
    
    // For Designer only
    public MainWindowViewModel()
    {
        if (!Design.IsDesignMode) return;
        
        CurrentPage = new MyModsViewModel(); // Default page
    }
    
    // Constructor
    public MainWindowViewModel(PageFactory pageFactory, DialogService dialogService)
    {
        _pageFactory = pageFactory;
        _dialogService = dialogService;
        CurrentPage = _pageFactory.GetPageViewModel(PageNames.Home);
    }
    
    // Private methods
    private async Task RevealAboutSectionUi() => await _dialogService.ShowDialog(new AppInfoDialogViewModel());
}

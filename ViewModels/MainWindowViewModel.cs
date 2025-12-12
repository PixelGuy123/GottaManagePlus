using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GottaManagePlus.Factories;
using GottaManagePlus.Interfaces;

namespace GottaManagePlus.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDialogProvider
{
    private readonly PageFactory? _pageFactory;

    [ObservableProperty]
    private PageViewModel? _currentPage;

    [ObservableProperty] 
    private DialogViewModel? _dialog;
    
    // For Designer only
    public MainWindowViewModel() { }
    
    // Constructor
    public MainWindowViewModel(PageFactory pageFactory)
    {
        _pageFactory = pageFactory;
        CurrentPage = _pageFactory.GetPageViewModel(PageNames.Home);
    }

    [RelayCommand]
    private void GoToHome()
    {
        CurrentPage = _pageFactory!.GetPageViewModel(PageNames.Home);
    }

    [RelayCommand]
    private void GoToSettings()
    {
        CurrentPage = _pageFactory!.GetPageViewModel(PageNames.Settings);
    }
}

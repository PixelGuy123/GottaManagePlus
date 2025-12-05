using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GottaManagePlus.Factories;

namespace GottaManagePlus.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly PageFactory? _pageFactory;

    [ObservableProperty]
    private PageViewModel? _currentPage;

    // Parameterless constructor for previewer purposes
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

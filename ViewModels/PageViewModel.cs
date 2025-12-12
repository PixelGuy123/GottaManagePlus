using CommunityToolkit.Mvvm.ComponentModel;

namespace GottaManagePlus.ViewModels;

public partial class PageViewModel(PageNames pageName) : ViewModelBase
{
    [ObservableProperty]
    private PageNames _pageName = pageName;
}
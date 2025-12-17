using CommunityToolkit.Mvvm.ComponentModel;

namespace GottaManagePlus.ViewModels;

public abstract partial class PageViewModel(PageNames pageName) : ViewModelBase
{
    protected PageViewModel(PageNames pageName, ViewModelBase viewModelBase) : this(pageName)
    {
        _sideMenuBase = viewModelBase;
    }
    
    [ObservableProperty]
    private PageNames _pageName = pageName;

    [ObservableProperty] 
    private ViewModelBase? _sideMenuBase;
}
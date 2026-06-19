using CommunityToolkit.Mvvm.ComponentModel;

namespace GottaManagePlus.ViewModels;

public abstract partial class PageViewModel(PageNames pageName) : ViewModelBase
{
    [ObservableProperty]
    public partial PageNames PageName { get; set; } = pageName;
}
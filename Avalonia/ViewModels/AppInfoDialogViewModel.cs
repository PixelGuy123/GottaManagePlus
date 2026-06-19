using CommunityToolkit.Mvvm.Input;

namespace GottaManagePlus.ViewModels;

public partial class AppInfoDialogViewModel : DialogViewModel
{
    [RelayCommand]
    public void Ok() => Close();
    
    // Does nothing currently
    protected override void Setup(params object?[]? args) { }
}
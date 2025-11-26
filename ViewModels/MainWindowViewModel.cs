using Avalonia;

namespace GottaManagePlus.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    // Const values
    const float AllMenus_DefaultRadiusValue = 15f;
    public CornerRadius AllMenus_CornerRadius_LeftScreen { get; } = new(0f, AllMenus_DefaultRadiusValue, AllMenus_DefaultRadiusValue, 0f);
    public CornerRadius AllMenus_CornerRadius_All { get; } = new(AllMenus_DefaultRadiusValue);
}

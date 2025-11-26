using Avalonia;
using Avalonia.Media.Imaging;
using GottaManagePlus.Modules.AvaloniaUtils;

namespace GottaManagePlus.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    // Const values
    const float AllMenus_DefaultRadiusValue = 15f;

    // Real Binding Values
    public Bitmap? GottaSweepLogo { get; } = ImageHelper.LoadAsBitmap("GottaManagePlus.UI.SweepLogo.webp");
    public CornerRadius AllMenus_CornerRadius_TopRight { get; } = new(0f, AllMenus_DefaultRadiusValue, 0f, 0f);
    public CornerRadius AllMenus_CornerRadius_All { get; } = new(AllMenus_DefaultRadiusValue);
}

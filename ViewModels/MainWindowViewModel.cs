using Avalonia.Media.Imaging;
using GottaManagePlus.Modules.AvaloniaUtils;

namespace GottaManagePlus.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public Bitmap? GottaSweepLogo { get; } = ImageHelper.LoadAsBitmap("GottaManagePlus.UI.SweepLogo.webp");
}

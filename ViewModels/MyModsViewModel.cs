using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GottaManagePlus.Models;

namespace GottaManagePlus.ViewModels;

public partial class MyModsViewModel : PageViewModel
{
    [ObservableProperty]
    private ObservableCollection<ModItem> _mods;

    public MyModsViewModel() : base(PageNames.Home)
    {
        _mods = [];
        for (var i = 0; i < 3; i++)
            _mods.Add(new ModItem { ModName = $"Mod {i + 1}" });
        _mods.Add(new ModItem { ModName = "Baldi\'s Basics Times" });
        _mods.Add(new ModItem { ModName = "Baldi\'s Basics Advanced Edition" });
        _mods.Add(new ModItem { ModName = "Basics of Plus - The one best mod that has been given the reward for the longest name to ever exist." });
    }
}

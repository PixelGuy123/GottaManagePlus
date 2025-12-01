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
        for (int i = 0; i < 16; i++)
            _mods.Add(new() { ModName = $"Mod {i + 1}" });
    }
}

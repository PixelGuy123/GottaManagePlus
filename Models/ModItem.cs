using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GottaManagePlus.Models;

public partial class ModItem(int id, string modName) : ItemWithPath(id)
{
    [ObservableProperty]
    private string _modName = modName;
    [ObservableProperty]
    private Version _modVersion = new(0, 0, 0); // Default is basically 0

    public override string ToString() => $"Mod: {ModName}";
}
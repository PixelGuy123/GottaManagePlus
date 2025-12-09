using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GottaManagePlus.Models;

public partial class ModItem : ObservableObject
{
    [ObservableProperty]
    private string _modName = string.Empty;
    [ObservableProperty]
    private Version _modVersion = new(0, 0, 0); // Default is basically 0
    
    public ModItem(string modName)
    {
        ModName = modName;
    }

    public override string ToString() => $"Mod: {ModName}";
}
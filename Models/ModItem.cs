using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GottaManagePlus.Models;

public partial class ModItem : ObservableObject
{
    [ObservableProperty] 
    private int _id = 0;
    [ObservableProperty]
    private string _modName = string.Empty;
    [ObservableProperty]
    private Version _modVersion = new(0, 0, 0); // Default is basically 0
    
    public ModItem(int id, string modName)
    {
        ModName = modName;
        Id = id;
    }

    public override string ToString() => $"Mod: {ModName}";
}
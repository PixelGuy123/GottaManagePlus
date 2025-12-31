using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GottaManagePlus.Models;

public partial class ModItem(int id, string modName) : ItemWithPath(id)
{
    // Observables
    [ObservableProperty]
    private string _modName = modName;
    [ObservableProperty] 
    private string? _metadataFullPath, _thumbnailFullPath;
    [ObservableProperty]
    private Version _modVersion = new(0, 0, 0); // Default is basically 0

    // Public methods
    public override string ToString() => $"Mod: {ModName}";
}
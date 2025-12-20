using System;
using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GottaManagePlus.Models;

public partial class ProfileItem(int id, string profileName) : ItemWithPath(id)
{
    // Observables
    [ObservableProperty] private int _id;
    [ObservableProperty] private string _profileName = profileName;
    [ObservableProperty] private ObservableCollection<ItemWithPath> _configsMetaDataList = [
        new(0) { FullOsPath = "/media/pixeldesktop/D0B2C468B2C4551E/Program Files (x86)/Steam/steamapps/common/Baldi's Basics Plus/BepInEx/config/Char.HellMode.Expanded.cfg" }
    ];
    [ObservableProperty] private ObservableCollection<ItemWithPath> _patchersMetaDataList = [];
    [ObservableProperty] private ObservableCollection<ModItem> _modMetaDataList = [
        new(0, "Mod 1"),
        new(1, "Mod 2"),
        new(2, "Mod 3"),
        new(3, "Baldi's Basics Times"),
        new(4, "Baldi's Basics Advanced Edition"),
        new(5, "Basics of Plus - The one best mod with the longest name ever made. You can see, it's one of the biggest names I've ever written to a sutpid freaking mod. Lorem ipsum for tyoyiu tuu.")
    ];
    
    // Internal fields
    internal DateTime DateOfCreation = new(2025, 6, 25), DateOfUpdate = new(2025, 7, 30);
    
    // Public getters
    public string CreationDate => DateOfCreation.ToString(CultureInfo.InvariantCulture); 
    public string UpdateDate => DateOfUpdate.ToString(CultureInfo.InvariantCulture); 
    public override string ToString() => $"Profile Name: {ProfileName}";
}
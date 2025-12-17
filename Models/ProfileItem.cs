using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GottaManagePlus.Models;

public partial class ProfileItem : ObservableObject
{
    [ObservableProperty] private int _id;
    [ObservableProperty] private string _profileName = string.Empty;
    [ObservableProperty] private DateTime? _dateOfCreation;
    [ObservableProperty] private ObservableCollection<ModItem> _modMetaDataList = [
        new(0, "Mod 1"),
        new(1, "Mod 2"),
        new(2, "Mod 3"),
        new(3, "Baldi's Basics Times"),
        new(4, "Baldi's Basics Advanced Edition"),
        new(5, "Basics of Plus - The one best mod with the longest name ever made. You can see, it's one of the biggest names I've ever written to a sutpid freaking mod. Lorem ipsum for tyoyiu tuu.")
    ];

    public ProfileItem(int id, string profileName)
    {
        ProfileName = profileName;
        Id = id;
    }

    public override string ToString() => $"Profile Name: {ProfileName}";
}
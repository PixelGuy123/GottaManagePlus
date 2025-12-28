using System;
using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GottaManagePlus.Models;

public partial class ProfileItem(int id, string profileName) : ItemWithPath(id)
{
    public static ProfileItem Default => new(0, "Default");
    
    // Observables
    [ObservableProperty] 
    private bool _isSelectedProfile; // Tells whether the profile is being used or is a ReadOnly file for metadata.
    [ObservableProperty] 
    private bool _isProfileMissingMetadata; // used by the UI to know whether to indicate METADATA IS MISSING or not.
    [ObservableProperty]
    private string _profileName = profileName;
    [ObservableProperty] 
    private long _profileMegabyteLength;
    [ObservableProperty] 
    private ObservableCollection<ItemWithPath> _configsMetaDataList = [];
    [ObservableProperty] 
    private ObservableCollection<ItemWithPath> _patchersMetaDataList = [];
    [ObservableProperty] 
    private ObservableCollection<ModItem> _modMetaDataList = [];
    
    // Internal fields
    internal DateTime DateOfCreation = DateTime.Now, DateOfUpdate = DateTime.Now;
    
    // Public getters
    public string CreationDate => DateOfCreation.ToString(CultureInfo.InvariantCulture); 
    public string UpdateDate => DateOfUpdate.ToString(CultureInfo.InvariantCulture); 
    public override string ToString() => $"Profile Name: {ProfileName}";
}
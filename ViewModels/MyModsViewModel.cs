using System.Linq;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GottaManagePlus.Models;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;

namespace GottaManagePlus.ViewModels;

public partial class MyModsViewModel : PageViewModel
{
    private readonly List<ModItem> _allMods;
    
    // Observable Properties
    [ObservableProperty]
    private ObservableCollection<ModItem> _mods;
    public ObservableCollection<ModItem> ImmutableModList { get; }
    [ObservableProperty]
    private ModItem? _currentModItem = null;
    [ObservableProperty]
    private string? _text = null;
    
    // From the generator. https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/observableproperty
    partial void OnCurrentModItemChanged(ModItem? value) => UpdateModsList(value); 
    
    
    [RelayCommand]
    public void ResetSearch()
    {
        CurrentModItem = null;
        Text = null;
    }
    
    // Constructor
    public MyModsViewModel() : base(PageNames.Home)
    {
        // Initialize Data
        _allMods =
        [
            new ModItem("Mod 1"),
            new ModItem("Mod 2"),
            new ModItem("Mod 3"),
            new ModItem("Baldi's Basics Times"),
            new ModItem("Baldi's Basics Advanced Edition"),
            new ModItem("Basics of Plus - The one best mod with the longest name ever made. You can see, it's one of the biggest names I've ever written to a sutpid freaking mod. Lorem ipsum for tyoyiu tuu.")
        ];

        // Initialize collections
        Mods = new ObservableCollection<ModItem>(_allMods);
        ImmutableModList = new ObservableCollection<ModItem>(_allMods);
    }

    // Private methods
    private void UpdateModsList(ModItem? highlightedItem)
    {
        IEnumerable<ModItem> sortedList;

        if (highlightedItem == null)
        {
            sortedList = _allMods;
        }
        else
        {
            sortedList = _allMods.OrderByDescending(x => x == highlightedItem);
        }
        
        Mods.Clear();
        foreach (var item in sortedList)
        {
            Mods.Add(item);
        }
    }
}

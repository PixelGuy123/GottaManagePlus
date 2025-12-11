using System;
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
    private ModItem? _lastSelectedItem;
    
    // Public readonly properties
    public IReadOnlyList<ModItem> ModList => _allMods;
    
    // Observable Properties
    [ObservableProperty]
    private ObservableCollection<ModItem> _observableMods;
    [ObservableProperty]
    private ModItem? _currentModItem;
    [ObservableProperty]
    private string? _text;
    
    // From the generator. https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/observableproperty
    partial void OnCurrentModItemChanged(ModItem? value) => UpdateModsList(value); 
    
    
    [RelayCommand]
    public void ResetSearch() => Text = null;
    
    
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
        ObservableMods = new ObservableCollection<ModItem>(_allMods);
    }

    // Private methods
    private void UpdateModsList(ModItem? highlightedItem)
    {
        // If item is not null, insert it at the top
        if (highlightedItem != null)
        {
            // Fix last selected item if needed
            int index;
            if (_lastSelectedItem != null)
            {
                index = _allMods.IndexOf(_lastSelectedItem);
                if (index != -1)
                {
                    ObservableMods.RemoveAt(0); // Presumably where the selected item is located at
                    ObservableMods.Insert(index, _lastSelectedItem);
                }
            }

            _lastSelectedItem = highlightedItem;
            index = ObservableMods.IndexOf(highlightedItem);
            if (index != -1)
            {
                ObservableMods.RemoveAt(index);
                ObservableMods.Insert(0, highlightedItem);
                return;
            }
        }
        // If highlighted item is null or not found, just reset the whole list
        _lastSelectedItem = null;
        ObservableMods.Clear();
        foreach (var item in _allMods)
            ObservableMods.Add(item);
    }
}

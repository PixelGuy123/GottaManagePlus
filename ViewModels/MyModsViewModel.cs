using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GottaManagePlus.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using GottaManagePlus.Interfaces;
using GottaManagePlus.Services;

namespace GottaManagePlus.ViewModels;

public partial class MyModsViewModel : PageViewModel
{
    private readonly List<ModItem> _allMods;
    private ModItem? _lastSelectedItem;
    private readonly DialogService _dialogService = null!;
    private readonly IDialogProvider _dialogProvider = null!;
    
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

    [RelayCommand]
    public void DeleteModItem(int id)
    {
        DeleteModItemUiAsync(id);
    }
    
    // For designer
    public MyModsViewModel() : base(PageNames.Home)
    {
        // Initialize Data
        _allMods =
        [
            new ModItem(0, "Mod 1"),
            new ModItem(1, "Mod 2"),
            new ModItem(2, "Mod 3"),
            new ModItem(3, "Baldi's Basics Times"),
            new ModItem(4, "Baldi's Basics Advanced Edition"),
            new ModItem(5, "Basics of Plus - The one best mod with the longest name ever made. You can see, it's one of the biggest names I've ever written to a sutpid freaking mod. Lorem ipsum for tyoyiu tuu.")
        ];
        
        // Initialize collections
        ObservableMods = new ObservableCollection<ModItem>(_allMods);
    } 
    
    // Constructor
    public MyModsViewModel(DialogService dialogService, MainWindowViewModel dialogProvider) : base(PageNames.Home)
    {
        // Initialize Data
        _allMods =
        [
            new ModItem(0, "Mod 1"),
            new ModItem(1, "Mod 2"),
            new ModItem(2, "Mod 3"),
            new ModItem(3, "Baldi's Basics Times"),
            new ModItem(4, "Baldi's Basics Advanced Edition"),
            new ModItem(5, "Basics of Plus - The one best mod with the longest name ever made. You can see, it's one of the biggest names I've ever written to a sutpid freaking mod. Lorem ipsum for tyoyiu tuu.")
        ];

        // Initialize collections
        ObservableMods = new ObservableCollection<ModItem>(_allMods);
        
        // Dialog Service
        _dialogService = dialogService;
        _dialogProvider = dialogProvider;
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

    private void ResetListVisibleConfigurations() // Basically reset the observable list
    {
        UpdateModsList(null);
        ResetSearch();
    }
    
    private async Task DeleteModItemUiAsync(int id) // Delete asynchronously the items
    {
        var index = _allMods.FindIndex(item => item.Id == id);
        if (index == -1) // If the item doesn't exist, skip
        {
            // TODO: Open dialog for not succeeding deletion
            return;
        }
        
        // TODO: Implement translation support here
        var confirmViewModel = new ConfirmDialogViewModel()
        {
            Title = $"Delete {_allMods[index].ModName}?",
            Message = "Are you sure you want to delete this print?",
            ConfirmText = "Yes",
            CancelText = "No"
        };

        await _dialogService.ShowDialog(_dialogProvider, confirmViewModel);
        
        // Do not if not accepted
        if (!confirmViewModel.Confirmed)
            return;
        
        _allMods.RemoveAt(index);
        ResetListVisibleConfigurations();
    }
}

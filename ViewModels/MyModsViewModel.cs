using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GottaManagePlus.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using GottaManagePlus.Interfaces;
using GottaManagePlus.Services;
using GottaManagePlus.Utils;

namespace GottaManagePlus.ViewModels;

// TODO: Add an indicator of "No mods available" to the Mod Viewer.
// TODO: Add a mod counter.
// TODO: Add a current profile indicator in the header.

public partial class MyModsViewModel : PageViewModel, IDisposable
{
    private readonly List<ModItem> _allMods = [];
    private ModItem? _lastSelectedItem;
    private readonly DialogService _dialogService = null!;
    private readonly IProfileProvider _profileProvider = null!;
    
    // Observable Properties
    [ObservableProperty] 
    private ObservableCollection<ModItem> _observableUnchangedMods = [];
    [ObservableProperty]
    private ObservableCollection<ModItem> _observableMods = [];
    [ObservableProperty]
    private ModItem? _currentModItem;
    [ObservableProperty]
    private string? _text;
    
    // From the generator. https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/observableproperty
    partial void OnCurrentModItemChanged(ModItem? value) => UpdateModsList(value); 
    
    [RelayCommand]
    public void ResetSearch() => Text = null;

    [RelayCommand]
    public async Task DeleteModItem(int id) => await DeleteModItemUiAsync(id);
    
    
    // For designer
    public MyModsViewModel() : base(PageNames.Home, new ProfilesViewModel(null!, new ProfileProvider(null!), null!, null!))
    {
        if (!Design.IsDesignMode) return;
        
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
        ObservableUnchangedMods = new ObservableCollection<ModItem>(_allMods);
    }
    
    // Constructor
    public MyModsViewModel(DialogService dialogService, ProfilesViewModel profilesViewModel, ProfileProvider profileProvider) : base(PageNames.Home, profilesViewModel)
    {
        // Service
        _dialogService = dialogService;
        _profileProvider = profileProvider;
        _profileProvider.OnProfilesUpdate += ProfilesProvider_OnProfilesUpdate;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _profileProvider.OnProfilesUpdate -= ProfilesProvider_OnProfilesUpdate;
    }

    // Private methods
    private void ProfilesProvider_OnProfilesUpdate(IProfileProvider provider)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _allMods.Clear();
            _allMods.AddRange(provider.GetInstanceActiveProfile().ModMetaDataList);

            ObservableUnchangedMods.Clear();
            foreach (var mod in _allMods)
                ObservableUnchangedMods.Add(mod);

            ResetListVisibleConfigurations();
        });
    }
    
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
            await _dialogService.ShowDialog(new ConfirmDialogViewModel(true)
            {
                Title = Constants.FailDialog,
                Message = $"""
                          Failed to delete the profile!
                          For some reason, there's no profile with the id ({id}).
                          """
            });
            return;
        }
        
        var confirmViewModel = new ConfirmDialogViewModel()
        {
            Title = $"Delete {_allMods[index].ModName}?",
            Message = "Are you sure you want to delete this mod?",
            ConfirmText = "Yes",
            CancelText = "No"
        };

        await _dialogService.ShowDialog(confirmViewModel);
        
        // Do not if not accepted
        if (!confirmViewModel.Confirmed)
            return;
        
        _allMods.RemoveAt(index);
        ResetListVisibleConfigurations();
    }
}

using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GottaManagePlus.Models;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using GottaManagePlus.Interfaces;
using GottaManagePlus.Models.GameEnvironments;
using GottaManagePlus.Models.UI;
using GottaManagePlus.Services;
using GottaManagePlus.Services.ExplorerServices;
using GottaManagePlus.Services.GameEnvironmentServices;
using GottaManagePlus.Services.ProfileServices;
using GottaManagePlus.Utils;
using Serilog;

namespace GottaManagePlus.ViewModels;

public partial class MyModsViewModel : PageViewModel, IDisposable
{
    private readonly DialogService _dialogService = null!;
    private readonly ProfileManager _profileManager = null!;
    private readonly DirectoryLauncher _directoryLauncher = null!;
    private readonly GameEnvironmentController _gameEnvironmentController = null!;

    // Getters
    protected readonly Dictionary<int, ModManifest> AllMods = [];
    private void FillUpAllMods(IEnumerable<ModManifest>? manifests) { AllMods.Clear(); var index = 0; foreach (var manifest in manifests ?? []) AllMods.Add(index++, manifest); }
    
    // Observable Properties
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(HasAnyModsToDisplay))]
    private ObservableCollection<ModManifest> _observableUnchangedMods = []; // The one that communicates directly with the service

    // AutoCompleteBox Form
    [ObservableProperty] private ObservableCollection<ModManifest> _observableMods = []; // Observable Mods that is actually touched
    [ObservableProperty] private ModManifest? _currentModManifest; // Current Manifest Selected
    [ObservableProperty] private string? _text; // To clarify, it's Text from the AutoCompleteBox

    // ReadOnly Properties for UI
    public int NumberOfModsPerRow { get; } = 6;
    public bool HasAnyModsToDisplay => ObservableUnchangedMods.Count != 0;
    public string CurrentPlusVersion =>
        _gameEnvironmentController.CurrentEnvironment!.GameVersion.ToString();


    // From the generator. https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/observableproperty
    partial void OnCurrentModManifestChanged(ModManifest? value) => UpdateModsList(value);

    // To update the display
    private void OnObservableUnchangedModsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        OnPropertyChanged(nameof(HasAnyModsToDisplay));

    [RelayCommand]
    public void ResetSearch() => Text = null;

    [RelayCommand]
    public async Task DeleteModManifest(int id) => await DeleteModManifestUiAsync(id);
    
    [RelayCommand]
    public async Task AddModRequest() => await AddModUiAsync();

    [RelayCommand]
    public async Task OpenModPath(int id)
    {
        const string modFixSuggestion = "Try reloading the profiles list.";
        ConfirmDialogViewModel confirmDialog;

        // Check if mod exists
        if (!AllMods.TryGetValue(id, out var mod)) // If the item doesn't exist, skip
        {
            confirmDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
            confirmDialog.Prepare(
                null,
                Constants.FailDialog,
                $"""
                 Failed to delete the profile!
                 For some reason, there's no profile with the id ({id}).
                 """
            );
            await _dialogService.ShowDialog(confirmDialog);
            return;
        }

        // Get the directory path
        var modDirectoryPath = mod.GetPluginDirectoryFromManifest(_gameEnvironmentController);
        
        // Check if it exists
        if (!Directory.Exists(modDirectoryPath))
        {
            confirmDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
            confirmDialog.Prepare(
                true,
                Constants.FailDialog,
                $"The path to the mod is somehow invalid!\n{modFixSuggestion}"
            );
            await _dialogService.ShowDialog(confirmDialog);
            return;
        }

        // Open the mod here
        if (!await _directoryLauncher.OpenDirectoryInfo(new DirectoryInfo(modDirectoryPath)))
        {
            confirmDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
            confirmDialog.Prepare(
                true,
                Constants.FailDialog,
                $"Failed to open the path to the mod due to an unknown error!\n{modFixSuggestion}"
            );
            await _dialogService.ShowDialog(confirmDialog);
        }
    }


    // For designer
    public MyModsViewModel() : base(PageNames.Home)
    {
        if (!Design.IsDesignMode) return;

        // Initialize Data
        List<ModManifest> manifests =
        [
            // Short standard entries (approx. 20 items)
            new() { Name = "Mod 1" },
            new() { Name = "Baldi's Basics Times" },
            new() { Name = "Baldi's Advanced Edition" },
            new() { Name = "Optimization Pack" },
            new() { Name = "Texture Replacer v2" },
            new() { Name = "Sound Effects Overhaul" },
            new() { Name = "Quick Fix" },
            new() { Name = "Community Hub" },
            new() { Name = "Night Mode" },
            new() { Name = "Speed Run Helper" },
            new() { Name = "Inventory Manager" },
            new() { Name = "Map Expansion" },
            new() { Name = "Character Skins" },
            new() { Name = "Difficulty Adjuster" },
            new() { Name = "UI Enhancer" },
            new() { Name = "Save Editor" },
            new() { Name = "Quest Tracker" },
            new() { Name = "Weather System" },
            new() { Name = "Audio Remaster" },
            new() { Name = "Bug Fixes Bundle" },

            // Long name entries (approx. 150 characters) for UI testing
            new() { Name = "The Ultimate Comprehensive Overhaul Package for Enhanced Gameplay Experience and Visual Fidelity Improvements Across All Levels Including Boss Battles" },
            new() { Name = "Advanced Physics Engine Modification Suite with Realistic Collision Detection and Dynamic Lighting Support for Maximum Immersion and Performance Optimization" }
        ];

        // Initialize collections
        ObservableUnchangedMods = new ObservableCollection<ModManifest>(manifests);
        ObservableMods = new ObservableCollection<ModManifest>(manifests);
    }

    // Constructor
    public MyModsViewModel(DialogService dialogService,
        ProfileManager profileManager, GameEnvironmentController controller, DirectoryLauncher directoryLauncher, SettingsService settingsService) : base(PageNames.Home)
    {
        // Service
        _dialogService = dialogService;
        _profileManager = profileManager;
        _gameEnvironmentController = controller;
        _directoryLauncher = directoryLauncher;
        
        // Settings
        NumberOfModsPerRow = settingsService.CurrentSettings.NumberOfRowsPerMod;
        
        // Create mods list
        FillUpAllMods(_profileManager.ActiveProfile?.ModDataFiles);
        
        // Listeners
        _profileManager.OnActiveProfileUpdate += ProfilesProvider_OnProfilesUpdate;
        ObservableUnchangedMods.CollectionChanged += OnObservableUnchangedModsCollectionChanged;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _profileManager.OnActiveProfileUpdate -= ProfilesProvider_OnProfilesUpdate;
    }

    // Private methods
    private void ProfilesProvider_OnProfilesUpdate(ProfileMetadata? profileMetadata)
    {
        Dispatcher.UIThread.Post(() =>
        {
            // Fill up the mods from the metadata.
            FillUpAllMods(profileMetadata?.ModDataFiles);

            // Form a new collection from them.
            var newCollection = new ObservableCollection<ModManifest>(AllMods.Values);
            newCollection.CollectionChanged += OnObservableUnchangedModsCollectionChanged;
            ObservableUnchangedMods = newCollection;

            ResetListVisibleConfigurations();
        });
    }

    private void UpdateModsList(ModManifest? highlightedItem)
    {
        // If item is not null, insert it at the top
        if (highlightedItem != null)
        {
            ObservableMods = new ObservableCollection<ModManifest>(
                ObservableMods.OrderByDescending(mod => highlightedItem.Name.ManyStartWith(mod.Name)
                ));
            return;
        }

        // If highlighted item is null or not found, just reset the whole list
        ObservableMods = new ObservableCollection<ModManifest>(AllMods.Values.OrderBy(mod => mod.Name));
    }

    private void ResetListVisibleConfigurations() // Basically reset the observable list
    {
        UpdateModsList(null);
        ResetSearch();
    }

    private async Task AddModUiAsync() // Popup for adding a mod
    {
        
    }

    private async Task DeleteModManifestUiAsync(int id) // Delete asynchronously the items
    {
        ConfirmDialogViewModel confirmDialog;
        if (!AllMods.TryGetValue(id, out var modToDelete)) // If the item doesn't exist, skip
        {
            confirmDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
            confirmDialog.Prepare(
                true,
                Constants.FailDialog,
                $"""
                 Failed to delete the mod!
                 For some reason, there's no mod with the id ({id}).
                 """
            );
            await _dialogService.ShowDialog(confirmDialog);
            return;
        }

        // Show confirmation dialog
        confirmDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
        confirmDialog.Prepare(
            null,
            $"Delete {modToDelete.Name}?",
            "Are you sure you want to delete this mod?",
            "Yes",
            "No"
        );
        await _dialogService.ShowDialog(confirmDialog);

        // Do not if not accepted
        if (!confirmDialog.Confirmed)
            return;

        // Get metadata for deleting in profile provider
        var modName = modToDelete.Name;

        // Get active profile metadata and remove all the instances of the mod
        var profileMetadata = _profileManager.ActiveProfile;
        var modDataFiles = profileMetadata?.ModDataFiles ?? [];

        for (var i = 0; i < modDataFiles.Count; i++)
        {
            var mod = modDataFiles[i];
            var modDirectoryPath = mod.GetPluginDirectoryFromManifest(_gameEnvironmentController);
            if (mod.Name != modName || string.IsNullOrEmpty(modDirectoryPath)) continue;

            // Try to manually delete
            try
            {
                // TODO: Handle proper mod deletion through a service instead of manual implementation
                Directory.Delete(modDirectoryPath);
            }
            catch (Exception e)
            {
                Log.Logger.Error("Failed to delete the mod ({ModName})!\n{exception}", modName, e);
                break;
            }

            // Remove item
            profileMetadata?.ModDataFiles.RemoveAt(i--);
        }

        // Request save changes to the profile
        var loadingDialog = _dialogService.GetDialog<LoadingDialogViewModel>();
        loadingDialog.Prepare("Saving changes...", null, (Delegate)_profileManager.SaveActiveProfile);
        
        // Try to save profile
        if (!await _dialogService.ShowDialog(loadingDialog))
        {
            confirmDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
            confirmDialog.Prepare(
                Constants.FailDialog,
                $"Failed to save the changes. If this issue persists, try:\n{Constants.SolutionFilePermissions}"
            );
            
            // Show dialog
            await _dialogService.ShowDialog(confirmDialog);
            return;
        }
    }
}
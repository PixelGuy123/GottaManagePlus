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
using GottaManagePlus.Services;
using GottaManagePlus.Utils;

namespace GottaManagePlus.ViewModels;

public partial class MyModsViewModel : PageViewModel, IDisposable
{
    private readonly List<ModItem> _allMods = [];
    private readonly DialogService _dialogService = null!;
    private readonly IGameFolderViewer _gameFolderViewer = null!;
    private readonly IProfileProvider _profileProvider = null!;
    private readonly IFilesService _filesService = null!;

    // Observable Properties
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(HasAnyModsToDisplay))]
    private ObservableCollection<ModItem> _observableUnchangedMods = [];

    [ObservableProperty] private ObservableCollection<ModItem> _observableMods = [];
    [ObservableProperty] private ModItem? _currentModItem;
    [ObservableProperty] private string? _text; // To clarify, it's Text from the AutoCompleteBox

    public int NumberOfModsPerRow { get; } = 6;
    public bool HasAnyModsToDisplay => ObservableUnchangedMods.Count != 0;

    public string CurrentPlusVersion =>
        _gameFolderViewer?.GetGameVersion().ToString() ?? "0.13.1";


    // From the generator. https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/observableproperty
    partial void OnCurrentModItemChanged(ModItem? value) => UpdateModsList(value);

    // To update the display
    private void OnObservableUnchangedModsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        OnPropertyChanged(nameof(HasAnyModsToDisplay));

    [RelayCommand]
    public void ResetSearch() => Text = null;

    [RelayCommand]
    public async Task DeleteModItem(int id) => await DeleteModItemUiAsync(id);

    [RelayCommand]
    public async Task OpenModPath(int id)
    {
        const string modFixSuggestion = "Try reloading the profiles list.";
        ConfirmDialogViewModel confirmDialog;

        // Check if mod exists
        var index = _allMods.FindIndex(item => item.Id == id);
        if (index == -1) // If the item doesn't exist, skip
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

        // Get mod instance
        var mod = _allMods[index];
        // Get directory path
        if (!File.Exists(mod.FullOsPath))
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
        if (!_filesService.OpenFileInfo(new FileInfo(mod.FullOsPath)))
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
    public MyModsViewModel() : base(PageNames.Home,
        new ProfilesViewModel(null!, new ProfileProvider(null!), null!, null!, null!))
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
            new ModItem(5,
                "Basics of Plus - The one best mod with the longest name ever made. You can see, it's one of the biggest names I've ever written to a sutpid freaking mod. Lorem ipsum for tyoyiu tuu."),
            new ModItem(6,
                "Basics of Plus - The one best mod with the longest name ever made. You can see, it's one of the biggest names I've ever written to a sutpid freaking mod. Lorem ipsum for tyoyiu tuu."),
            new ModItem(7,
                "Basics of Plus - The one best mod with the longest name ever made. You can see, it's one of the biggest names I've ever written to a sutpid freaking mod. Lorem ipsum for tyoyiu tuu."),
            new ModItem(8,
                "Basics of Plus - The one best mod with the longest name ever made. You can see, it's one of the biggest names I've ever written to a sutpid freaking mod. Lorem ipsum for tyoyiu tuu."),
            new ModItem(9,
                "Basics of Plus - The one best mod with the longest name ever made. You can see, it's one of the biggest names I've ever written to a sutpid freaking mod. Lorem ipsum for tyoyiu tuu."),
            new ModItem(10,
                "Basics of Plus - The one best mod with the longest name ever made. You can see, it's one of the biggest names I've ever written to a sutpid freaking mod. Lorem ipsum for tyoyiu tuu."),
            new ModItem(11,
                "Basics of Plus - The one best mod with the longest name ever made. You can see, it's one of the biggest names I've ever written to a sutpid freaking mod. Lorem ipsum for tyoyiu tuu."),
            new ModItem(12,
                "Basics of Plus - The one best mod with the longest name ever made. You can see, it's one of the biggest names I've ever written to a sutpid freaking mod. Lorem ipsum for tyoyiu tuu."),
            new ModItem(13,
                "Basics of Plus - The one best mod with the longest name ever made. You can see, it's one of the biggest names I've ever written to a sutpid freaking mod. Lorem ipsum for tyoyiu tuu."),
            new ModItem(14,
                "Basics of Plus - The one best mod with the longest name ever made. You can see, it's one of the biggest names I've ever written to a sutpid freaking mod. Lorem ipsum for tyoyiu tuu."),
            new ModItem(15,
                "Basics of Plus - The one best mod with the longest name ever made. You can see, it's one of the biggest names I've ever written to a sutpid freaking mod. Lorem ipsum for tyoyiu tuu."),
            new ModItem(16,
                "Basics of Plus - The one best mod with the longest name ever made. You can see, it's one of the biggest names I've ever written to a sutpid freaking mod. Lorem ipsum for tyoyiu tuu."),
            new ModItem(17,
                "Basics of Plus - The one best mod with the longest name ever made. You can see, it's one of the biggest names I've ever written to a sutpid freaking mod. Lorem ipsum for tyoyiu tuu."),
            new ModItem(18,
                "Basics of Plus - The one best mod with the longest name ever made. You can see, it's one of the biggest names I've ever written to a sutpid freaking mod. Lorem ipsum for tyoyiu tuu."),
            new ModItem(19,
                "Basics of Plus - The one best mod with the longest name ever made. You can see, it's one of the biggest names I've ever written to a sutpid freaking mod. Lorem ipsum for tyoyiu tuu."),
            new ModItem(20,
                "Basics of Plus - The one best mod with the longest name ever made. You can see, it's one of the biggest names I've ever written to a sutpid freaking mod. Lorem ipsum for tyoyiu tuu."),
            new ModItem(21,
                "Basics of Plus - The one best mod with the longest name ever made. You can see, it's one of the biggest names I've ever written to a sutpid freaking mod. Lorem ipsum for tyoyiu tuu."),
            new ModItem(22,
                "Basics of Plus - The one best mod with the longest name ever made. You can see, it's one of the biggest names I've ever written to a sutpid freaking mod. Lorem ipsum for tyoyiu tuu."),
            new ModItem(23,
                "Basics of Plus - The one best mod with the longest name ever made. You can see, it's one of the biggest names I've ever written to a sutpid freaking mod. Lorem ipsum for tyoyiu tuu."),
            new ModItem(24,
                "Basics of Plus - The one best mod with the longest name ever made. You can see, it's one of the biggest names I've ever written to a sutpid freaking mod. Lorem ipsum for tyoyiu tuu."),
            new ModItem(25,
                "Basics of Plus - The one best mod with the longest name ever made. You can see, it's one of the biggest names I've ever written to a sutpid freaking mod. Lorem ipsum for tyoyiu tuu."),
            new ModItem(26,
                "Basics of Plus - The one best mod with the longest name ever made. You can see, it's one of the biggest names I've ever written to a sutpid freaking mod. Lorem ipsum for tyoyiu tuu."),
            new ModItem(27,
                "Basics of Plus - The one best mod with the longest name ever made. You can see, it's one of the biggest names I've ever written to a sutpid freaking mod. Lorem ipsum for tyoyiu tuu."),
        ];

        // Initialize collections
        ObservableUnchangedMods = new ObservableCollection<ModItem>(_allMods);
        ObservableMods = new ObservableCollection<ModItem>(_allMods);
    }

    // Constructor
    public MyModsViewModel(DialogService dialogService, ProfilesViewModel profilesViewModel,
        ProfileProvider profileProvider, FilesService filesService, PlusFolderViewer viewer,
        SettingsService settingsService) : base(PageNames.Home, profilesViewModel)
    {
        // Service
        _dialogService = dialogService;
        _profileProvider = profileProvider;
        _gameFolderViewer = viewer;
        _filesService = filesService;
        NumberOfModsPerRow = settingsService.CurrentSettings.NumberOfRowsPerMod;
        
        // Listeners
        profilesViewModel.AfterProfileUpdate += ProfilesProvider_OnProfilesUpdate;
        ObservableUnchangedMods.CollectionChanged += OnObservableUnchangedModsCollectionChanged;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        ((ProfilesViewModel)SideMenuBase!).AfterProfileUpdate -= ProfilesProvider_OnProfilesUpdate;
    }

    // Private methods
    private void ProfilesProvider_OnProfilesUpdate(IProfileProvider provider)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _allMods.Clear();
            _allMods.AddRange(provider.GetInstanceActiveProfile().ModMetaDataList);

            var newCollection = new ObservableCollection<ModItem>(_allMods);
            newCollection.CollectionChanged += OnObservableUnchangedModsCollectionChanged;
            ObservableUnchangedMods = newCollection;

            ResetListVisibleConfigurations();
        });
    }

    private void UpdateModsList(ModItem? highlightedItem)
    {
        // If item is not null, insert it at the top
        if (highlightedItem != null)
        {
            ObservableMods = new ObservableCollection<ModItem>(
                ObservableMods.OrderByDescending(mod => highlightedItem.ModName.ManyStartWith(mod.ModName)
                ));
            return;
        }

        // If highlighted item is null or not found, just reset the whole list
        ObservableMods = new ObservableCollection<ModItem>(_allMods.OrderBy(mod => mod.ModName));
    }

    private void ResetListVisibleConfigurations() // Basically reset the observable list
    {
        UpdateModsList(null);
        ResetSearch();
    }

    private async Task DeleteModItemUiAsync(int id) // Delete asynchronously the items
    {
        ConfirmDialogViewModel confirmDialog;
        var index = _allMods.FindIndex(item => item.Id == id);
        if (index == -1) // If the item doesn't exist, skip
        {
            confirmDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
            confirmDialog.Prepare(
                true,
                Constants.FailDialog,
                $"""
                 Failed to delete the profile!
                 For some reason, there's no profile with the id ({id}).
                 """
            );
            await _dialogService.ShowDialog(confirmDialog);
            return;
        }

        // Show confirmation dialog
        confirmDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
        confirmDialog.Prepare(
            null,
            $"Delete {_allMods[index].ModName}?",
            "Are you sure you want to delete this mod?",
            "Yes",
            "No"
        );
        await _dialogService.ShowDialog(confirmDialog);

        // Do not if not accepted
        if (!confirmDialog.Confirmed)
            return;

        // Get metadata for deleting in profile provider
        var modName = _allMods[index].ModName;

        // Get active profile item and remove all the instances of the mod
        var profileItem = _profileProvider.GetInstanceActiveProfile();
        var tempModList = new ObservableCollection<ModItem>(profileItem.ModMetaDataList); // For reverting changes
        for (var i = 0; i < profileItem.ModMetaDataList.Count; i++)
        {
            var mod = profileItem.ModMetaDataList[i];
            if (mod.ModName != modName || string.IsNullOrEmpty(mod.FullOsPath)) continue;

            // Try to manually delete
            try
            {
                // TODO: Handle proper mod deletion through a service instead of manual implementation
                File.Delete(mod.FullOsPath);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Failed to delete the mod ({modName})!", Constants.DebugError);
                Debug.WriteLine(e.ToString(), Constants.DebugError);
                break;
            }

            // Remove item
            profileItem.ModMetaDataList.RemoveAt(i--);
        }

        // Request save changes to the profile
        var loadingDialog = _dialogService.GetDialog<LoadingDialogViewModel>();
        loadingDialog.Prepare("Saving changes...", null, (Delegate)_profileProvider.SaveActiveProfile);
        // Try to delete mod
        if (!await _dialogService.ShowLoadingDialog(loadingDialog))
        {
            // If the mod fails to be deleted, revert back to the old list
            profileItem.ModMetaDataList = tempModList;

            confirmDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
            confirmDialog.Prepare(
                Constants.FailDialog,
                $"Failed to save the changes. If this issue persists, try:\n{Constants.SolutionFilePermissions}"
            );
            // Show dialog
            await _dialogService.ShowDialog(confirmDialog);
            return;
        }

        // Update profile data (save)
        await WaitToUpdateData();
    }

    private async Task WaitToUpdateData(string preferredIndex = "") // Copy-paste from ProfileViewModel.cs
    {
        var loadingDialog = _dialogService.GetDialog<LoadingDialogViewModel>();
        loadingDialog.Prepare("Updating profile data...", null,
            (Delegate)_profileProvider.UpdateProfilesData, preferredIndex);
        if (!await _dialogService.ShowLoadingDialog(loadingDialog))
        {
            var confirmDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
            confirmDialog.Prepare(true, Constants.FailDialog,
                $"""
                 Failed to update the profiles list!
                 If the issue persists, you can try:
                 {Constants.SolutionFilePermissions}
                 """
            );
            // Not-so-aggressive dialog
            await _dialogService.ShowDialog(confirmDialog);
        }
    }
}
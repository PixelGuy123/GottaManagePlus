using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GottaManagePlus.Interfaces;
using GottaManagePlus.Models;
using GottaManagePlus.Services;
using GottaManagePlus.Utils;

namespace GottaManagePlus.ViewModels;

public partial class ProfilesViewModel : ViewModelBase, IDisposable
{
    private readonly DialogService _dialogService = null!;
    private readonly IProfileProvider _profileProvider = null!;
    private readonly IGameFolderViewer _gameFolderViewer = null!;
    private readonly SettingsService _settingsService = null!;
    private readonly IFilesService _filesService = null!;
    private readonly List<ProfileItem> _allProfiles = [];
    private ProfileItem? _lastSelectedItem;

    public event Action<IProfileProvider>? AfterProfileUpdate;
    
    // Observable Properties
    [ObservableProperty] 
    private ObservableCollection<ProfileItem> _observableUnchangedProfiles = [];
    [ObservableProperty] 
    private ObservableCollection<ProfileItem> _observableProfiles = [];
    [ObservableProperty]
    private ProfileItem? _currentProfileItem;
    [ObservableProperty]
    private string? _text;
    
    // From the generator. https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/observableproperty
    partial void OnCurrentProfileItemChanged(ProfileItem? value) => UpdateProfilesList(value); 
    
    [RelayCommand]
    public void ResetSearch() => Text = null;

    [RelayCommand]
    public async Task OpenProfileMetadata(int id) => await OpenProfileMetaDataAndHandleActions(id);

    [RelayCommand]
    public async Task UpdateProfilesData() => await WaitToUpdateData();

    [RelayCommand]
    public async Task CreateProfileUi() => await CreateProfileUiAsync();

    [RelayCommand]
    public async Task SwitchToProfile(int id) => await SwitchProfileUiAsync(id);

    // Previewer Constructor
    public ProfilesViewModel()
    {
        if (!Design.IsDesignMode) return;
        
        // Initialize Data
        _allProfiles =
        [
            new ProfileItem(0, "Profile 1"),
            new ProfileItem(1, "Profile 2"),
            new ProfileItem(2, "Profile 3"),
            new ProfileItem(3, "The Ultimate ModPack"),
            new ProfileItem(4, "Biggest Modpack ever"),
            new ProfileItem(5, "Funny mod pack with the longest trollest biggest hugest name that you've ever seen.")
        ];
        
        // Initialize collections
        ObservableProfiles = new ObservableCollection<ProfileItem>(_allProfiles);
        ObservableUnchangedProfiles = new ObservableCollection<ProfileItem>(_allProfiles);

        _profileProvider = new ProfileProvider(null!);
    }
    
    // DI Constructor
    public ProfilesViewModel(DialogService dialogService, ProfileProvider profileProvider, FilesService filesService, SettingsService settingsService, PlusFolderViewer plusFolderViewer)
    {
        if (Design.IsDesignMode) return;
        
        // Services
        _dialogService = dialogService;
        _filesService = filesService;
        _settingsService = settingsService;
        _gameFolderViewer = plusFolderViewer;
        _profileProvider = profileProvider;
        _profileProvider.OnProfilesUpdate += ProfilesProvider_OnProfilesUpdate;
        
        // Update profile data on random thread
        Dispatcher.UIThread.InvokeAsync(() => WaitToUpdateData(_settingsService.CurrentSettings.CurrentProfileSet), DispatcherPriority.Loaded);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _profileProvider.OnProfilesUpdate -= ProfilesProvider_OnProfilesUpdate;
    }
    
    private void ProfilesProvider_OnProfilesUpdate(IProfileProvider provider)
    {
        // Initialize Data
        Dispatcher.UIThread.Post(() => 
        {
            _allProfiles.Clear();
            _allProfiles.AddRange(_profileProvider.GetLoadedProfiles());

            ObservableUnchangedProfiles = new ObservableCollection<ProfileItem>(_allProfiles);
            
            ResetListVisibleConfigurations();
            
            // Update profile settings
            _settingsService.CurrentSettings.CurrentProfileSet = _profileProvider.GetInstanceActiveProfile().ProfileName;
            
            // Update other view models subscribed to this event
            AfterProfileUpdate?.Invoke(provider);
        });
    }
    
    // Private methods
    private void UpdateProfilesList(ProfileItem? highlightedItem)
    {
        // If item is not null, insert it at the top
        if (highlightedItem != null)
        {
            // Fix last selected item if needed
            int index;
            if (_lastSelectedItem != null)
            {
                index = _allProfiles.IndexOf(_lastSelectedItem);
                if (index != -1)
                {
                    ObservableProfiles.RemoveAt(0); // Presumably where the selected item is located at
                    ObservableProfiles.Insert(index, _lastSelectedItem);
                }
            }

            _lastSelectedItem = highlightedItem;
            index = ObservableProfiles.IndexOf(highlightedItem);
            if (index != -1)
            {
                ObservableProfiles.RemoveAt(index);
                ObservableProfiles.Insert(0, highlightedItem);
                return;
            }
        }
        // If highlighted item is null or not found, just reset the whole list
        _lastSelectedItem = null;
        ObservableProfiles = new ObservableCollection<ProfileItem>(_allProfiles);
    }

    private void ResetListVisibleConfigurations() // Basically reset the observable list
    {
        UpdateProfilesList(null);
        ResetSearch();
    }

    private async Task OpenProfileMetaDataAndHandleActions(int id)
    {
        var index = _allProfiles.FindIndex(item => item.Id == id);
        if (index == -1) // If the item doesn't exist, skip
        {
            var confirmDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
            confirmDialog.Prepare(true, Constants.FailDialog, $"Failed to open the profile.\nFor some reason, their id ({id}) wasn't found!");
            await _dialogService.ShowDialog(confirmDialog);
            return;
        }

        if (_allProfiles[index].IsProfileMissingMetadata)
        {
            var confirmDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
            confirmDialog.Prepare(true, Constants.WarningDialog, """
                          The profile you're attempting to open is missing its metadata! 
                          If you want to know what's inside it, you will need to load this profile.
                          Don't worry, your currently active profile will be saved.
                          """);
            await _dialogService.ShowDialog(confirmDialog);
            return;
        }

        // Create profile viewer
        var profileViewer = _dialogService.GetDialog<PreviewProfileDialogViewModel>();
        
        
        // Loop until the dialog is truly closed
        while (true)
        {
            // Prepare viewer before showing again
            profileViewer.Prepare(
                _allProfiles[index], 
                _allProfiles.Count > 1, 
                _filesService,
                _dialogService);
            await _dialogService.ShowDialog(profileViewer);
            
            if (profileViewer.ShouldDeleteProfile)
            {
                // If deleting the item works, then we don't need to loop back
                if (await DeleteProfileItemUiAsync(id)) break;

                continue; // Loop back, since that wasn't a normal close
            }

            if (profileViewer.ShouldExportProfile)
            {
                await ExportProfileItem(id);
                continue;
            }

            // If there's a feedback, display it and reset the loop
            if (profileViewer.SubDialogView != null)
            {
                await _dialogService.ShowDialog(profileViewer.SubDialogView);
                continue;
            }

            break;
        }
    }

    private async Task ExportProfileItem(int index)
    {
        var confirmViewModel = _dialogService.GetDialog<ConfirmDialogViewModel>();
        confirmViewModel.Prepare(
            null,
            $"Export {_allProfiles[index].ProfileName}?",
            "Are you sure you want to export this profile?",
            "Yes",
            "No"
        );

        // Show confirmation dialog
        await _dialogService.ShowDialog(confirmViewModel);
        
        // Do not if not accepted
        if (!confirmViewModel.Confirmed)
            return;
        
        var loadingDialog = _dialogService.GetDialog<LoadingDialogViewModel>();
        loadingDialog.Prepare("Profile Export", "Exporting profile...", (Delegate)_profileProvider.ExportProfile, index);
        
        if (!await _dialogService.ShowLoadingDialog(loadingDialog))
        {
            // If it fails, show dialog
            var confirmDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
            confirmDialog.Prepare(true, Constants.FailDialog, $"Failed to export the profile! If you're still having issues, try this:\n{Constants.SolutionFilePermissions}");
            await _dialogService.ShowDialog(confirmDialog);
            return;
        }
        // Success action (open the export)
        await _filesService.OpenDirectoryInfo(new DirectoryInfo(_gameFolderViewer.SearchPath(
            _gameFolderViewer.GetPathFrom(IGameFolderViewer.CommonDirectory.ManagerRoot),
            Constants.ProfileExportFolder
            )));
    }
    
    private async Task<bool> DeleteProfileItemUiAsync(int index) // Delete asynchronously the items
    {
        var confirmViewModel = _dialogService.GetDialog<ConfirmDialogViewModel>();
        confirmViewModel.Prepare(
            null,
            $"Delete {_allProfiles[index].ProfileName}?",
            "Are you sure you want to delete this profile?",
            "Yes",
            "No"
        );

        // Show confirmation dialog
        await _dialogService.ShowDialog(confirmViewModel);
        
        // Do not if not accepted
        if (!confirmViewModel.Confirmed)
            return false;
        
        var loadingDialog = _dialogService.GetDialog<LoadingDialogViewModel>();
        loadingDialog.Prepare("Deleting profile...", null, (Delegate)_profileProvider.DeleteProfile, index);
        
        if (await _dialogService.ShowLoadingDialog(loadingDialog))
        {
            var confirmDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
            confirmDialog.Prepare(true, Constants.SuccessDialog, $"Successfully deleted the profile (id: {index}).");
            await _dialogService.ShowDialog(confirmDialog);
            return true;
        }
        
        // Warn delete profile failed
        var failDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
        failDialog.Prepare(true, Constants.FailDialog, $"Failed to delete the profile (id: {index}).");
        await _dialogService.ShowDialog(failDialog);
        return false;
    }

    private async Task CreateProfileUiAsync()
    {
        // Create the profile creation dialog and display
        var creatingPfDialog = _dialogService.GetDialog<CreateProfileDialogViewModel>();
        creatingPfDialog.Prepare(
            _filesService, // File service
            _profileProvider.GetLoadedProfiles() // Get the loaded profiles
            .Select(p => p.ProfileName)); // Select the names for these profiles
        await _dialogService.ShowDialog(creatingPfDialog);

        // If no creation was requested, cancel
        if (!creatingPfDialog.Confirmed)
            return;
        
        // By default, save the current profile, since we're switching to another profile
        // Save previous profile
        var loadingDialog = _dialogService.GetDialog<LoadingDialogViewModel>();
        loadingDialog.Prepare("Saving currently active profile...", null, (Delegate)_profileProvider.SaveActiveProfile);
        
        if (!await _dialogService.ShowLoadingDialog(loadingDialog))
        {
            // If failed, show a dialog
            var confirmDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
            confirmDialog.Prepare(true, Constants.FailDialog, $"""
                           Failed to switch the profile.
                           The current active profile failed to be saved.
                           If this issue persists, you can try:
                           {Constants.SolutionFilePermissions}
                           """);
            await _dialogService.ShowDialog(confirmDialog);
            return;
        }

        switch (creatingPfDialog.SelectedTabIndex) // Defines what mode to go with
        {
            // 0: Create new original profile
            case 0:
                // Creates profile with name
                loadingDialog = _dialogService.GetDialog<LoadingDialogViewModel>();
                loadingDialog.Prepare("Creating new profile...", null, (Delegate)_profileProvider.AddProfile,
                    creatingPfDialog.ProfileName, // Profile name
                    true); // Destroy exiting storage
                
                // If it worked, success dialog; otherwise, fail dialog
                if (!await _dialogService.ShowLoadingDialog(loadingDialog))
                {
                    var confirmDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
                    confirmDialog.Prepare(true, Constants.FailDialog, "Failed to create the profile!");
                    await _dialogService.ShowDialog(confirmDialog);
                }
                else
                {
                    var confirmDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
                    confirmDialog.Prepare(true, Constants.SuccessDialog, $"Created {creatingPfDialog.ProfileName} with success!");
                    await _dialogService.ShowDialog(confirmDialog);
                }
                break;
            // 1: Create profile based on other profile
            case 1:
                // Creates profile with name
                loadingDialog = _dialogService.GetDialog<LoadingDialogViewModel>();
                loadingDialog.Prepare("Cloning profile...", "Selecting profile and cloning it...", (Delegate)_profileProvider.CloneProfile,
                        creatingPfDialog.CloneProfileName, // Profile name
                        creatingPfDialog.ProfileIndexToClone); // Destroy exiting storage
                
                // If it worked, success dialog; otherwise, fail dialog
                if (!await _dialogService.ShowLoadingDialog(loadingDialog))
                {
                    var confirmDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
                    confirmDialog.Prepare(true, Constants.FailDialog, "Failed to clone the profile!");
                    await _dialogService.ShowDialog(confirmDialog);
                }
                else
                {
                    var confirmDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
                    confirmDialog.Prepare(true, Constants.SuccessDialog, $"Created {creatingPfDialog.CloneProfileName} with success!");
                    await _dialogService.ShowDialog(confirmDialog);
                }
                break;
            
            // 2: Import profile from a pack
            case 2:
                // Creates profile with index
                loadingDialog = _dialogService.GetDialog<LoadingDialogViewModel>();
                loadingDialog.Prepare("Importing profile...", "Selecting profile and importing it...", (Delegate)_profileProvider.ImportProfile,
                        creatingPfDialog.ProfileImportPath); // Profile path
                
                // If it worked, success dialog; otherwise, fail dialog
                if (!await _dialogService.ShowLoadingDialog(loadingDialog))
                {
                    var confirmDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
                    confirmDialog.Prepare(true, Constants.FailDialog, "Failed to clone the profile!");
                    await _dialogService.ShowDialog(confirmDialog);
                }
                else
                {
                    var confirmDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
                    confirmDialog.Prepare(true, Constants.SuccessDialog, $"Imported {Path.GetFileName(creatingPfDialog.ProfileImportPath)} with success!");
                    await _dialogService.ShowDialog(confirmDialog);
                }
                break;
        }
    }

    private async Task SwitchProfileUiAsync(int id)
    {
        if (id < 0 || id >= _allProfiles.Count)
            throw new IndexOutOfRangeException($"Profile ID ({id}) is out of range.");
        
        if (_profileProvider.GetInstanceActiveProfile() == _allProfiles[id]) // Ignores completely
            return;
        
        var confirmViewModel = _dialogService.GetDialog<ConfirmDialogViewModel>();
        confirmViewModel.Prepare(
            null,
            $"Switch to {_allProfiles[id].ProfileName}?",
            "Are you sure you want to switch to this profile?\nAll data from your previous profile will be saved first.",
            "Yes",
            "No"
        );
        // Show confirmation dialog
        await _dialogService.ShowDialog(confirmViewModel);
        
        // Do not if not accepted
        if (!confirmViewModel.Confirmed)
            return;
        
        // Save previous profile
        var loadingDialog = _dialogService.GetDialog<LoadingDialogViewModel>();
        loadingDialog.Prepare("Saving currently active profile...", null, (Delegate)_profileProvider.SaveActiveProfile);

        if (!await _dialogService.ShowLoadingDialog(loadingDialog))
        {
            // If failed, show a dialog
            var confirmDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
            confirmDialog.Prepare(true, Constants.FailDialog, $"""
                          Failed to switch the profile.
                          The current active profile failed to be saved.
                          If this issue persists, you can try:
                          {Constants.SolutionFilePermissions}
                          """);
            await _dialogService.ShowDialog(confirmDialog);
            return;
        }
        
        // Switch profile
        loadingDialog = _dialogService.GetDialog<LoadingDialogViewModel>();
        loadingDialog.Prepare(null, null, (Delegate)_profileProvider.SetActiveProfile, id);
        
        if (!await _dialogService.ShowLoadingDialog(loadingDialog))
        {
            var confirmDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
            confirmDialog.Prepare(true, Constants.FailDialog, $"""
                           Failed to switch the profile ({id}) due to an unknown reason.
                           If this issue persists, you can try:
                           {Constants.SolutionFilePermissions} 
                           """);
            await _dialogService.ShowDialog(confirmDialog);
            return;
        }
        
        // Success dialog
        var successDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
        successDialog.Prepare(true, Constants.SuccessDialog, "You've successfully switched profiles!");
        await _dialogService.ShowDialog(successDialog);
    }

    private async Task WaitToUpdateData(string preferredIndex = "", bool closeProgramIfFail = false)
    {
        var loadingDialog = _dialogService.GetDialog<LoadingDialogViewModel>();
        loadingDialog.Prepare("Updating profile data...", null, (Delegate)_profileProvider.UpdateProfilesData, preferredIndex);
        
        if (!await _dialogService.ShowLoadingDialog(loadingDialog))
        {
            if (closeProgramIfFail)
            {
                // Aggressive dialog
                var confirmDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
                confirmDialog.Prepare(true, Constants.FailDialog, $"""
                              Failed to update the profiles list! The program will close after you close this.
                              If the issue persists, you can try:
                              {Constants.SolutionFilePermissions}
                              """);
                await _dialogService.ShowDialog(confirmDialog);
                // End application
                Environment.Exit(0);
                return;
            }
            
            // Not-so-aggressive dialog
            var failDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
            failDialog.Prepare(true, Constants.FailDialog, $"""
                           Failed to update the profiles list!
                           If the issue persists, you can try:
                           {Constants.SolutionFilePermissions}
                           """);
            await _dialogService.ShowDialog(failDialog);
        }
    }
}

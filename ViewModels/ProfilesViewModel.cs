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

            ObservableUnchangedProfiles.Clear();
            foreach (var profile in _allProfiles)
                ObservableUnchangedProfiles.Add(profile);
            
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
        ObservableProfiles.Clear();
        foreach (var item in _allProfiles)
            ObservableProfiles.Add(item);
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
            await _dialogService.ShowDialog(new ConfirmDialogViewModel(true)
            {
                Title = Constants.FailDialog,
                Message = $"Failed to open the profile.\nFor some reason, their id ({id}) wasn't found!"
            });
            return;
        }

        if (_allProfiles[index].IsProfileMissingMetadata)
        {
            await _dialogService.ShowDialog(new ConfirmDialogViewModel(true, true)
            {
                Title = Constants.WarningDialog,
                Message = """
                          The profile you're attempting to open is missing its metadata! 
                          If you want to know what's inside it, you will need to load this profile.
                          Don't worry, your currently active profile will be saved.
                          """
            });
            return;
        }

        // Create profile viewer
        var profileViewer = new PreviewProfileDialogViewModel(
            _allProfiles[index], 
            _allProfiles.Count > 1, 
            _filesService);
        
        // Loop until the dialog is truly closed
        while (true)
        {
            profileViewer.ResetState(); // Reset dialog state (IsDialogOpen, for example)
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
        var confirmViewModel = new ConfirmDialogViewModel
        {
            Title = $"Export {_allProfiles[index].ProfileName}?",
            Message = "Are you sure you want to export this profile?",
            ConfirmText = "Yes",
            CancelText = "No"
        };

        // Show confirmation dialog
        await _dialogService.ShowDialog(confirmViewModel);
        
        // Do not if not accepted
        if (!confirmViewModel.Confirmed)
            return;
        
        var loadingDialog = new LoadingDialogViewModel(_profileProvider.ExportProfile, index) { Title = "Profile Export", Status = "Exporting profile..."};
        if (!await _dialogService.ShowLoadingDialog(loadingDialog))
        {
            // If it fails, show dialog
            await _dialogService.ShowDialog(new ConfirmDialogViewModel(true, true)
            {
                Title = Constants.FailDialog,
                Message = $"Failed to export the profile! If you're still having issues, try this:\n{Constants.SolutionFilePermissions}"
            });
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
        var confirmViewModel = new ConfirmDialogViewModel
        {
            Title = $"Delete {_allProfiles[index].ProfileName}?",
            Message = "Are you sure you want to delete this profile?",
            ConfirmText = "Yes",
            CancelText = "No"
        };

        // Show confirmation dialog
        await _dialogService.ShowDialog(confirmViewModel);
        
        // Do not if not accepted
        if (!confirmViewModel.Confirmed)
            return false;
        
        var loadingDialog = new LoadingDialogViewModel(_profileProvider.DeleteProfile, index) { Title = "Deleting profile..." };
        if (await _dialogService.ShowLoadingDialog(loadingDialog))
        {
            await _dialogService.ShowDialog(new ConfirmDialogViewModel(true)
            {
                Title = Constants.SuccessDialog,
                Message = $"Successfully deleted the profile (id: {index})."
            });
            return true;
        }
        
        // Warn delete profile failed
        await _dialogService.ShowDialog(new ConfirmDialogViewModel(true, true)
        {
            Title = Constants.FailDialog,
            Message = $"Failed to delete the profile (id: {index})."
        });
        return false;
    }

    private async Task CreateProfileUiAsync()
    {
        // Create the profile creation dialog and display
        var creatingPfDialog = new CreateProfileDialogViewModel(
            _filesService, // File service
            _profileProvider.GetLoadedProfiles() // Get the loaded profiles
            .Select(p => p.ProfileName)); // Select the names for these profiles
        await _dialogService.ShowDialog(creatingPfDialog);

        // If no creation was requested, cancel
        if (!creatingPfDialog.Confirmed)
            return;
        
        // By default, save the current profile, since we're switching to another profile
        // Save previous profile
        var loadingDialog = new LoadingDialogViewModel(_profileProvider.SaveActiveProfile)
        {
            Title = "Saving currently active profile..."
        };
        
        if (!await _dialogService.ShowLoadingDialog(loadingDialog))
        {
            // If failed, show a dialog
            await _dialogService.ShowDialog(new ConfirmDialogViewModel(true, true)
            {
                Title = Constants.FailDialog,
                Message = $"""
                           Failed to switch the profile.
                           The current active profile failed to be saved.
                           If this issue persists, you can try:
                           {Constants.SolutionFilePermissions}
                           """
            });
            return;
        }

        switch (creatingPfDialog.SelectedTabIndex) // Defines what mode to go with
        {
            // 0: Create new original profile
            case 0:
                // Creates profile with name
                loadingDialog = new LoadingDialogViewModel(_profileProvider.AddProfile,
                    creatingPfDialog.ProfileName, // Profile name
                    true) // Destroy exiting storage
                {
                    Title = "Creating new profile..."
                };
                // If it worked, success dialog; otherwise, fail dialog
                if (!await _dialogService.ShowLoadingDialog(loadingDialog))
                {
                    await _dialogService.ShowDialog(new ConfirmDialogViewModel(true)
                    {
                        Title = Constants.FailDialog,
                        Message = "Failed to create the profile!"
                    });
                }
                else
                {
                    await _dialogService.ShowDialog(new ConfirmDialogViewModel(true)
                    {
                        Title = Constants.SuccessDialog,
                        Message = $"Created {creatingPfDialog.ProfileName} with success!"
                    });
                }
                break;
            // 1: Create profile based on other profile
            case 1:
                // Creates profile with name
                loadingDialog = new LoadingDialogViewModel(_profileProvider.CloneProfile,
                        creatingPfDialog.CloneProfileName, // Profile name
                        creatingPfDialog.ProfileIndexToClone) // Destroy exiting storage
                    {
                        Title = "Cloning profile...",
                        Status = "Selecting profile and cloning it..."
                    };
                // If it worked, success dialog; otherwise, fail dialog
                if (!await _dialogService.ShowLoadingDialog(loadingDialog))
                {
                    await _dialogService.ShowDialog(new ConfirmDialogViewModel(true)
                    {
                        Title = Constants.FailDialog,
                        Message = "Failed to clone the profile!"
                    });
                }
                else
                {
                    await _dialogService.ShowDialog(new ConfirmDialogViewModel(true)
                    {
                        Title = Constants.SuccessDialog,
                        Message = $"Created {creatingPfDialog.CloneProfileName} with success!"
                    });
                }
                break;
            
            // 2: Import profile from a pack
            case 2:
                // Creates profile with index
                loadingDialog = new LoadingDialogViewModel(_profileProvider.ImportProfile,
                        creatingPfDialog.ProfileImportPath // Profile path
                        )
                    {
                        Title = "Importing profile...",
                        Status = "Selecting profile and importing it..."
                    };
                // If it worked, success dialog; otherwise, fail dialog
                if (!await _dialogService.ShowLoadingDialog(loadingDialog))
                {
                    await _dialogService.ShowDialog(new ConfirmDialogViewModel(true)
                    {
                        Title = Constants.FailDialog,
                        Message = "Failed to clone the profile!"
                    });
                }
                else
                {
                    await _dialogService.ShowDialog(new ConfirmDialogViewModel(true)
                    {
                        Title = Constants.SuccessDialog,
                        Message = $"Imported {Path.GetFileName(creatingPfDialog.ProfileImportPath)} with success!"
                    });
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
        
        var confirmViewModel = new ConfirmDialogViewModel
        {
            Title = $"Switch to {_allProfiles[id].ProfileName}?",
            Message =
                "Are you sure you want to switch to this profile?\nAll data from your previous profile will be saved first.",
            ConfirmText = "Yes",
            CancelText = "No"
        };
        // Show confirmation dialog
        await _dialogService.ShowDialog(confirmViewModel);
        
        // Do not if not accepted
        if (!confirmViewModel.Confirmed)
            return;
        
        // Save previous profile
        var loadingDialog = new LoadingDialogViewModel(_profileProvider.SaveActiveProfile)
        {
            Title = "Saving currently active profile..."
        };
        if (!await _dialogService.ShowLoadingDialog(loadingDialog))
        {
            // If failed, show a dialog
            await _dialogService.ShowDialog(new ConfirmDialogViewModel(true, true)
            {
                Title = Constants.FailDialog,
                Message = $"""
                          Failed to switch the profile.
                          The current active profile failed to be saved.
                          If this issue persists, you can try:
                          {Constants.SolutionFilePermissions}
                          """
            });
            return;
        }
        
        // Switch profile
        loadingDialog = new LoadingDialogViewModel(_profileProvider.SetActiveProfile, id);
        if (!await _dialogService.ShowLoadingDialog(loadingDialog))
        {
            await _dialogService.ShowDialog(new ConfirmDialogViewModel(true, true)
            {
                Title = Constants.FailDialog,
                Message = $"""
                           Failed to switch the profile ({id}) due to an unknown reason.
                           If this issue persists, you can try:
                           {Constants.SolutionFilePermissions} 
                           """
            });
            return;
        }
        
        // Success dialog
        await _dialogService.ShowDialog(new ConfirmDialogViewModel(true, true)
        {
            Title = Constants.SuccessDialog,
            Message = "You've successfully switched profiles!"
        });
    }

    private async Task WaitToUpdateData(string preferredIndex = "", bool closeProgramIfFail = false)
    {
        var loadingDialog = new LoadingDialogViewModel(_profileProvider.UpdateProfilesData, preferredIndex)
        {
            Title = "Updating profile data..."
        };
        if (!await _dialogService.ShowLoadingDialog(loadingDialog))
        {
            if (closeProgramIfFail)
            {
                // Aggressive dialog
                await _dialogService.ShowDialog(new ConfirmDialogViewModel(true, true)
                {
                    Title = Constants.FailDialog,
                    Message = $"""
                              Failed to update the profiles list! The program will close after you close this.
                              If the issue persists, you can try:
                              {Constants.SolutionFilePermissions}
                              """
                });
                // End application
                Environment.Exit(0);
                return;
            }
            
            // Not-so-aggressive dialog
            await _dialogService.ShowDialog(new ConfirmDialogViewModel(true, true)
            {
                Title = Constants.FailDialog,
                Message = $"""
                           Failed to update the profiles list!
                           If the issue persists, you can try:
                           {Constants.SolutionFilePermissions}
                           """
            });
        }
    }
}
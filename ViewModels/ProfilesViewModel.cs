using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

//TODO: Add an update profiles data button

public partial class ProfilesViewModel : ViewModelBase, IDisposable
{
    private readonly DialogService _dialogService = null!;
    private readonly IProfileProvider _profileProvider = null!;
    private readonly SettingsService _settingsService = null!;
    private readonly IFilesService _filesService = null!;
    private readonly List<ProfileItem> _allProfiles = [];
    private ProfileItem? _lastSelectedItem;
    
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
    public ProfilesViewModel(DialogService dialogService, ProfileProvider profileProvider, FilesService filesService, SettingsService settingsService)
    {
        if (Design.IsDesignMode) return;
        
        // Services
        _dialogService = dialogService;
        _filesService = filesService;
        _settingsService = settingsService;
        _profileProvider = profileProvider;
        _profileProvider.OnProfilesUpdate += ProfilesProvider_OnProfilesUpdate;
        
        // Update profile data
        Dispatcher.UIThread.AwaitWithPriority(WaitToUpdateData(_settingsService.CurrentSettings.CurrentProfileSet), DispatcherPriority.Loaded);
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
            _settingsService.CurrentSettings.CurrentProfileSet = _profileProvider.GetActiveProfile();
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
            // TODO: Open dialog for not succeeding opening it
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

            break;
        }
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

        // TODO: Display a progress bar dialog for this process
        if (!await _profileProvider.DeleteProfile(index))
        {
            // TODO: Show dialog warning something went wrong during deletion! And cancel, of course.
            return false;
        }
        return true;
    }

    private async Task CreateProfileUiAsync()
    {
        // TODO: Display a progress bar dialog for this process
        if (!await _profileProvider.AddProfile("SomeRandomProfile_" + new Random().Next(0, 256)))
        {
            // TODO: Create a fail dialog here
            return;
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
            Message = "Are you sure you want to switch to this profile?\nAll data from your previous profile will be saved first.",
            ConfirmText = "Yes",
            CancelText = "No"
        };

        // Show confirmation dialog
        await _dialogService.ShowDialog(confirmViewModel);
        
        // Do not if not accepted
        if (!confirmViewModel.Confirmed)
            return;
        
        // Save previous profile
        if (!await _profileProvider.SaveActiveProfile())
        {
            // If failed, show a dialog
            // TODO: Create a dialog to warn it failed.
            return;
        }

        var previousId = _profileProvider.GetActiveProfile();
        
        // Switch profile
        if (!await _profileProvider.SetActiveProfile(id))
        {
            // TODO: Create a dialog saying something went wrong and that it'll revert back to the previous profile
            return;
        }
        
        // TODO: Maybe a dialog saying the profile change was a success?
    }

    private async Task WaitToUpdateData(int preferredIndex = -1, bool closeProgramIfFail = false)
    {
        // TODO: Create a progress bar for updating data
        if (!await _profileProvider.UpdateProfilesData(defaultSelection: preferredIndex))
        {
            if (closeProgramIfFail)
            {
                // TODO: Create a dialog telling the user what to do to prevent the issue
                return;
            }
            
            // TODO: Create a dialog warning something went wrong when loading the profiles
        }
    }
}
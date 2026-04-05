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
using GottaManagePlus.Interfaces.ProfileManagement;
using GottaManagePlus.Models;
using GottaManagePlus.Services;
using GottaManagePlus.Services.ExplorerServices;
using GottaManagePlus.Services.GameEnvironmentServices;
using GottaManagePlus.Services.ProfileServices;
using GottaManagePlus.Utils;

namespace GottaManagePlus.ViewModels;

public partial class ProfilesViewModel : PageViewModel, IDisposable
{
    // ---- Private API ----
    private readonly DialogService _dialogService = null!;
    private readonly ProfileManager _profileManager = null!;
    private readonly SettingsService _settingsService = null!;
    private readonly ProfileRepository _profileRepository = null!;
    private readonly IProfileExportController _profileExportController = null!;
    private readonly IProfileDestructor _destructor = null!;
    private readonly IProfileCreator _profileCreator = null!;
    private readonly IProfileCloner _profileCloner = null!;
    private readonly GameEnvironmentController _environmentController = null!;
    private readonly DirectoryLauncher _directoryLauncher = null!;
    
    // Getters
    private readonly Dictionary<int, ProfileMetadata> _allProfiles = [];
    private void FillUpAllProfiles(IEnumerable<ProfileMetadata>? profiles) { _allProfiles.Clear(); var index = 0; foreach (var profile in profiles ?? []) _allProfiles.Add(index++, profile); }
    
    // Observable Properties
    [ObservableProperty] 
    private ObservableCollection<ProfileMetadata> _observableUnchangedProfiles = [];
    [ObservableProperty] 
    private ObservableCollection<ProfileMetadata> _observableProfiles = [];
    [ObservableProperty]
    private ProfileMetadata? _currentProfileMetadata;
    [ObservableProperty]
    private string? _text;
    
    // From the generator. https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/observableproperty
    partial void OnCurrentProfileMetadataChanged(ProfileMetadata? value) => UpdateProfilesList(value); 
    
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
    public ProfilesViewModel() : base(PageNames.Profiles)
    {
        if (!Design.IsDesignMode) return;
        
        // Initialize Data
        FillUpAllProfiles([
        new ProfileMetadata 
        { 
            Name = "Baldi's Basics Ultimate Mod Pack", 
            EstimatedBytesLength = 4523000, 
            Description = "A comprehensive collection of all major gameplay enhancements for Baldi's Basics.", 
            CreationDate = DateTime.Now.AddYears(-1).AddMonths(2), 
            LastUpdateDate = DateTime.Now.AddDays(-5) 
        },
        new ProfileMetadata 
        { 
            Name = "No More Chasing Mode", 
            EstimatedBytesLength = 120500, 
            Description = "Removes Baldi from chasing you entirely. Use at your own risk!", 
            CreationDate = DateTime.Now.AddMonths(6), 
            LastUpdateDate = DateTime.Now.AddDays(-1) 
        },
        new ProfileMetadata 
        { 
            Name = "Speedy Runners Only", 
            EstimatedBytesLength = 89000, 
            Description = "Increases player movement speed by 200% and reduces stamina drain.", 
            CreationDate = DateTime.Now.AddMonths(3), 
            LastUpdateDate = DateTime.Now.AddDays(-12) 
        },
        new ProfileMetadata 
        { 
            Name = "Classic 1997 Skin Pack", 
            EstimatedBytesLength = 2340000, 
            Description = "Restores original textures and sounds from the very first demo release.", 
            CreationDate = DateTime.Now.AddYears(1), 
            LastUpdateDate = DateTime.Now.AddMonths(-1) 
        },
        new ProfileMetadata 
        { 
            Name = "Math Problem Chaos", 
            EstimatedBytesLength = 156000, 
            Description = "Changes all math problems to be unsolvable without a calculator item.", 
            CreationDate = DateTime.Now.AddMonths(8), 
            LastUpdateDate = DateTime.Now.AddDays(-3) 
        },
        new ProfileMetadata 
        { 
            Name = "Gotta Go Fast (True)", 
            EstimatedBytesLength = 98000, 
            Description = "An experimental profile that removes all obstacles and doors in the school.", 
            CreationDate = DateTime.Now.AddDays(15), 
            LastUpdateDate = DateTime.Now 
        },
        new ProfileMetadata 
        { 
            Name = "Principal's Office Escape", 
            EstimatedBytesLength = 340000, 
            Description = "Shortens the time required to escape the principal's office significantly.", 
            CreationDate = DateTime.Now.AddMonths(2), 
            LastUpdateDate = DateTime.Now.AddDays(-7) 
        },
        new ProfileMetadata 
        { 
            Name = "Hidden Items Revealer", 
            EstimatedBytesLength = 67000, 
            Description = "Highlights hidden notebooks and items on the map automatically.", 
            CreationDate = DateTime.Now.AddMonths(1), 
            LastUpdateDate = DateTime.Now.AddDays(-20) 
        },
        new ProfileMetadata 
        { 
            Name = "Randomized Hallway Layout", 
            EstimatedBytesLength = 450000, 
            Description = "Shuffles hallway connections every time you enter a new room for maximum confusion.", 
            CreationDate = DateTime.Now.AddYears(1).AddMonths(6), 
            LastUpdateDate = DateTime.Now.AddDays(-45) 
        },
        new ProfileMetadata 
        { 
            Name = "Audio Overhaul", 
            EstimatedBytesLength = 1890000, 
            Description = "Replaces all sound effects with high-quality remastered audio tracks.", 
            CreationDate = DateTime.Now.AddMonths(4), 
            LastUpdateDate = DateTime.Now.AddDays(-2) 
        },
        new ProfileMetadata 
        { 
            Name = "Infinite Notebook Spawns", 
            EstimatedBytesLength = 22000, 
            Description = "Notebooks spawn infinitely in random locations to help you reach 100% completion easily.", 
            CreationDate = DateTime.Now.AddDays(5), 
            LastUpdateDate = DateTime.Now 
        },
        new ProfileMetadata 
        { 
            Name = "Glitchy Reality", 
            EstimatedBytesLength = 560000, 
            Description = "Introduces visual glitches and texture flickering to mimic a corrupted game file.", 
            CreationDate = DateTime.Now.AddMonths(5), 
            LastUpdateDate = DateTime.Now.AddDays(-10) 
        },
        new ProfileMetadata 
        { 
            Name = "Quiet School", 
            EstimatedBytesLength = 110000, 
            Description = "Silences all background music and ambient noise for a spooky atmosphere.", 
            CreationDate = DateTime.Now.AddMonths(12), 
            LastUpdateDate = DateTime.Now.AddDays(-8) 
        },
        new ProfileMetadata 
        { 
            Name = "Hardcore Survival", 
            EstimatedBytesLength = 78000, 
            Description = "One hit kills you if you don't collect enough sanity items. No second chances.", 
            CreationDate = DateTime.Now.AddMonths(7), 
            LastUpdateDate = DateTime.Now.AddDays(-1) 
        },
        new ProfileMetadata 
        { 
            Name = "Community Favorites Vol. 1", 
            EstimatedBytesLength = 3200000, 
            Description = "A curated list of the top-rated community mods combined into one seamless experience.", 
            CreationDate = DateTime.Now.AddYears(1).AddMonths(3), 
            LastUpdateDate = DateTime.Now.AddDays(-15) 
        }
    ]);
        
        // Initialize collections
        ObservableProfiles = new ObservableCollection<ProfileMetadata>(_allProfiles.Values);
        ObservableUnchangedProfiles = new ObservableCollection<ProfileMetadata>(_allProfiles.Values);
    }
    
    // DI Constructor
    public ProfilesViewModel(
        DialogService dialogService, 
        ProfileManager profileManager, 
        DirectoryLauncher directoryLauncher,
        SettingsService settingsService,
        ProfileRepository profileRepository,
        IProfileExportController profileExportController,
        IProfileDestructor destructor,
        IProfileCreator profileCreator,
        IProfileCloner profileCloner,
        GameEnvironmentController environmentController) : base(PageNames.Profiles)
    {
        if (Design.IsDesignMode) return;
        
        // Services
        _dialogService = dialogService;
        _directoryLauncher = directoryLauncher;
        _settingsService = settingsService;
        _profileRepository = profileRepository;
        _profileExportController = profileExportController;
        _destructor = destructor;
        _profileCreator = profileCreator;
        _profileCloner = profileCloner;
        _environmentController = environmentController;
        _profileManager = profileManager;
        
        // Event Listening
        _profileRepository.OnProfilesUpdate += ProfilesProvider_OnProfilesUpdate;
        
        // Update profile data on random thread
        Dispatcher.UIThread.InvokeAsync(() => WaitToUpdateData(_settingsService.CurrentSettings.CurrentProfileSet), DispatcherPriority.Loaded);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _profileRepository.OnProfilesUpdate -= ProfilesProvider_OnProfilesUpdate;
    }
    
    private void ProfilesProvider_OnProfilesUpdate(ProfileRepository provider)
    {
        // Initialize Data
        Dispatcher.UIThread.Post(() => 
        {
            // Fill up the profiles with new IDs
            FillUpAllProfiles(provider.GetAll());
            ObservableUnchangedProfiles = new ObservableCollection<ProfileMetadata>(_allProfiles.Values);
            
            ResetListVisibleConfigurations();
            
            // Update profile settings
            _settingsService.CurrentSettings.CurrentProfileSet = _profileManager.ActiveProfile?.Name ?? ProfileMetadata.DefaultName;
        });
    }
    
    // Private methods
    private void UpdateProfilesList(ProfileMetadata? highlightedItem)
    {
        // If item is not null, insert it at the top
        if (highlightedItem != null)
        {
            ObservableProfiles = new ObservableCollection<ProfileMetadata>(
                ObservableProfiles.OrderByDescending(profile => highlightedItem.Name.ManyStartWith(profile.Name)
                ));
            return;
        }
        // If highlighted item is null or not found, just reset the whole list
        ObservableProfiles = new ObservableCollection<ProfileMetadata>(_allProfiles.Values);
    }

    private void ResetListVisibleConfigurations() // Basically reset the observable list
    {
        UpdateProfilesList(null);
        ResetSearch();
    }

    private async Task OpenProfileMetaDataAndHandleActions(int id)
    {
        if (!_allProfiles.TryGetValue(id, out var profile)) // If the item doesn't exist, skip
        {
            var confirmDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
            confirmDialog.Prepare(true, Constants.FailDialog, $"Failed to open the profile.\nFor some reason, their id ({id}) wasn't found!");
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
                profile, 
                _allProfiles.Count > 1, 
                _directoryLauncher,
                _dialogService,
                _environmentController);
            await _dialogService.ShowDialog(profileViewer);
            
            if (profileViewer.ShouldDeleteProfile)
            {
                // If deleting the item works, then we don't need to loop back
                if (await DeleteProfileMetadataUiAsync(id)) break;

                continue; // Loop back, since that wasn't a normal close
            }

            if (profileViewer.ShouldExportProfile)
            {
                await ExportProfileMetadata(id);
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

    private async Task ExportProfileMetadata(int index)
    {
        var confirmViewModel = _dialogService.GetDialog<ConfirmDialogViewModel>();
        confirmViewModel.Prepare(
            null,
            $"Export {_allProfiles[index].Name}?",
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
        loadingDialog.Prepare("Profile Export", "Exporting profile...", (Delegate)_profileExportController.ExportProfile, _allProfiles[index]);
        
        if (!await _dialogService.ShowDialog(loadingDialog))
        {
            // If it fails, show dialog
            var confirmDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
            confirmDialog.Prepare(true, Constants.FailDialog, $"Failed to export the profile! If you're still having issues, try this:\n{Constants.SolutionFilePermissions}");
            await _dialogService.ShowDialog(confirmDialog);
            return;
        }
        // Success action (open the export)
        await _directoryLauncher.OpenDirectoryInfo(new DirectoryInfo(_environmentController.GetOrCreateProfilesExportFolderPath()));
    }
    
    private async Task<bool> DeleteProfileMetadataUiAsync(int index) // Delete asynchronously the items
    {
        var confirmViewModel = _dialogService.GetDialog<ConfirmDialogViewModel>();
        confirmViewModel.Prepare(
            null,
            $"Delete {_allProfiles[index].Name}?",
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
        loadingDialog.Prepare("Deleting profile...", null, (Delegate)_destructor.DeleteProfile, _allProfiles[index]);
        
        if (await _dialogService.ShowDialog(loadingDialog))
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
            _directoryLauncher, // File service
            _profileRepository.GetAll() // Get the loaded profiles
            .Select(p => p.Name)); // Select the names for these profiles
        await _dialogService.ShowDialog(creatingPfDialog);

        // If no creation was requested, cancel
        if (!creatingPfDialog.Confirmed)
            return;
        
        // By default, save the current profile, since we're switching to another profile
        // Save previous profile
        var loadingDialog = _dialogService.GetDialog<LoadingDialogViewModel>();
        loadingDialog.Prepare("Saving currently active profile...", null, (Delegate)_profileManager.SaveActiveProfile);
        
        if (!await _dialogService.ShowDialog(loadingDialog))
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
                loadingDialog.Prepare("Creating new profile...", null, (Delegate)_profileCreator.CreateProfile,
                    new ProfileMetadata { Name = creatingPfDialog.ProfileName ?? "Another Profile" }, // Profile name
                    true); // Destroy exiting storage
                
                // If it worked, success dialog; otherwise, fail dialog
                if (!await _dialogService.ShowDialog(loadingDialog))
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
                loadingDialog.Prepare("Cloning profile...", "Selecting profile and cloning it...",
                    (Delegate)_profileCloner.CloneProfile,
                    creatingPfDialog.CloneProfileName); // Profile name
                
                // If it worked, success dialog; otherwise, fail dialog
                if (!await _dialogService.ShowDialog(loadingDialog))
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
                if (!await _dialogService.ShowDialog(loadingDialog))
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

        if (!await _dialogService.ShowDialog(loadingDialog))
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
        
        if (!await _dialogService.ShowDialog(loadingDialog))
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
        
        if (!await _dialogService.ShowDialog(loadingDialog))
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

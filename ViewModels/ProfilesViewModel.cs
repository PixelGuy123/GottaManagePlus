using System;
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
using Serilog;

namespace GottaManagePlus.ViewModels;

public partial class ProfilesViewModel : PageViewModel, IDisposable
{
    // ---- Dependencies ----
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

    // ---- Observable Properties ----
    [ObservableProperty] private ObservableCollection<ProfileMetadata> _observableProfiles = [];
    [ObservableProperty] private ObservableCollection<ProfileMetadata> _observableUnchangedProfiles = [];
    [ObservableProperty] private ProfileMetadata? _currentProfileMetadata;
    [ObservableProperty] private string? _text;

    // ---- Event Handlers & Commands ----
    partial void OnCurrentProfileMetadataChanged(ProfileMetadata? value) => UpdateProfilesUiList(value);

    [RelayCommand] public void ResetSearch() => Text = null;
    [RelayCommand] public async Task OpenProfileMetadata(ProfileMetadata profile) => await OpenProfileMetaDataAndHandleActions(profile);
    [RelayCommand] public async Task CreateProfileUi() => await CreateProfileUiAsync();
    [RelayCommand] public async Task SwitchToProfile(ProfileMetadata profile) => await SwitchProfileUiAsync(profile);
    [RelayCommand] public async Task UpdateProfiles() => await UpdateProfilesList();

    // ---- Design-Time Constructor ----
    public ProfilesViewModel() : base(PageNames.LogViewer)
    {
        if (!Design.IsDesignMode) return;
        ObservableUnchangedProfiles = new ObservableCollection<ProfileMetadata>(
        [
            new ProfileMetadata { Name = "Design Profile 1" },
            new ProfileMetadata { Name = "Design Profile 2" },
            new ProfileMetadata { Name = "Design Profile 3 with very long name for testing out of bounds text" }
        ]);
        ObservableProfiles = new ObservableCollection<ProfileMetadata>(ObservableUnchangedProfiles);
    }

    // ---- Dependency Injection Constructor ----
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
        GameEnvironmentController environmentController) : base(PageNames.LogViewer)
    {
        if (Design.IsDesignMode) return;

        _dialogService = dialogService;
        _profileManager = profileManager;
        _directoryLauncher = directoryLauncher;
        _settingsService = settingsService;
        _profileRepository = profileRepository;
        _profileExportController = profileExportController;
        _destructor = destructor;
        _profileCreator = profileCreator;
        _profileCloner = profileCloner;
        _environmentController = environmentController;

        _profileRepository.OnProfilesUpdate += ProfilesProvider_OnProfilesUpdate;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _profileRepository.OnProfilesUpdate -= ProfilesProvider_OnProfilesUpdate;
    }

    // ---- Repository Event Handling ----
    private void ProfilesProvider_OnProfilesUpdate(ProfileRepository provider)
    {
        Dispatcher.UIThread.Post(() =>
        {
            ObservableUnchangedProfiles = new ObservableCollection<ProfileMetadata>(provider.GetAll());
            ResetListVisibleConfigurations();
            
        });
    }

    // ---- UI List Management ----
    private void UpdateProfilesUiList(ProfileMetadata? highlightedItem)
    {
        if (highlightedItem != null)
        {
            ObservableProfiles = new ObservableCollection<ProfileMetadata>(
                ObservableUnchangedProfiles.OrderByDescending(profile => highlightedItem.Name.ManyStartWith(profile.Name)));
            return;
        }
        ObservableProfiles = new ObservableCollection<ProfileMetadata>(ObservableUnchangedProfiles);
    }

    private void ResetListVisibleConfigurations()
    {
        UpdateProfilesUiList(null);
        ResetSearch();
    }

    // ---- Profile Operations ----
    private async Task OpenProfileMetaDataAndHandleActions(ProfileMetadata profile)
    {
        var profileViewer = _dialogService.GetDialog<PreviewProfileDialogViewModel>();

        while (true)
        {
            profileViewer.Prepare(profile, ObservableUnchangedProfiles.Count > 1, _directoryLauncher, _dialogService, _environmentController);
            await _dialogService.ShowDialog(profileViewer);

            if (profileViewer.ShouldDeleteProfile)
            {
                if (await DeleteProfileMetadataUiAsync(profile)) break;
                continue;
            }

            if (profileViewer.ShouldExportProfile)
            {
                await ExportProfileMetadata(profile);
                continue;
            }

            if (profileViewer.SubDialogView != null)
            {
                await _dialogService.ShowDialog(profileViewer.SubDialogView);
                continue;
            }

            break;
        }
    }

    private async Task ExportProfileMetadata(ProfileMetadata profile)
    {
        if (!await _dialogService.PromptUserQuestion(
                $"Export {profile.Name}?", 
                "Are you sure you want to export this profile?")) return;

        if (!await _dialogService.GenerateLoadingProcess(
                failDialogDescription:
                $"Failed to export the profile! If you're still having issues, try this:\n{Constants.CommonIssuesSolution}",
                successDialogDescription: null,
                "Profile Export", "Exporting profile...", (Delegate)_profileExportController.ExportProfile, profile
            ))
            return;
        
        await _directoryLauncher.OpenDirectoryInfo(new DirectoryInfo(_environmentController.GetOrCreateProfilesExportFolderPath()));
    }

    private async Task<bool> DeleteProfileMetadataUiAsync(ProfileMetadata profile)
    {
        if (!await _dialogService.PromptUserQuestion(
                $"Delete {profile.Name}?", 
                "Are you sure you want to delete this profile?")) 
            return false;

        return await _dialogService.GenerateLoadingProcess(
            $"Failed to delete the profile ({profile.Name}).",
            $"Successfully deleted the profile ({profile.Name}).",
            "Deleting profile...", null, (Delegate)_destructor.DeleteProfile, profile
        );
    }

    private async Task CreateProfileUiAsync()
    {
        var creatingPfDialog = _dialogService.GetDialog<CreateProfileDialogViewModel>();
        creatingPfDialog.Prepare(_directoryLauncher, ObservableUnchangedProfiles.Select(p => p.Name));
        await _dialogService.ShowDialog(creatingPfDialog);

        if (!creatingPfDialog.Confirmed) return;

         switch (creatingPfDialog.SelectedTabIndex)
        {
            case 0: // Create New
            {
                await _dialogService.GenerateLoadingProcess(
                    "Failed to create the profile!",
                    $"Created {creatingPfDialog.ProfileName} successfully!",
                    "Creating new profile...", null, (Delegate)_profileCreator.CreateProfile,
                    new ProfileMetadata { Name = creatingPfDialog.ProfileName ?? "New Profile" }, true);
                break;
            }
            case 1: // Clone
            {
                // Resolve source profile instance by name if the dialog only exposes strings
                var sourceProfile = ObservableUnchangedProfiles.FirstOrDefault(p => p.Name == creatingPfDialog.CloneProfileName);
                if (sourceProfile == null) return;

                await _dialogService.GenerateLoadingProcess(
                    "Failed to clone the profile!",
                    $"Cloned to {creatingPfDialog.ProfileName} successfully!",
                    "Cloning profile...", "Selecting profile and cloning it...",
                    (Delegate)_profileCloner.CloneProfile, sourceProfile, creatingPfDialog.ProfileName);
                break;
            }
            case 2: // Import
            {
                await _dialogService.GenerateLoadingProcess(
                    "Failed to import the profile!",
                    $"Imported {Path.GetFileName(creatingPfDialog.ProfileImportPath)} successfully!",
                    "Importing profile...", "Selecting profile and importing it...",
                    (Delegate)_profileExportController.ExtractExportedProfile, creatingPfDialog.ProfileImportPath);
                break;
            }
        }
    }

    private async Task SwitchProfileUiAsync(ProfileMetadata profile)
    {
        if (_profileManager.ActiveProfile == profile) return;
        
        if (!await _dialogService.PromptUserQuestion(
                    $"Switch to {profile.Name}?", 
                    "Are you sure you want to switch to this profile?")) 
            return;

        // Loading process
        await _dialogService.GenerateLoadingProcess(
            $"Failed to switch the profile ({profile.Name}) due to an unknown reason.\nIf this issue persists, you can try:\n{Constants.CommonIssuesSolution}",
            $"Successfully switched to {profile.Name}!",
            null, null, (Delegate)_profileManager.SetActiveProfile, profile
        );
    }

    private async Task UpdateProfilesList()
    {
        await _dialogService.GenerateLoadingProcess(
            "Failed to update the profiles list! Rolling back on changes...",
            "Successfully updated the profiles list.",
            null, null, (Delegate)_profileManager.UpdateProfileRepository,
            _settingsService.CurrentSettings.CurrentProfileSet);
    }
}
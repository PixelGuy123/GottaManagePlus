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
    partial void OnCurrentProfileMetadataChanged(ProfileMetadata? value) => UpdateProfilesList(value);

    [RelayCommand] public void ResetSearch() => Text = null;
    [RelayCommand] public async Task OpenProfileMetadata(ProfileMetadata profile) => await OpenProfileMetaDataAndHandleActions(profile);
    [RelayCommand] public async Task CreateProfileUi() => await CreateProfileUiAsync();
    [RelayCommand] public async Task SwitchToProfile(ProfileMetadata profile) => await SwitchProfileUiAsync(profile);

    // ---- Design-Time Constructor ----
    public ProfilesViewModel() : base(PageNames.Profiles)
    {
        if (!Design.IsDesignMode) return;
        ObservableUnchangedProfiles = new ObservableCollection<ProfileMetadata>(
        [
            new ProfileMetadata { Name = "Design Profile 1", Description = "Sample design data." },
            new ProfileMetadata { Name = "Design Profile 2", Description = "Sample design data." }
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
        GameEnvironmentController environmentController) : base(PageNames.Profiles)
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
            _settingsService.CurrentSettings.CurrentProfileSet = _profileManager.ActiveProfile?.Name ?? ProfileMetadata.DefaultName;
        });
    }

    // ---- UI List Management ----
    private void UpdateProfilesList(ProfileMetadata? highlightedItem)
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
        UpdateProfilesList(null);
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
        var confirmViewModel = _dialogService.GetDialog<ConfirmDialogViewModel>();
        confirmViewModel.Prepare(null, $"Export {profile.Name}?", "Are you sure you want to export this profile?", "Yes", "No");
        await _dialogService.ShowDialog(confirmViewModel);
        if (!confirmViewModel.Confirmed) return;

        var loadingDialog = _dialogService.GetDialog<LoadingDialogViewModel>();
        loadingDialog.Prepare("Profile Export", "Exporting profile...", (Delegate)_profileExportController.ExportProfile, profile);

        if (!await _dialogService.ShowDialog(loadingDialog))
        {
            var failDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
            failDialog.Prepare(true, Constants.FailDialog, $"Failed to export the profile! If you're still having issues, try this:\n{Constants.SolutionFilePermissions}");
            await _dialogService.ShowDialog(failDialog);
            return;
        }
        
        await _directoryLauncher.OpenDirectoryInfo(new DirectoryInfo(_environmentController.GetOrCreateProfilesExportFolderPath()));
    }

    private async Task<bool> DeleteProfileMetadataUiAsync(ProfileMetadata profile)
    {
        var confirmViewModel = _dialogService.GetDialog<ConfirmDialogViewModel>();
        confirmViewModel.Prepare(null, $"Delete {profile.Name}?", "Are you sure you want to delete this profile?", "Yes", "No");
        await _dialogService.ShowDialog(confirmViewModel);
        if (!confirmViewModel.Confirmed) return false;

        var loadingDialog = _dialogService.GetDialog<LoadingDialogViewModel>();
        loadingDialog.Prepare("Deleting profile...", null, (Delegate)_destructor.DeleteProfile, profile);

        if (await _dialogService.ShowDialog(loadingDialog))
        {
            var successDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
            successDialog.Prepare(true, Constants.SuccessDialog, $"Successfully deleted the profile ({profile.Name}).");
            await _dialogService.ShowDialog(successDialog);
            return true;
        }

        var failDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
        failDialog.Prepare(true, Constants.FailDialog, $"Failed to delete the profile ({profile.Name}).");
        await _dialogService.ShowDialog(failDialog);
        return false;
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
                var loadingDialog = _dialogService.GetDialog<LoadingDialogViewModel>();
                loadingDialog.Prepare("Creating new profile...", null, (Delegate)_profileCreator.CreateProfile,
                    new ProfileMetadata { Name = creatingPfDialog.ProfileName ?? "New Profile" }, true);

                if (!await _dialogService.ShowDialog(loadingDialog))
                {
                    var failDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
                    failDialog.Prepare(true, Constants.FailDialog, "Failed to create the profile!");
                    await _dialogService.ShowDialog(failDialog);
                }
                else
                {
                    var successDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
                    successDialog.Prepare(true, Constants.SuccessDialog, $"Created {creatingPfDialog.ProfileName} successfully!");
                    await _dialogService.ShowDialog(successDialog);
                }
                break;
            }
            case 1: // Clone
            {
                // Resolve source profile instance by name if the dialog only exposes strings
                var sourceProfile = ObservableUnchangedProfiles.FirstOrDefault(p => p.Name == creatingPfDialog.CloneProfileName);
                if (sourceProfile == null) return;

                var loadingDialog = _dialogService.GetDialog<LoadingDialogViewModel>();
                loadingDialog.Prepare("Cloning profile...", "Selecting profile and cloning it...",
                    (Delegate)_profileCloner.CloneProfile, sourceProfile, creatingPfDialog.ProfileName);

                if (!await _dialogService.ShowDialog(loadingDialog))
                {
                    var failDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
                    failDialog.Prepare(true, Constants.FailDialog, "Failed to clone the profile!");
                    await _dialogService.ShowDialog(failDialog);
                }
                else
                {
                    var successDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
                    successDialog.Prepare(true, Constants.SuccessDialog, $"Cloned to {creatingPfDialog.ProfileName} successfully!");
                    await _dialogService.ShowDialog(successDialog);
                }
                break;
            }
            case 2: // Import
            {
                var loadingDialog = _dialogService.GetDialog<LoadingDialogViewModel>();
                loadingDialog.Prepare("Importing profile...", "Selecting profile and importing it...",
                    (Delegate)_profileExportController.ExtractExportedProfile, creatingPfDialog.ProfileImportPath);

                if (!await _dialogService.ShowDialog(loadingDialog))
                {
                    var failDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
                    failDialog.Prepare(true, Constants.FailDialog, "Failed to import the profile!");
                    await _dialogService.ShowDialog(failDialog);
                }
                else
                {
                    var successDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
                    successDialog.Prepare(true, Constants.SuccessDialog, $"Imported {Path.GetFileName(creatingPfDialog.ProfileImportPath)} successfully!");
                    await _dialogService.ShowDialog(successDialog);
                }
                break;
            }
        }
    }

    private async Task SwitchProfileUiAsync(ProfileMetadata profile)
    {
        if (_profileManager.ActiveProfile == profile) return;

        var confirmViewModel = _dialogService.GetDialog<ConfirmDialogViewModel>();
        confirmViewModel.Prepare(null, $"Switch to {profile.Name}?", "Are you sure you want to switch to this profile?", "Yes", "No");
        await _dialogService.ShowDialog(confirmViewModel);
        if (!confirmViewModel.Confirmed) return;

        var loadingDialog = _dialogService.GetDialog<LoadingDialogViewModel>();
        loadingDialog.Prepare(null, null, (Delegate)_profileManager.SetActiveProfile, profile);

        if (!await _dialogService.ShowDialog(loadingDialog))
        {
            var failDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
            failDialog.Prepare(true, Constants.FailDialog, $"Failed to switch the profile ({profile.Name}) due to an unknown reason.\nIf this issue persists, you can try:\n{Constants.SolutionFilePermissions}");
            await _dialogService.ShowDialog(failDialog);
            return;
        }

        var successDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
        successDialog.Prepare(true, Constants.SuccessDialog, $"You've successfully switched to {profile.Name}!");
        await _dialogService.ShowDialog(successDialog);
    }
}
/*
This file is part of GottaManagePlus (https://github.com/PixelGuy123/GottaManagePlus)

    Copyright (C) 2026 PixelGuy123

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.

*/

using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GottaManagePlus.Factories;
using GottaManagePlus.Interfaces;
using GottaManagePlus.Interfaces.ProfileManagement;
using GottaManagePlus.Models;
using GottaManagePlus.Models.DialogManagement;
using GottaManagePlus.Models.UI;
using GottaManagePlus.Services;
using GottaManagePlus.Services.ExplorerServices;
using GottaManagePlus.Services.GameEnvironmentServices;
using GottaManagePlus.Services.ProfileServices;
using GottaManagePlus.Utils;

namespace GottaManagePlus.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDialogProvider
{
    private readonly PageFactory? _pageFactory;
    private readonly DialogService _dialogService = null!;
    private readonly GameEnvironmentController _gameEnvironmentController = null!;
    private readonly SettingsService _settingsService = null!;
    private readonly ProfileManager _profileManager = null!;
    private readonly ApplicationManager _applicationManager = null!;
    private readonly IProfileExportController _profileExportController = null!;
    private readonly IProfileDestructor _profileDestructor = null!;
    private readonly IProfileCreator _profileCreator = null!;
    private readonly IProfileCloner _profileCloner = null!;
    private readonly FilePicker _filePicker = null!;
    private readonly DirectoryLauncher _directoryLauncher = null!;
    private readonly ProfileRepository _profileRepository = null!;


    // Observable Properties
    [ObservableProperty]
    public partial PageViewModel? CurrentPage { get; set; }

    [ObservableProperty]
    public partial DialogViewModel? Dialog { get; set; }

    [ObservableProperty] 
    public partial bool SideMenuOpen { get; set; } = Design.IsDesignMode;
    [ObservableProperty]

    public partial bool ExecutablePathSet { get; set; } = Design.IsDesignMode;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteProfileUiCommand))]
    public partial int ProfileCount { get; set; }

    [ObservableProperty]
    public partial ProfileMetadata? SelectedProfile { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<ProfileMetadata>? ProfileMetadataCollection { get; set; }

    // Update Value
    partial void OnProfileMetadataCollectionChanged(ObservableCollection<ProfileMetadata>? value) =>
        ProfileCount = value?.Count ?? 0;

    [RelayCommand]
    public void ToggleSideMenu() => SideMenuOpen = !SideMenuOpen;

    [RelayCommand]
    public void GoToHome()
    {
        if (ExecutablePathSet)
            GoTo<MyModsViewModel>();
    }

    [RelayCommand]
    public void GoToSettings() => GoTo<SettingsViewModel>();

    [RelayCommand]
    public async Task RevealAboutSection() => await RevealAboutSectionUi();

    [RelayCommand]
    public async Task RevealLicense() => await RevealLicenseUi();

    [RelayCommand]
    public async Task CreateProfileUi() => await CreateProfileUiAsync();
    
    [RelayCommand]
    public async Task DeleteProfileUi() => await DeleteProfileMetadataUiAsync(_profileManager.ActiveProfile!);
    
    [RelayCommand]
    public async Task ExportProfileUi() => await ExportProfileMetadata(_profileManager.ActiveProfile!);

    [RelayCommand]
    public async Task SwitchToProfileUi(ProfileMetadata profile) => await SwitchProfileUiAsync(profile);

    // For Designer only
    public MainWindowViewModel()
    {
        if (!Design.IsDesignMode) return;

        CurrentPage = new MyModsViewModel(); // Default page
        ProfileMetadataCollection = [ProfileMetadata.Default, ProfileMetadata.Default];
        ProfileMetadataCollection[1].Name = "Secondary Default";
        ProfileCount = ProfileMetadataCollection.Count;
        SelectedProfile = ProfileMetadataCollection[0];
    }

    // Constructor
    public MainWindowViewModel(
        PageFactory pageFactory,
        DialogService dialogService,
        GameEnvironmentController gameEnvironmentController,
        SettingsService settingsService,
        ProfileRepository profileRepository,
        ProfileManager profileManager,
        ApplicationManager applicationManager,
        IProfileExportController profileExportController,
        IProfileDestructor profileDestructor,
        IProfileCreator profileCreator,
        IProfileCloner profileCloner,
        FilePicker filePicker,
        DirectoryLauncher directoryLauncher)
    {
        _pageFactory = pageFactory;
        _dialogService = dialogService;
        _gameEnvironmentController = gameEnvironmentController;
        _settingsService = settingsService;
        _profileManager = profileManager;
        _applicationManager = applicationManager;
        _profileExportController = profileExportController;
        _profileDestructor = profileDestructor;
        _profileCreator = profileCreator;
        _profileCloner = profileCloner;
        _filePicker = filePicker;
        _directoryLauncher = directoryLauncher;
        _profileRepository = profileRepository;

        // Cache on start

        // Profile Update
        ProfileMetadataCollection = [.. _profileRepository.GetAll()];

        // **** Settings Setup ****
        gameEnvironmentController.OnEnvironmentUpdate += (newEnvironment, _) =>
        {
            // Update Executable Path flag.
            var previouslyPathSet = ExecutablePathSet;
            ExecutablePathSet = newEnvironment != null && !string.IsNullOrEmpty(newEnvironment.ExecutablePath);

            // If a new environment was set, update the repository.
            if (ExecutablePathSet)
            {
                Dispatcher.UIThread.Invoke(async () => 
                    await UpdateProfileRepository(
                        await UpdateEnvironmentSnapshot(!previouslyPathSet && ExecutablePathSet)));
            }
        };

        // If the executable is all set, then the manager should visualize the mods
        gameEnvironmentController.SetNewEnvironment(_settingsService.CurrentSettings.BaldiPlusExecutablePath);
        if (gameEnvironmentController.CurrentEnvironment != null)
        {
            // Set the executable path to true
            ExecutablePathSet = true;

            // Change pages
            CurrentPage = _pageFactory.GetPageViewModel<MyModsViewModel>();
        }
        else // Otherwise, force the user to set that manually
        {
            var settings = _pageFactory.GetPageViewModel<SettingsViewModel>();
            CurrentPage = settings;

            // Display that one needed dialog
            settings.DisplayGameFolderRequirementFolder();
        }

        // Update the Profile Selection for the settings.
        _profileManager.OnActiveProfileUpdate +=
            newProfile =>
            {
                // Update profile count
                SelectedProfile = newProfile;
                ProfileMetadataCollection = [.. _profileRepository.GetAll()];

                // Update settings
                _settingsService.Update(settings =>
                    settings.CurrentProfileSet = newProfile?.Name ?? ProfileMetadata.DefaultName);
                
                // Update Snapshot
                Dispatcher.UIThread.Invoke(async () => await UpdateEnvironmentSnapshot(false));
            };
    }

    // ---- Public ----
    public async Task<bool> HandleSettingsSave(bool promptCancelOption)
    {
        // Update snapshot
        await UpdateEnvironmentSnapshot(false);
        
        // Loading dialog for saving active profile
        if (!(_profileRepository.IsEmpty || // Or, if there are no profiles to save, skip this dialog
              await _dialogService.GenerateBooleanLoadingProcess("Saving current active profile...", null,
                  _profileManager.SaveActiveProfile)))
        {
            // If the option is not available, just return true and close anyway.
            if (!promptCancelOption) return true;

            return await _dialogService.PromptUserQuestion(
                "Failed to save settings!",
                "Are you sure you still want to leave the application without saving changes?");
        }

        // Then, one for saving settings
        return promptCancelOption | await _dialogService.GenerateBooleanLoadingProcess("Saving settings...", null, (Delegate)_settingsService.SaveAsync
        );
    }

    // ----- Private -----

    #region Main Window

    private void GoTo<TVm>()
        where TVm : PageViewModel
    {
        if (CurrentPage is not TVm)
            CurrentPage = _pageFactory!.GetPageViewModel<TVm>();
    }

    /// <summary>
    /// Show the App Info dialog.
    /// </summary>
    private async Task RevealAboutSectionUi()
    {
        await _dialogService.ShowDialog<AppInfoDialogViewModel>(null);
    }
    
    /// <summary>
    /// Show the License dialog.
    /// </summary>
    private async Task RevealLicenseUi()
    {
        await _dialogService.ShowDialog<LicenseDialogViewModel>(null);
    }

    /// <summary>
    /// Checks whether there's a default profile available or not. If not, one is created automatically.
    /// </summary>
    private async Task UpdateProfileRepository(bool updateProfileDataBeforeSwitch)
    {
        // Messages Setup
        const string firstAttemptFail =
            "The profile system failed to retrieve the new list of profiles locally! Do you want to retry (Yes/No)\nPressing \"No\" quits the application.";
        var secondAttemptFail = firstAttemptFail +
                                $"\nWhen closing the application, try following these recommendations:\n{Constants.CommonIssuesSolution}";

        // Boolean setup
        var firstAttemptDone = false;

        // Infinite loop for updating profiles
        while (true)
        {
            // If the update fails, prompt the user a question.
            if (!await _dialogService.GenerateBooleanLoadingProcess("Profile Repository Update", "Updating Profile Repository...",
                    _profileManager.UpdateProfileRepository,
                    _settingsService.CurrentSettings.CurrentProfileSet,
                    updateProfileDataBeforeSwitch   
                ))
            {
                // If the user chooses "No", close the application.
                if (!await _dialogService.PromptUserQuestion(Constants.FailDialog,
                        firstAttemptDone ? secondAttemptFail : firstAttemptFail))
                {
                    // Exit application forcefully
                    _applicationManager.Exit();
                    return;
                }

                firstAttemptDone = true;
                continue;
            }

            break;
        }
    }

    // False means no changes needed; True means user wants to overwrite profile.
    private async Task<bool> UpdateEnvironmentSnapshot(bool raiseQuestionIfDifferencesDetected)
    {
        // TODO: Localization here
        const string question = """
                                Conflict detected between GMP's working directory's settings and the current profile's settings. The content inside the loaded profile differs from what the current working directory has stored.

                                How would you like to proceed?
                                ADAPT: Merge the profile's data with the active mods (keep the current mods, adds any missing ones from the profile).
                                IGNORE: Ignore the working directory's settings and potentially overwrite with the profile's content.
                                """;
        
        // If the snapshot returns true, there's a difference to be solved.
        var result = await _dialogService.ShowDialog<LoadingDialogViewModel>(
            new LoadingDialogContext(
                _gameEnvironmentController.UpdateEnvironmentSnapshot,
                Title: "Updating Environment",
                Status: "Checking for snapshot differences..."));

        if (result?.Result is not SnapshotChangeReport { HasChanges: true } report ||
            !raiseQuestionIfDifferencesDetected) return true;
        // Show the snapshot changes in a dialog with TreeDataGrid
        var logContainer = report.ToLogContainer();
        var context = new ConfirmDialogContext(
            DialogServiceUtils.QuestionAnswerType.AdaptOrIgnore,
            Constants.WarningDialog, question, logContainer: logContainer
            );
        var confirmResult = await _dialogService.ShowDialog<ConfirmDialogViewModel>(context);
            
        return confirmResult?.Confirmed ?? false; // True = Adapt, False = Ignore

    }

    #endregion

    #region Profile Manipulation

    private async Task ExportProfileMetadata(ProfileMetadata profile)
    {
        if (!await _dialogService.PromptUserQuestion(
                $"Export '{profile.Name}'?",
                $"Are you sure you want to export '{profile.Name}'?")) return;

        if (!await _dialogService.GenerateBooleanLoadingProcess("Profile Export", "Exporting profile...", _profileExportController.ExportProfile, profile
            ))
            return;

        await _directoryLauncher.OpenDirectoryInfo(
            new DirectoryInfo(_gameEnvironmentController.GetOrCreateProfilesExportFolderPath()));
    }

    private async Task DeleteProfileMetadataUiAsync(ProfileMetadata profile)
    {
        // Ensure the user wants to delete the profile.
        if (!await _dialogService.PromptUserQuestion(
                $"Delete '{profile.Name}'?",
                $"Are you sure you want to delete '{profile.Name}'?"))
            return;

        // Attempts to delete the profile.
        await _dialogService.GenerateBooleanLoadingProcess("Deleting profile...", null, _profileDestructor.DeleteProfile, profile
        );
    }

    private async Task CreateProfileUiAsync()
    {
        // Create the dialog to make profiles.
        var createResult = await _dialogService.ShowDialog<CreateProfileDialogViewModel>(
            new CreateProfileDialogContext(
                _filePicker,
                ProfileMetadataCollection!.Select(p => p.Name)));
        if (createResult?.Result is not CreateProfileDialogViewModel creatingPfDialog)
            return;

        // If not confirmed, cancel.
        if (!creatingPfDialog.Confirmed) return;

        // Get the index.
        switch (creatingPfDialog.SelectedTabIndex)
        {
            case 0: // Create New
            {
                await _dialogService.GenerateBooleanLoadingProcess("Creating new profile...", null, _profileCreator.CreateProfile,
                    new ProfileMetadata { Name = creatingPfDialog.ProfileName ?? "New Profile" });
                break;
            }
            case 1: // Clone
            {
                // Get the profile name.
                var targetProfile = creatingPfDialog.ExistingProfiles[creatingPfDialog.ProfileIndexToClone ?? 0];
                // Resolve source profile instance by name if the dialog only exposes strings.
                var sourceProfile = ProfileMetadataCollection!.FirstOrDefault(p =>
                    string.Equals(p.Name, targetProfile, StringComparison.OrdinalIgnoreCase));
                
                if (sourceProfile == null) return;

                // Clone profile.
                await _dialogService.GenerateBooleanLoadingProcess("Cloning profile...", "Selecting profile and cloning it...",
                    (Delegate)_profileCloner.CloneProfile, sourceProfile, creatingPfDialog.CloneProfileName);
                break;
            }
            case 2: // Import
            {
                await _dialogService.GenerateBooleanLoadingProcess("Importing profile...", "Selecting profile and importing it...",
                    (Delegate)_profileExportController.ExtractExportedProfile, creatingPfDialog.ProfileImportPath);
                break;
            }
        }
        
        // Update profile list.
        await UpdateProfileRepository(false);
    }

    private async Task SwitchProfileUiAsync(ProfileMetadata profile)
    {
       
        if (_profileManager.ActiveProfile == profile) return;

        if (!await _dialogService.PromptUserQuestion(
                $"Switch to \'{profile.Name}\'?",
                "Are you sure you want to switch to this profile?"))
            return;

        // Loading process
        await _dialogService.GenerateBooleanLoadingProcess(null, null, _profileManager.SetActiveProfile, profile
        );
    }

    #endregion
}
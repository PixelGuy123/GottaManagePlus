using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GottaManagePlus.Models;
using GottaManagePlus.Services;
using GottaManagePlus.Services.ExplorerServices;
using GottaManagePlus.Services.GameEnvironmentServices;
using GottaManagePlus.Services.ModServices;
using GottaManagePlus.Services.ProfileServices;
using GottaManagePlus.Utils;
using Serilog;

namespace GottaManagePlus.ViewModels;

public partial class MyModsViewModel : PageViewModel, IDisposable
{
    // ---- Dependencies ----
    private readonly DialogService _dialogService = null!;
    private readonly ProfileManager _profileManager = null!;
    private readonly DirectoryLauncher _directoryLauncher = null!;
    private readonly ModUnInstaller _modUninstaller = null!;
    private readonly ModInstaller _modInstaller = null!;
    private readonly GameEnvironmentController _gameEnvironmentController = null!;

    // ---- Observable Properties ----
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(HasAnyModsToDisplay))]
    private ObservableCollection<ModManifest> _observableUnchangedMods = [];

    [ObservableProperty] private ObservableCollection<ModManifest> _observableMods = [];
    [ObservableProperty] private ModManifest? _currentModManifest;
    [ObservableProperty] private string? _text;

    // ---- Public Getters ----
    public int NumberOfModsPerRow { get; private set; } = 6;
    public bool HasAnyModsToDisplay => ObservableUnchangedMods.Count != 0;
    public string CurrentPlusVersion => _gameEnvironmentController.CurrentEnvironment!.GameVersion.ToString();
    
    // ---- Event Handlers & Commands ----
    partial void OnCurrentModManifestChanged(ModManifest? value) => UpdateModsList(value);

    private void OnObservableUnchangedModsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        OnPropertyChanged(nameof(HasAnyModsToDisplay));

    [RelayCommand] public void ResetSearch() => Text = null;
    [RelayCommand] public async Task DeleteModManifest(ModManifest mod) => await DeleteModManifestUiAsync(mod);
    [RelayCommand] public async Task AddModRequest() => await AddModUiAsync();
    [RelayCommand] public async Task OpenModPath(ModManifest mod) => await OpenModPathUiAsync(mod);

    // ---- Design-Time Constructor ----
    public MyModsViewModel() : base(PageNames.Home)
    {
        if (!Design.IsDesignMode) return;
        ObservableUnchangedMods = new ObservableCollection<ModManifest>(
        [
            new ModManifest { Name = "Design Mod 1" },
            new ModManifest { Name = "Design Mod 2" }
        ]);
        ObservableMods = new ObservableCollection<ModManifest>(ObservableUnchangedMods);
    }

    // ---- DI Constructor ----
    public MyModsViewModel(
        DialogService dialogService, 
        ProfileManager profileManager, 
        GameEnvironmentController controller, 
        DirectoryLauncher directoryLauncher, 
        SettingsService settingsService,
        ModUnInstaller modUninstaller,
        ModInstaller modInstaller) : base(PageNames.Home)
    {
        _dialogService = dialogService;
        _profileManager = profileManager;
        _gameEnvironmentController = controller;
        _directoryLauncher = directoryLauncher;
        _modUninstaller = modUninstaller;
        _modInstaller = modInstaller;
        NumberOfModsPerRow = settingsService.CurrentSettings.NumberOfRowsPerMod;

        FillUpAllMods(_profileManager.ActiveProfile?.ModDataFiles);
        _profileManager.OnActiveProfileUpdate += ProfilesProvider_OnProfilesUpdate;
        ObservableUnchangedMods.CollectionChanged += OnObservableUnchangedModsCollectionChanged;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _profileManager.OnActiveProfileUpdate -= ProfilesProvider_OnProfilesUpdate;
    }

    // ---- Data Population ----
    private void FillUpAllMods(IEnumerable<ModManifest>? manifests)
    {
        var list = manifests?.ToList() ?? [];
        ObservableUnchangedMods = new ObservableCollection<ModManifest>(list);
    }

    private void ProfilesProvider_OnProfilesUpdate(ProfileMetadata? profileMetadata)
    {
        Dispatcher.UIThread.Post(() =>
        {
            FillUpAllMods(profileMetadata?.ModDataFiles);
            ResetListVisibleConfigurations();
        });
    }

    private void UpdateModsList(ModManifest? highlightedItem)
    {
        if (highlightedItem != null)
        {
            ObservableMods = new ObservableCollection<ModManifest>(
                ObservableUnchangedMods.OrderByDescending(mod => highlightedItem.Name.ManyStartWith(mod.Name)));
            return;
        }
        ObservableMods = new ObservableCollection<ModManifest>(ObservableUnchangedMods.OrderBy(mod => mod.Name));
    }

    private void ResetListVisibleConfigurations()
    {
        UpdateModsList(null);
        ResetSearch();
    }

    private async Task AddModUiAsync()
    {
        // TODO: Implement mod addition logic via a dedicated ModService when available
    }

    private async Task DeleteModManifestUiAsync(ModManifest modToDelete)
    {
        var confirmDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
        confirmDialog.Prepare(null, $"Delete {modToDelete.Name}?", "Are you sure you want to delete this mod?", "Yes", "No");
        await _dialogService.ShowDialog(confirmDialog);
        if (!confirmDialog.Confirmed) return;

        var profileMetadata = _profileManager.ActiveProfile;
        if (profileMetadata == null) return;

        var modDirectoryPath = modToDelete.GetPluginDirectoryFromManifest(_gameEnvironmentController);
        if (!string.IsNullOrEmpty(modDirectoryPath) && Directory.Exists(modDirectoryPath))
        {
            try
            {
                // Uninstalls and update it afterward.
                _modUninstaller.DeleteMod(modToDelete, ProfilesProvider_OnProfilesUpdate);
            }
            catch (Exception e)
            {
                Log.Error("Failed to delete the mod directory for {ModName}!\n{Exception}", modToDelete.Name, e);
                var failDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
                failDialog.Prepare(true, Constants.FailDialog, $"Failed to delete mod files for {modToDelete.Name}. Check file permissions.");
                await _dialogService.ShowDialog(failDialog);
                return;
            }
        }

        var loadingDialog = _dialogService.GetDialog<LoadingDialogViewModel>();
        loadingDialog.Prepare("Saving changes...", null, (Delegate)_profileManager.SaveActiveProfile);

        if (!await _dialogService.ShowDialog(loadingDialog))
        {
            var failDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
            failDialog.Prepare(true, Constants.FailDialog, $"Failed to save the changes. If this issue persists, try:\n{Constants.SolutionFilePermissions}");
            await _dialogService.ShowDialog(failDialog);
        }
    }

    private async Task OpenModPathUiAsync(ModManifest mod)
    {
        var modDirectoryPath = mod.GetPluginDirectoryFromManifest(_gameEnvironmentController);
        if (string.IsNullOrEmpty(modDirectoryPath) || !Directory.Exists(modDirectoryPath))
        {
            var confirmDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
            confirmDialog.Prepare(true, Constants.FailDialog, "The path to the mod is somehow invalid!\nTry reloading the profiles list.");
            await _dialogService.ShowDialog(confirmDialog);
            return;
        }

        if (!await _directoryLauncher.OpenDirectoryInfo(new DirectoryInfo(modDirectoryPath)))
        {
            var confirmDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
            confirmDialog.Prepare(true, Constants.FailDialog, "Failed to open the path to the mod due to an unknown error!\nTry reloading the profiles list.");
            await _dialogService.ShowDialog(confirmDialog);
        }
    }
}
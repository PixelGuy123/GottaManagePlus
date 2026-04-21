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
    // TODO: Make a valid error popup message for every prompt available. 
    
    // ---- Dependencies ----
    private readonly DialogService _dialogService = null!;
    private readonly ProfileManager _profileManager = null!;
    private readonly DirectoryLauncher _directoryLauncher = null!;
    private readonly ModUnInstaller _modUninstaller = null!;
    private readonly ModInstaller _modInstaller = null!;
    private readonly GameEnvironmentController _gameEnvironmentController = null!;

    // ---- Observable Properties ----
    [ObservableProperty]
    private ObservableCollection<ModManifest> _observableUnchangedMods = [];

    [ObservableProperty] private ObservableCollection<ModManifest> _observableMods = [];
    [ObservableProperty] private ModManifest? _currentModManifest;
    [ObservableProperty] private string? _searchText;
    
    // ---- Mod Preview ----
    [ObservableProperty] private ModManifest? _manifestInPreview;

    // ---- Public Getters ----
    public int ModsEnabledCount => ObservableMods.Count(mod => mod.Metadata.Activated);
    public int? NumberOfModsPerRow { get; private set; } = 6;
    public string CurrentPlusVersion => Design.IsDesignMode ? 
        "0.14.1" : _gameEnvironmentController.CurrentEnvironment!.GameVersion.ToString();
    
    // ---- Event Handlers & Commands ----
    partial void OnCurrentModManifestChanged(ModManifest? value) => UpdateModsList(value);

    [RelayCommand] public void ResetSearch() => SearchText = null;
    [RelayCommand] public async Task DeleteModManifest(ModManifest mod) => await DeleteModManifestUiAsync(mod);
    [RelayCommand] public async Task AddModRequest() => await AddModUiAsync();
    [RelayCommand] public async Task OpenModPath(ModManifest mod) => await OpenPathUiAsync(mod.GetPluginDirectoryFromManifest(_gameEnvironmentController));
    [RelayCommand] public async Task OpenModAssetsPath(ModManifest mod) => await OpenModAssetsPathUiAsync(mod);
    [RelayCommand] public async Task OpenGamePath(ModManifest mod) => await OpenPathUiAsync(_gameEnvironmentController.CurrentEnvironment!.RootPath);
    [RelayCommand] public void ExitModVisualization() => ManifestInPreview = null;

    // ---- Design-Time Constructor ----
    public MyModsViewModel() : base(PageNames.Home)
    {
        if (!Design.IsDesignMode) return;
        ObservableUnchangedMods = new ObservableCollection<ModManifest>(
        [
            new ModManifest { Name = "Design Mod 1" },
            new ModManifest { Name = "Design Mod 1" },
            new ModManifest { Name = "Design Mod 1" },
            new ModManifest { Name = "Design Mod 1" },
            new ModManifest { Name = "Design Mod 1" },
            new ModManifest { Name = "Design Mod 1" },
            new ModManifest { Name = "Design Mod 1" },
            new ModManifest { Name = "Design Mod 1" },
            new ModManifest { Name = "Design Mod 1" },
            new ModManifest { Name = "Design Mod 1" },
            new ModManifest { Name = "Design Mod 1" },
            new ModManifest { Name = "Design Mod 1" },
            new ModManifest { Name = "Design Mod 1" },
            new ModManifest { Name = "Design Mod 1" },
            new ModManifest { Name = "Design Mod 1" },
            new ModManifest { Name = "Design Mod 1" },
            new ModManifest { Name = "Design Mod 1" },
            new ModManifest { Name = "Design Mod 1" },
            new ModManifest { Name = "Design Mod 1" },
            new ModManifest { Name = "Design Mod 1" },
            new ModManifest { Name = "Design Mod 1" },
            new ModManifest { Name = "Design Mod 1" },
            new ModManifest { Name = "Design Mod 2", Description = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed porttitor " +
                                                                   "magna eu est semper consectetur. Vestibulum elementum mollis sem vel " +
                                                                   "tempor. Suspendisse potenti. Nam quis varius augue. Vestibulum ante ipsum " +
                                                                   "primis in faucibus orci luctus et ultrices posuere cubilia curae; Donec " +
                                                                   "faucibus quam non arcu blandit, ac posuere quam dapibus. Donec efficitur " +
                                                                   "bibendum felis. Donec cursus gravida tortor, in venenatis quam malesuada aliquam." +
                                                                   " Suspendisse eu erat sem. Quisque tortor augue, feugiat eu elementum sed, accumsan id " +
                                                                   "quam.\n\nInterdum et malesuada fames ac ante ipsum primis in faucibus. Nulla bibendum risus rhoncus pellentesque pellentesque. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia curae; Praesent interdum scelerisque massa, sit amet imperdiet nisl maximus in. Aliquam at eros ut quam vulputate auctor. Aliquam at risus eget dui finibus ullamcorper. Aliquam gravida erat sit amet ante efficitur pretium. Nulla ac neque a lorem aliquet rutrum ac eu ligula. Integer viverra diam vitae vestibulum condimentum. Praesent vel turpis enim. Nam ut nulla velit. Morbi turpis ipsum, vulputate at sapien eget, pharetra bibendum diam. Vestibulum ornare urna sed lectus mattis tristique. Mauris lobortis tellus sed purus gravida scelerisque et eget erat. Ut volutpat a odio vel posuere.\n\nNullam elit metus, congue ac lacus vel, venenatis egestas nisl. Nunc non ante quam. Proin ut tellus elit. Ut sit amet ex finibus ante malesuada laoreet nec vitae sapien. Etiam bibendum, massa id rutrum gravida, magna enim mollis dolor, vulputate gravida augue velit eu elit. Donec eu odio tristique, porttitor nisl dictum, efficitur odio. Suspendisse ac egestas mauris. Sed quis tortor sed arcu dictum sagittis. Quisque porttitor elementum metus ut efficitur. Duis at urna dui. Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas. Proin molestie leo id efficitur facilisis. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nunc et aliquet purus, eu consectetur neque.\n\nSed vehicula augue vel mauris facilisis, sit amet dignissim lectus condimentum. Interdum et malesuada fames ac ante ipsum primis in faucibus. Vivamus elementum iaculis arcu. Curabitur viverra viverra eros sit amet tristique. Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos. Duis dictum eros quis velit fringilla pretium. Praesent vitae ante vel sapien laoreet venenatis eu quis orci. Proin venenatis augue ut justo feugiat cursus. Cras lobortis lobortis est, a sodales felis semper mattis. Curabitur sit amet dui non mauris luctus aliquam. Pellentesque pellentesque suscipit orci eget ultricies. Integer eleifend vel mi ac suscipit. Duis gravida quam lacus, vitae sagittis ipsum bibendum nec. Vivamus sollicitudin purus nec tortor aliquam accumsan. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia curae;\n\nMauris rutrum, nunc ac ullamcorper hendrerit, eros nunc laoreet ligula, in congue urna eros rhoncus mi. In efficitur at turpis dictum ultrices. Cras tellus nunc, egestas quis tellus a, eleifend dictum libero. Praesent dapibus sodales fringilla. Vivamus euismod, tellus ac viverra egestas, metus mi sollicitudin diam, et aliquet lectus arcu id massa. Suspendisse sed odio nec dui ultrices tincidunt in in tortor. Vivamus felis nisi, fringilla in posuere id, finibus accumsan metus. Aenean pretium urna sit amet molestie gravida. Vivamus vestibulum, felis vitae tincidunt hendrerit, lacus metus iaculis leo, non tristique purus elit in quam. Duis pharetra dolor quis nulla rutrum, a hendrerit massa efficitur. Nulla quis odio nec diam dapibus eleifend at non tellus. Integer sit amet sem odio. Sed eu magna tortor. Nulla sollicitudin nisl eu arcu varius, id porta ante volutpat. "}
        ]);
        ObservableMods = new ObservableCollection<ModManifest>(ObservableUnchangedMods);
        ManifestInPreview = ObservableUnchangedMods[1];
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
        if (!await _dialogService.PromptUserQuestion(
                $"Delete {modToDelete.Name}?",
                "Are you sure you want to delete this mod?"))
            return;

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
                Log.Logger.Error("Failed to delete the mod directory for {ModName}!\n{Exception}", modToDelete.Name, e);
                await _dialogService.NotifyUser(Constants.FailDialog, $"Failed to delete mod files for {modToDelete.Name}. Check file permissions.");
                return;
            }
        }

        await _dialogService.GenerateLoadingProcess(
            $"Failed to save the changes. If this issue persists, try:\n{Constants.CommonIssuesSolution}",
            null,
            "Saving changes...", null, (Delegate)_profileManager.SaveActiveProfile);
    }

    private async Task OpenModAssetsPathUiAsync(ModManifest mod)
    {
        HashSet<string> directoriesToOpen = [];
        
        // Gather all the asset directories available.
        foreach (var path in from asset in mod.Assets 
                 where !string.IsNullOrEmpty(asset.Destination) 
                 select _gameEnvironmentController.SearchAbsolutePath(asset.Destination!))
        {
            // If the path exists and isn't added to the list, add it.
            if (Directory.Exists(path))
                directoriesToOpen.Add(path);
        }
        
        // If there's more than one asset folder to be opened,
        // ask user for permission to open multiple directories.
        if (directoriesToOpen.Count > 1 && !await _dialogService.PromptUserQuestion("Open Multiple Directories",
                $"This mod has {directoriesToOpen.Count} distinct asset folders. Do you wish to open all of them?",
                DialogServiceUtils.QuestionAnswerType.AllowOrDisallow))
            return;
        
        // Attempts to open every directory available.
        foreach (var directory in directoriesToOpen)
            await OpenPathUiAsync(directory);
    }
    
    private async Task OpenPathUiAsync(string path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
        {
            await _dialogService.NotifyUser(Constants.FailDialog, "The path to the mod is somehow invalid!\nTry reloading the profiles list.");
            return;
        }

        if (!await _directoryLauncher.OpenDirectoryInfo(new DirectoryInfo(path)))
        {
            await _dialogService.NotifyUser(Constants.FailDialog, "Failed to open the path to the mod due to an unknown error!\nTry reloading the profiles list.");
        }
    }
}
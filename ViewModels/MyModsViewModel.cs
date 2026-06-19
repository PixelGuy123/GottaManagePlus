using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GottaManagePlus.Models;
using GottaManagePlus.Models.ModManagement;
using GottaManagePlus.Models.UI;
using GottaManagePlus.Services;
using GottaManagePlus.Services.APIServices;
using GottaManagePlus.Services.ExplorerServices;
using GottaManagePlus.Services.GameEnvironmentServices;
using GottaManagePlus.Services.ModServices;
using GottaManagePlus.Services.ProfileServices;
using GottaManagePlus.Utils;
using Serilog;

namespace GottaManagePlus.ViewModels;

public partial class MyModsViewModel : PageViewModel, IDisposable
{
    // ---- File Picker Types ----
    private static readonly FilePickerFileType[] PluginOnlyPick =
    [
        new("Plugin Format")
        {
            Patterns = ["*.dll"],
            AppleUniformTypeIdentifiers = ["public.assembly-source"],
        }
    ];

    private static readonly FilePickerFileType[] ArchiveTypesPick =
    [
        new("Archive Files")
        {
            Patterns =
            [
                "*.zip", "*.gzip", "*.gz", "*.7z", "*.7zip", "*.rar", "*.tar", "*.tar.gz", "*.tar.bz2", "*.bz2", "*.xz",
                "*.wim"
            ],
            AppleUniformTypeIdentifiers = ["public.zip-archive", "org.gnu.gnu-zip-archive", "public.bzip2-archive"],
        }
    ];

    // ---- Dependencies ----
    private readonly DialogService _dialogService = null!;
    private readonly ProfileManager _profileManager = null!;
    private readonly DirectoryLauncher _directoryLauncher = null!;
    private readonly DirectoryPicker _directoryPicker = null!;
    private readonly FilePicker _filePicker = null!;
    private readonly ModUnInstaller _modUninstaller = null!;
    private readonly ModArchiveGenerator _archiveGenerator = null!;
    private readonly ModActivator _modActivator = null!;
    private readonly ModInstaller _modInstaller = null!;
    private readonly GameEnvironmentController _gameEnvironmentController = null!;
    private readonly GamebananaApiService _gamebananaApiService = null!;

    // ---- Observable Properties ----
    [ObservableProperty]
    public partial ObservableCollection<ObservableModManifest> ObservableUnchangedMods { get; set; } = [];

    [ObservableProperty] public partial ObservableCollection<ObservableModManifest> ObservableMods { get; set; } = [];

    [ObservableProperty] public partial ObservableModManifest? CurrentModManifest { get; set; }

    [ObservableProperty] public partial string? SearchText { get; set; }

    // ---- Mod Preview ----
    [ObservableProperty] public partial ObservableModManifest? ManifestInPreview { get; set; }
    [ObservableProperty] public partial bool IsManifestPreviewOpen { get; set; }

    // ---- Public Getters ----
    public int ModsEnabledCount => ObservableMods.Count(mod => mod.IsActivated);
    public int? NumberOfModsPerRow { get; private set; } = 6;

    public string CurrentPlusVersion => Design.IsDesignMode
        ? "0.14.1"
        : _gameEnvironmentController.CurrentEnvironment!.GameVersion.ToString();

    // ---- Event Handlers & Commands ----
    partial void OnCurrentModManifestChanged(ObservableModManifest? value) => UpdateModsList(value?.InnerManifest);

    partial void OnIsManifestPreviewOpenChanged(bool value) // Mutual exchange with ManifestInPreview
    {
        if (!value) ManifestInPreview = null;
    }

    partial void OnManifestInPreviewChanged(ObservableModManifest? value) => // Mutual exchange with IsManifestPreviewOpen
        IsManifestPreviewOpen = value != null;
    

    [RelayCommand]
    public void ResetSearch() => SearchText = null;

    [RelayCommand]
    public async Task DeleteModManifest(ObservableModManifest mod) => await DeleteModManifestUiAsync(mod.InnerManifest);

    [RelayCommand]
    public async Task AddModLocally(bool lookForDllFile) => await AddModUiAsync(lookForDllFile);
    [RelayCommand]
    public async Task OpenGamebananaModSelector()
    {
        var selectModDialog = _dialogService.GetDialog<ModSelectionDialogViewModel>();
        selectModDialog.Prepare(_dialogService, _gamebananaApiService, _gameEnvironmentController, _modInstaller);
        await _dialogService.ShowDialog(selectModDialog);
    }

    [RelayCommand]
    public async Task OpenModPath() =>
        await OpenPathUiAsync(ManifestInPreview!.InnerManifest.GetPluginDirectoryFromManifest(_gameEnvironmentController));

    [RelayCommand]
    public void SelectModToPreview(ObservableModManifest mod) =>
        ManifestInPreview = mod;

    [RelayCommand]
    public void EnableModOrNot(ObservableModManifest mod)
    {
        _modActivator.ToggleActivation(mod.InnerManifest, !mod.IsActivated); // Update in disk
        mod.IsActivated = mod.InnerManifest.Metadata.Activated; // Update in memory
        OnPropertyChanged(nameof(ModsEnabledCount)); // Update mod count
    }


    [RelayCommand]
    public async Task OpenModAssetsPath() => await OpenModAssetsPathUiAsync(ManifestInPreview!.InnerManifest);

    [RelayCommand]
    public async Task OpenGamePath() =>
        await OpenPathUiAsync(_gameEnvironmentController.CurrentEnvironment!.RootPath);

    [RelayCommand]
    public void ExitModVisualization() => ManifestInPreview = null;

    // ---- Design-Time Constructor ----
    public MyModsViewModel() : base(PageNames.Home)
    {
        if (!Design.IsDesignMode) return;
        ObservableUnchangedMods = new ObservableCollection<ObservableModManifest>(
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
            new ModManifest
            {
                Name = "Design Mod 2", Description =
                    // ReSharper disable StringLiteralTypo
                    "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed porttitor " +
                    "magna eu est semper consectetur. Vestibulum elementum mollis sem vel " +
                    "tempor. Suspendisse potenti. Nam quis varius augue. Vestibulum ante ipsum " +
                    "primis in faucibus orci luctus et ultrices posuere cubilia curae; Donec " +
                    "faucibus quam non arcu blandit, ac posuere quam dapibus. Donec efficitur " +
                    "bibendum felis. Donec cursus gravida tortor, in venenatis quam malesuada aliquam." +
                    " Suspendisse eu erat sem. Quisque tortor augue, feugiat eu elementum sed, accumsan id " +
                    "quam.\n\nInterdum et malesuada fames ac ante ipsum primis in faucibus. Nulla bibendum risus rhoncus pellentesque pellentesque. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia curae; Praesent interdum scelerisque massa, sit amet imperdiet nisl maximus in. Aliquam at eros ut quam vulputate auctor. Aliquam at risus eget dui finibus ullamcorper. Aliquam gravida erat sit amet ante efficitur pretium. Nulla ac neque a lorem aliquet rutrum ac eu ligula. Integer viverra diam vitae vestibulum condimentum. Praesent vel turpis enim. Nam ut nulla velit. Morbi turpis ipsum, vulputate at sapien eget, pharetra bibendum diam. Vestibulum ornare urna sed lectus mattis tristique. Mauris lobortis tellus sed purus gravida scelerisque et eget erat. Ut volutpat a odio vel posuere.\n\nNullam elit metus, congue ac lacus vel, venenatis egestas nisl. Nunc non ante quam. Proin ut tellus elit. Ut sit amet ex finibus ante malesuada laoreet nec vitae sapien. Etiam bibendum, massa id rutrum gravida, magna enim mollis dolor, vulputate gravida augue velit eu elit. Donec eu odio tristique, porttitor nisl dictum, efficitur odio. Suspendisse ac egestas mauris. Sed quis tortor sed arcu dictum sagittis. Quisque porttitor elementum metus ut efficitur. Duis at urna dui. Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas. Proin molestie leo id efficitur facilisis. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nunc et aliquet purus, eu consectetur neque.\n\nSed vehicula augue vel mauris facilisis, sit amet dignissim lectus condimentum. Interdum et malesuada fames ac ante ipsum primis in faucibus. Vivamus elementum iaculis arcu. Curabitur viverra viverra eros sit amet tristique. Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos. Duis dictum eros quis velit fringilla pretium. Praesent vitae ante vel sapien laoreet venenatis eu quis orci. Proin venenatis augue ut justo feugiat cursus. Cras lobortis lobortis est, a sodales felis semper mattis. Curabitur sit amet dui non mauris luctus aliquam. Pellentesque pellentesque suscipit orci eget ultricies. Integer eleifend vel mi ac suscipit. Duis gravida quam lacus, vitae sagittis ipsum bibendum nec. Vivamus sollicitudin purus nec tortor aliquam accumsan. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia curae;\n\nMauris rutrum, nunc ac ullamcorper hendrerit, eros nunc laoreet ligula, in congue urna eros rhoncus mi. In efficitur at turpis dictum ultrices. Cras tellus nunc, egestas quis tellus a, eleifend dictum libero. Praesent dapibus sodales fringilla. Vivamus euismod, tellus ac viverra egestas, metus mi sollicitudin diam, et aliquet lectus arcu id massa. Suspendisse sed odio nec dui ultrices tincidunt in in tortor. Vivamus felis nisi, fringilla in posuere id, finibus accumsan metus. Aenean pretium urna sit amet molestie gravida. Vivamus vestibulum, felis vitae tincidunt hendrerit, lacus metus iaculis leo, non tristique purus elit in quam. Duis pharetra dolor quis nulla rutrum, a hendrerit massa efficitur. Nulla quis odio nec diam dapibus eleifend at non tellus. Integer sit amet sem odio. Sed eu magna tortor. Nulla sollicitudin nisl eu arcu varius, id porta ante volutpat. "
                    // ReSharper enable StringLiteralTypo
            }
        ]);
        ObservableMods = new ObservableCollection<ObservableModManifest>(ObservableUnchangedMods);
        ManifestInPreview = ObservableUnchangedMods[1];
    }

    // ---- DI Constructor ----
    public MyModsViewModel(
        DialogService dialogService,
        ProfileManager profileManager,
        GameEnvironmentController controller,
        DirectoryLauncher directoryLauncher,
        DirectoryPicker directoryPicker,
        FilePicker filePicker,
        SettingsService settingsService,
        ModUnInstaller modUninstaller,
        ModArchiveGenerator archiveGenerator,
        ModActivator modActivator,
        GamebananaApiService gamebananaApiService,
        ModInstaller modInstaller) : base(PageNames.Home)
    {
        _dialogService = dialogService;
        _profileManager = profileManager;
        _gameEnvironmentController = controller;
        _directoryLauncher = directoryLauncher;
        _directoryPicker = directoryPicker;
        _filePicker = filePicker;
        _modUninstaller = modUninstaller;
        _archiveGenerator = archiveGenerator;
        _modActivator = modActivator;
        _modInstaller = modInstaller;
        _gamebananaApiService = gamebananaApiService;
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
        var list = manifests?.Select(m => new ObservableModManifest(m)).ToList() ?? [];
        ObservableUnchangedMods = new ObservableCollection<ObservableModManifest>(list);
        ResetListVisibleConfigurations();
        OnPropertyChanged(nameof(ModsEnabledCount));
    }

    private void ProfilesProvider_OnProfilesUpdate(ProfileMetadata? profileMetadata) =>
        Dispatcher.UIThread.Post(() => FillUpAllMods(profileMetadata?.ModDataFiles));

    private void UpdateModsList(ModManifest? highlightedItem)
    {
        if (highlightedItem != null)
        {
            ObservableMods = new ObservableCollection<ObservableModManifest>(
                ObservableUnchangedMods.OrderByDescending(mod =>
                    highlightedItem.Name.ManyStartWith(mod.InnerManifest.Name)));
            return;
        }

        ObservableMods =
            new ObservableCollection<ObservableModManifest>(
                ObservableUnchangedMods.OrderBy(mod => mod.InnerManifest.Name));
    }

    private void ResetListVisibleConfigurations()
    {
        UpdateModsList(null);
        ResetSearch();
    }

    private async Task AddModUiAsync(bool lookForDllFileOnly)
    {
        try
        {
            // If looking for DLL only, we're expecting a plugin and a single asset folder.
            string? archiveToInstall;
            
            // # DLL ONLY SEARCH #
            if (lookForDllFileOnly)
            {
                // Retrieve the dll file from the picker.
                var dllFile =
                    (await _filePicker.OpenSingleFileAsync("Open a Plugin file to be imported.",
                        filterChoices: PluginOnlyPick,
                        preselectedPath: _gameEnvironmentController.CurrentEnvironment!.RootPath))?.TryGetLocalPath();

                // No dll file, no continuation.
                if (dllFile == null)
                {
                    await _dialogService.NotifyUser(Constants.FailDialog,
                        "No .dll file has been provided. Please, select a plugin to be imported.");
                    return;
                }

                // Let user select multiple asset folders.
                var assetDirectories = await _directoryPicker.OpenMultipleFoldersAsync(
                    "(OPTIONAL) Open asset folders to import.",
                    new DirectoryInfo(_gameEnvironmentController.CurrentEnvironment!.RootPath));
                IStorageFolder? destinationForAssets = null;

                // If there are assets selected, set a destination for them inside the root environment.
                if (assetDirectories.Count != 0)
                {
                    destinationForAssets = await _directoryPicker.OpenFolderAsync(
                        "Set a destination folder for the assets.",
                        new DirectoryInfo(_gameEnvironmentController.CurrentEnvironment!.RootPath));
                    if (destinationForAssets == null)
                    {
                        await _dialogService.NotifyUser(Constants.FailDialog,
                            "No destination for assets has been provided. Please, select a destination directory for the assets.");
                        return;
                    }
                }

                // Generate destination assets from the asset directories.
                var destinedAssets = assetDirectories.Select(folder => new DestinedAsset
                {
                    LocalPath = folder.TryGetLocalPath()!, Destination = destinationForAssets?.TryGetLocalPath()
                }).ToArray();

                // Get a temporary location for the to-be-generated archive.
                using var tempDir = _gameEnvironmentController.CreateTempSubdirectory(Log.Logger);
                archiveToInstall = Path.Combine(tempDir.DirectoryInfo.FullName, Path.GetFileNameWithoutExtension(dllFile) + ".bin");

                // Now, actually wrap the files in a temporary zip file.
                if (!await _dialogService.GenerateBooleanLoadingProcess(
                        "Failed to generate an archive for the mod!",
                        null,
                        "Generating Archive for Plugin",
                        null,
                        (Delegate)_archiveGenerator.GenerateArchive,
                        new[] { dllFile },
                        destinedAssets,
                        archiveToInstall
                    ))
                {
                    // If it fails, just return back
                    return;
                }
            }
            // # ARCHIVE SEARCH #
            else
            {
                // Retrieve the archive itself.
                var archiveFile =
                    (await _filePicker.OpenSingleFileAsync("Open a mod archive to be imported.",
                        filterChoices: ArchiveTypesPick,
                        preselectedPath: _gameEnvironmentController.CurrentEnvironment!.RootPath))?.TryGetLocalPath();

                archiveToInstall = archiveFile;
            }

            // If no archive was set, stop here.
            if (string.IsNullOrEmpty(archiveToInstall))
            {
                await _dialogService.NotifyUser(Constants.FailDialog, "No archive has been selected!");
                return;
            }

            // Install archive generated here.
            var result = (await _dialogService.GenerateGenericLoadingProcess(
                null,
                null,
                "Installing Mod",
                null,
                (Delegate)_modInstaller.InstallModArchiveAsync,
                archiveToInstall
            )).Result;

            if (result is ModInstallationResult installationResult)
            {
                var installationFailed = !installationResult.Success || installationResult.Metadata == null;
                // Display security issues if any are present
                if (installationResult.HasSecurityIssues)
                {
                    var logContainer =
                        installationResult.SecurityIssues.ToLogContainer("Security Issue", LogType.Warning);

                    // If this wasn't intentional (cancel), show message.
                    if (!installationResult.Cancelled && !installationFailed)
                    {
                        // If security issues are found, don't proceed with installation
                        await _dialogService.NotifyUser(Constants.WarningDialog,
                            "The mod you're installing contains potential security issues. The installation has been cancelled.\nTo disable this security measure, check Settings.",
                            container: logContainer);
                    }
                }

                // If it fails to be installed, warn first.
                if (installationFailed)
                    await _dialogService.NotifyUser(Constants.FailDialog,
                        "Failed to install the mod!");
                else
                {
                    // Fill up afterward.
                    FillUpAllMods(_profileManager.ActiveProfile?.ModDataFiles);
                    
                    // Save profile first.
                    await _dialogService.GenerateBooleanLoadingProcess(
                        null,
                        null,
                        "Saving Profile",
                        null,
                        (Delegate)_profileManager.SaveActiveProfile);
                    
                    // Then, notify.
                    await _dialogService.NotifyUser(Constants.SuccessDialog,
                        $"Successfully installed '{installationResult.Metadata?.Name}'!");
                }
            }
            else
                await _dialogService.NotifyUser(Constants.FailDialog,
                    "Failed to install the mod!");
        }
        catch (Exception e)
        {
            Log.Logger.Error("Error during generation of archive for installation!\n{e}", e);
        }
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
                _modUninstaller.DeleteMod(modToDelete, profile => FillUpAllMods(profile.ModDataFiles));
            }
            catch (Exception e)
            {
                Log.Logger.Error("Failed to delete the mod directory for {ModName}!\n{Exception}", modToDelete.Name, e);
                await _dialogService.NotifyUser(Constants.FailDialog,
                    $"Failed to delete mod files for {modToDelete.Name}. Check file permissions.");
                return;
            }
        }

        await _dialogService.GenerateBooleanLoadingProcess(
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
            await _dialogService.NotifyUser(Constants.FailDialog,
                "The path to the mod is somehow invalid!\nTry reloading the profiles list.");
            return;
        }

        if (!await _directoryLauncher.OpenDirectoryInfo(new DirectoryInfo(path)))
        {
            await _dialogService.NotifyUser(Constants.FailDialog,
                "Failed to open the path to the mod due to an unknown error!\nTry reloading the profiles list.");
        }
    }
}
using System.Collections.ObjectModel;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GottaManagePlus.Models;
using GottaManagePlus.Models.UI;
using GottaManagePlus.Services;
using GottaManagePlus.Services.APIServices;
using GottaManagePlus.Services.GameEnvironmentServices;
using GottaManagePlus.Utils;
using Serilog;

namespace GottaManagePlus.ViewModels;

/// <summary>
/// ViewModel for the mod selection dialog, allowing users to browse, filter, sort, and queue mods for installation.
/// </summary>
public partial class ModSelectionDialogViewModel : DialogViewModel
{
    #region Nested Types

    /// <summary>
    /// Defines the available sorting/filtering options for the mod list.
    /// </summary>
    public enum FilterTypes
    {
        None,
        NameAscending,
        NameDescending,
        AuthorAscending,
        AuthorDescending,
        DateAddedAscending,
        DateAddedDescending,
        DateModifiedAscending,
        DateModifiedDescending,
        DateUpdatedAscending,
        DateUpdatedDescending,
        DownloadCountAscending,
        DownloadCountDescending,
        ViewCountAscending,
        ViewCountDescending
    }

    #endregion

    #region Private Fields

    private DialogService _dialogService = null!;
    private GamebananaApiService _gamebananaApiService = null!;
    private GameEnvironmentController _gameEnvironmentController = null!;
    private string? _lastSearchedTerm;
    private int _incrementalPageCounter = 1;
    private readonly SemaphoreSlim _loadSemaphore = new(1, 1); // For limiting the IncrementModsList concurrent calls
    private readonly Queue<IndexMod> _failedRecordsQueue = new();

    #endregion

    #region Public Properties

    /// <summary>
    /// Complete collection of all mods loaded from the API (unsorted, unfiltered).
    /// </summary>
    [ObservableProperty]
    protected internal partial ObservableCollection<ModItem> AllMods { get; set; } = [];

    /// <summary>
    /// Display collection of mods after applying the current filter/sort.
    /// </summary>
    [ObservableProperty]
    public partial ObservableCollection<ModItem> Mods { get; set; } = [];

    /// <summary>
    /// Dictionary of mods queued for installation, mapped to the selected file for each.
    /// </summary>
    [ObservableProperty]
    public partial AvaloniaDictionary<ModItem, ModItem.ModFile> EnqueuedModsToInstall { get; set; } = [];

    /// <summary>
    /// Currently selected mod in the UI.
    /// </summary>
    [ObservableProperty]
    public partial ModItem? SelectedMod { get; set; }

    /// <summary>
    /// Currently selected file for the selected mod.
    /// </summary>
    [ObservableProperty]
    public partial ModItem.ModFile? SelectedFile { get; set; }

    /// <summary>
    /// Whether a loading indicator should be shown while fetching mods.
    /// </summary>
    [ObservableProperty]
    public partial bool DisplayLoadingIndicator { get; set; }
    
    /// <summary>
    /// The error text to indicate something went wrong during loading.
    /// </summary>
    [ObservableProperty]
    public partial string? LoadingErrorText { get; set; }

    /// <summary>
    /// Text entered in the search box.
    /// </summary>
    [ObservableProperty]
    public partial string? SearchModText { get; set; }

    /// <summary>
    /// Currently selected filter type.
    /// </summary>
    [ObservableProperty]
    public partial FilterTypes SelectedFilterType { get; set; } = FilterTypes.None;

    /// <summary>
    /// Available filter options (for UI binding).
    /// </summary>
    public FilterTypes[] FilterOptions { get; } = Enum.GetValues<FilterTypes>();

    #endregion

    #region Constructor and Design-Time Support

    /// <summary>
    /// Parameterless constructor used only for design-time preview in Avalonia designer.
    /// </summary>
    public ModSelectionDialogViewModel()
    {
        if (!Design.IsDesignMode) return;
        AllMods = new ObservableCollection<ModItem>(MockModsGenerator.Generate());
        SelectedMod = AllMods[0];
        ApplyFilterAndUpdateDisplay(FilterTypes.None);
    }

    #endregion

    #region Setup and Initialization

    /// <summary>
    /// Initializes the ViewModel with required services and loads the first page of mods.
    /// </summary>
    /// <param name="args">Expected: DialogService, GamebananaApiService, GameEnvironmentController.</param>
    protected override void Setup(params object?[]? args)
    {
        try
        {
            _dialogService = GetValueOrException<DialogService>(args, 0);
            _gamebananaApiService = GetValueOrException<GamebananaApiService>(args, 1);
            _gameEnvironmentController = GetValueOrException<GameEnvironmentController>(args, 2);

            // If the counter is not 1, then this is not the first time that this dialog loads.
            if (_incrementalPageCounter != 1) return;
            
            Dispatcher.UIThread.Post(async void () =>
            {
                try
                {
                    DisplayLoadingIndicator = true;
                    await IncrementModsList(null);  // This runs after dialog is shown
                    
                }
                catch
                {
                    // ignore
                }
                finally
                {
                    DisplayLoadingIndicator = false;
                }
            }, DispatcherPriority.Background);
        }
        catch (Exception e)
        {
            Log.Logger.Error(e, "Found error during mod selection dialog setup.");
        }
    }

    #endregion

    #region Lifecycle Overrides
    
    protected override void OnClose()
    {
        base.OnClose();
        #if DEBUG
        Log.Logger.Information("---- Enqueued Mods ----");
        foreach (var mod in EnqueuedModsToInstall)
            Log.Logger.Information("{modItem} --> {modFile}", mod.Key.ToString(), mod.Value.ToString());
        #endif
    }

    #endregion

    #region Commands

    [RelayCommand]
    public void SelectMod(ModItem mod) => SelectedMod = mod;

    [RelayCommand]
    public void ToggleModToInstallQueue(ModItem mod)
    {
        // If the mod exists, remove it and unselect it.
        if (EnqueuedModsToInstall.Remove(mod) || SelectedFile == null)
        {
            mod.IsSelected = false;
            return;
        }

        // If the mod is not added, add it and select it.
        EnqueuedModsToInstall.Add(mod, SelectedFile);
        mod.IsSelected = true;
    }

    [RelayCommand]
    public async Task AddModBunch() => await IncrementModsList(null);

    [RelayCommand]
    public async Task InstallAsync()
    {
        if (EnqueuedModsToInstall.Count == 0) return;

        // Close this dialog before anything
        Close();
        
        // TODO: Implement an actual installation process.

        if (await _dialogService.PromptUserQuestion(
                "Confirm Installation",
                $"You are about to install {EnqueuedModsToInstall.Count} mod(s). Proceed?",
                DialogServiceUtils.QuestionAnswerType.ProceedOrCancel))
        {
            // Install asynchronously (install)
            if (await _dialogService.GenerateLoadingProcess(
                    null,
                    $"{EnqueuedModsToInstall.Count} mod(s) have been installed successfully.",
                    "Installing Mods",
                    null,
                    (Delegate)MockInstallAsync))
            {
                // Installation completed successfully
            }
        }
    }

    [RelayCommand]
    public async Task CancelAsync()
    {
        // If the dialog can install, prompt user for an "are you sure" question.
        if (EnqueuedModsToInstall.Count != 0 && !await _dialogService.PromptUserQuestion(
                "Cancel Installation",
                "You have selected mods that will not be installed. Are you sure?"))
            return;
        Close();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Initiates a search for mods by the given term.
    /// </summary>
    /// <param name="searchTerm">The search query.</param>
    public async Task SearchByTerm(string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || _lastSearchedTerm == searchTerm) return;

        try
        {
            _incrementalPageCounter = 0;
            lock (_failedRecordsQueue)
            {
                _failedRecordsQueue.Clear();
            }
            await IncrementModsList(searchTerm);
            _lastSearchedTerm = searchTerm;
        }
        catch
        {
            // ignored
        }
    }

    #endregion

    #region Event Handlers (Partial Methods)

    partial void OnAllModsChanged(ObservableCollection<ModItem> value)
    {
        // Update the queue to only contain mods that are selected from the Mods collection (intersection).
        EnqueuedModsToInstall = new AvaloniaDictionary<ModItem, ModItem.ModFile>(
            value.Where(mod => EnqueuedModsToInstall.ContainsKey(mod))
                .ToDictionary(mod => mod, mod => EnqueuedModsToInstall[mod])
        );
    }

    partial void OnSelectedFilterTypeChanged(FilterTypes value) => ApplyFilterAndUpdateDisplay(value);

    partial void OnSelectedModChanged(ModItem? value)
    {
        // If mod is null, reset the selected file.
        SelectedFile = value is null || value.AllEnvironmentallyValidFiles.Count == 0 ? null :
            // If mod isn't null, automatically choose the first option available for SelectedFile.
            value.AllEnvironmentallyValidFiles[0];
    }

    partial void OnSelectedFileChanged(ModItem.ModFile? value)
    {
        // If the file changed, update its queue if it exists.
        if (value != null && SelectedMod != null && EnqueuedModsToInstall.ContainsKey(SelectedMod))
            EnqueuedModsToInstall[SelectedMod] = value;
    }

    partial void OnDisplayLoadingIndicatorChanged(bool value)
    {
        // If the loading indicator shows up again, hide the loading error text.
        if (value)
            LoadingErrorText = null;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Loads the next page of mods from the API, optionally filtered by a search term.
    /// </summary>
    /// <param name="searchTerm">Search term or null to load the default feed.</param>
    private async Task IncrementModsList(string? searchTerm)
    {
        const string ModLoadFail = "Some mods failed to be loaded in."; // TODO: Localization needed here
        await _loadSemaphore.WaitAsync();
        List<ModItem> allModsMirror = [.. AllMods];
        try
        {
            // ====== Lost Records Retrieval ====== 
            DisplayLoadingIndicator = true;
            List<IndexMod> toRetry;
            lock (_failedRecordsQueue)
            {
                toRetry = new List<IndexMod>(_failedRecordsQueue);
                _failedRecordsQueue.Clear();
            }
            
            // Parallel Conversion.
            var conversionTasks = toRetry.Select(record =>
                record.ToModItem(_gamebananaApiService, _gameEnvironmentController)
            ).ToList();
            
            
            // Wait for the result of all tasks.
            var tasks = conversionTasks.Select(t =>
                t.ContinueWith(_ => { }));
            await Task.WhenAll(tasks);

            for (var i = 0; i < conversionTasks.Count; i++)
            {
                var task = conversionTasks[i];
                var record = toRetry[i]; // Each index in conversionTask corresponds to the IndexMod
                
                if (!task.IsCompletedSuccessfully)
                {
                    // Re-queue if the conversion failed.
                    lock (_failedRecordsQueue)
                    {
                        _failedRecordsQueue.Enqueue(record);
                    }
                    Log.Logger.Error(task.Exception, "An error occurred while incrementing the items list.");
                    LoadingErrorText = ModLoadFail;
                    continue;
                }
                var modItem = await record.ToModItem(_gamebananaApiService, _gameEnvironmentController);
                if (allModsMirror.Contains(modItem)) continue;
                
#if RELEASE
                if (modItem.AllValidFiles.Any())
#elif DEBUG
                if (modItem.AllFiles.Any())
#endif
                    allModsMirror.Add(modItem);
            }

            // If it is less than 0, then this is a final page
            if (_incrementalPageCounter < 0) return;

            // ====== Normal Submission Retrieval ======
            // Get a list of submissions with the current counter.
            var result = await _gamebananaApiService.GetSubmissionListAsync(_incrementalPageCounter++, searchTerm);

            // If the result failed, show the error.
            if (result.IsFailure)
            {
                Log.Logger.Error("{error}", result.Error);
                LoadingErrorText = result.Error;
                return;
            }

            var index = result.Value!;

            // Start all record tasks in parallel.
            conversionTasks = index.Records.Select(record =>
                record.ToModItem(_gamebananaApiService, _gameEnvironmentController)
            ).ToList();

            // Wait for the result of all tasks.
            tasks = conversionTasks.Select(t =>
                t.ContinueWith(_ => { }));
            await Task.WhenAll(tasks);

            // Add it to the Mods list.
            foreach (var task in conversionTasks)
            {
                // If the task wasn't completed, then check for exceptions.
                if (!task.IsCompletedSuccessfully)
                {
                    Log.Logger.Error(task.Exception, "An error occurred while incrementing the items list.");
                    LoadingErrorText = ModLoadFail;
                    continue;
                }

                var modItem = task.Result;

                // Ensure the mod item is not contained in the list of mods.
                if (allModsMirror.Contains(modItem)) continue;

                // If no files have a GMP root, then this is not a valid ModItem.
#if RELEASE
                if (!modItem.AllValidFiles.Any()) continue;
#elif DEBUG
                if (!modItem.AllFiles.Any()) continue; // Display all files to test retrieval access.
#endif

                // Finally, add the mod item to the list.
                allModsMirror.Add(modItem);
            }

            // Finally, if the metadata is complete, set an invalid page counter
            if (index.Metadata?.IsComplete == true)
                _incrementalPageCounter = -1;
        }
        catch (Exception e)
        {
            _incrementalPageCounter--;
            Log.Logger.Error(e, "Failed to generate a mod list from the API.");
            LoadingErrorText = "Failed to load mods from the API.";
        }
        finally
        {
            DisplayLoadingIndicator = false;
            _loadSemaphore.Release();
            AllMods = new ObservableCollection<ModItem>(allModsMirror);
            // Reapply current sorting after new items have been added
            ApplyFilterAndUpdateDisplay(SelectedFilterType);
        }
    }

    /// <summary>
    /// Sorts the <see cref="AllMods"/> collection according to the specified filter and updates <see cref="Mods"/>.
    /// </summary>
    /// <param name="filterType">The filter/sort to apply.</param>
    private void ApplyFilterAndUpdateDisplay(FilterTypes filterType)
    {
        // Using LINQ, filter the collection
        Mods = new ObservableCollection<ModItem>(filterType switch
        {
            FilterTypes.NameAscending =>
                AllMods.OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase),
            FilterTypes.NameDescending =>
                AllMods.OrderByDescending(m => m.Name, StringComparer.OrdinalIgnoreCase),
            FilterTypes.AuthorAscending =>
                AllMods.OrderBy(m => m.Author, StringComparer.OrdinalIgnoreCase),
            FilterTypes.AuthorDescending =>
                AllMods.OrderByDescending(m => m.Author, StringComparer.OrdinalIgnoreCase),
            FilterTypes.DateAddedAscending =>
                AllMods.OrderBy(m => m.DateAdded),
            FilterTypes.DateAddedDescending =>
                AllMods.OrderByDescending(m => m.DateAdded),
            FilterTypes.DateModifiedAscending =>
                AllMods.OrderBy(m => m.DateModified),
            FilterTypes.DateModifiedDescending =>
                AllMods.OrderByDescending(m => m.DateModified),
            FilterTypes.DateUpdatedAscending =>
                AllMods.OrderBy(m => m.DateUpdated),
            FilterTypes.DateUpdatedDescending =>
                AllMods.OrderByDescending(m => m.DateUpdated),
            FilterTypes.DownloadCountAscending =>
                AllMods.OrderBy(m => m.DownloadCount ?? 0),
            FilterTypes.DownloadCountDescending =>
                AllMods.OrderByDescending(m => m.DownloadCount ?? 0),
            FilterTypes.ViewCountAscending =>
                AllMods.OrderBy(m => m.ViewCount ?? 0),
            FilterTypes.ViewCountDescending =>
                AllMods.OrderByDescending(m => m.ViewCount ?? 0),
            _ => AllMods   // FilterTypes.None -> preserve original insertion order
        });
    }

    /// <summary>
    /// Mock installation task simulating download and installation of queued mods.
    /// </summary>
    private async Task<bool> MockInstallAsync(CancellationToken ct, IProgress<ProgressReport>? progress)
    {
        var total = EnqueuedModsToInstall.Count;
        var completed = 0;

        foreach (var (mod, file) in EnqueuedModsToInstall)
        {
            ct.ThrowIfCancellationRequested();

            progress?.Report(new ProgressReport(completed++, total, currentStatus: $"Downloading {mod.Name}"));

            // Simulate download
            await Task.Delay(1000, ct);

            progress?.Report(new ProgressReport(completed, total, currentStatus: $"Installing {mod.Name}"));

            // Simulate install
            await Task.Delay(800, ct);
        }

        progress?.Report(new ProgressReport(total, total, currentStatus: "Installed all mods successfully!"));

        return true;
    }

    #endregion
}

/// <summary>
/// Temporary mock data generator (to be replaced with API call).
/// </summary>
internal static class MockModsGenerator
{
    public static List<ModItem> Generate()
    {
        var items = new List<ModItem>();

        for (var i = 1; i <= 10; i++)
        {
            var emptyVersion = i is 2 or 7; // empty version for items 2 and 7

            var item = new ModItem
            {
                Id = 600000 + i,
                Name = $"Dummy Mod {i}",
                Description = $"This is a dummy description for mod number {i}. It contains some interesting features.",
                Submitter = new ModItem.ModSubmitter
                {
                    Name = $"Submitter_{i}"
                },
                DateModified = DateTime.UtcNow.AddDays(-i),
                DateUpdated = DateTime.UtcNow.AddDays(-i + 1),
                DateAdded = DateTime.UtcNow.AddDays(-i - 5),
                DownloadCount = 100 * i,
                ViewCount = 500 * i,
                LikeCount = 10 * i,
                DownloadUrl = $"https://gamebanana.com/mods/download/{600000 + i}",
                PreviewMedia = new ModItem.ModPreviewMedia
                {
                    Images =
                    [
                        new ModItem.ModImage
                        {
                            BaseUrl = "https://images.gamebanana.com/img/ss/mods",
                            File = $"dummy_{i}.jpg",
                            File100 = $"dummy_{i}_100.jpg",
                            File530 = $"dummy_{i}_530.jpg"
                        }
                    ]
                },
                IsPrivate = i % 3 == 0,
                IsTrashed = i == 5,
                Version = emptyVersion ? string.Empty : $"1.{i % 5}.{i}",
                CommentsMode = i % 2 == 0 ? "open" : "closed",
                UpdatesCount = i,
                HasUpdates = i % 2 == 0,
                AllTodosCount = i * 2,
                HasTodos = i % 3 == 0,
                PostCount = i * 3,
                CreatedBySubmitter = true,
                IsPorted = i == 3,
                ThanksCount = i * 2,
                InitialVisibility = i % 2 == 0 ? "show" : "hide",
                PayType = i == 1 ? "paid" : "free",
                GenerateTableOfContents = i % 2 == 0,
                Text = """<h2>Please report new issues on <a href="https://github.com/Fasguy/BaldisBasicsModMenu-archive/issues">GitHub</a>, if possible.</h2>\r\n<h1><strong>Features</strong></h1><ul class="SelectedElement">\r\n    <li>Make characters ignore you</li>\r\n    <li>Change the speed, size and visiblity of characters</li>\r\n    <li>Enable, disable and clone characters</li>\r\n    <li>Edit your inventory and item values</li>\r\n    <li>Make characters ignore you</li>\r\n    <li>Character, notebook and Exit teleporters</li>\r\n    <li>Infinite items</li>\r\n    <li>Scene manager</li>\r\n    <li>Noclip</li>\r\n    <li>An answer to the impossible question</li>\r\n    <li>And more!</li></ul>\r\n<br>\r\n<h1><strong>Looking for an older version?</strong></h1><a href="https://fasguy.github.io/vault/" target="_blank">\r\nCheck the vault</a>""",
                ShowRipePromo = false,
                FollowLinks = true,
                AccessorHasUnliked = false,
                AccessorHasLiked = i % 2 == 0,
                AccessorHasThanked = i % 3 == 0,
                AccessorIsSubscribed = i == 1,
                AccessorSubscriptionRowId = i * 100,
                AdvancedRequirementsExist = i == 9,
                Files = [],
                ArchivedFiles = [],
                Credits = [],
                Requirements = []
            };

            // Add dummy files
            for (var f = 1; f <= 2; f++)
            {
                var modFile = new ModItem.ModFile
                {
                    Id = item.Id * 10 + f,
                    FileName = $"mod_file_{f}.zip",
                    Version = item.Version,
                    FileSize = 1024 * 1024 * f,
                    DateAdded = DateTime.UtcNow.AddDays(-f),
                    DownloadCount = 50 * f,
                    DownloadUrl = $"{item.DownloadUrl}/file/{f}",
                    Md5Checksum = Guid.NewGuid().ToString("N"),
                    AnalysisState = "clean",
                    AnalysisResult = "ok",
                    AnalysisResultVerbose = "No issues found",
                    AvState = "safe",
                    AvResult = "clean",
                    IsArchived = f == 2,
                    HasContents = true,
                    AnalysisWarnings = new Dictionary<string, List<string>>(),
                    ModManagerIntegrations = []
                };

                // Add a dummy warning for the first file of mod 6
                if (i == 6 && f == 1)
                {
                    modFile.AnalysisWarnings["_suspicious_behavior"] = ["Found potential issue"];
                }

                if (f == 1)
                    item.Files.Add(modFile);
                else
                    item.ArchivedFiles.Add(modFile);
            }

            // Add dummy credits groups
            var creditGroup = new ModItem.ModCreditsGroup
            {
                GroupName = "Authors",
                Authors =
                [
                    new ModItem.ModCreditAuthor
                    {
                        Role = "Main Creator",
                        Id = i * 1000,
                        Name = $"Author_{i}",
                        UpicUrl = $"https://example.com/upic_{i}.png",
                        ProfileUrl = $"https://gamebanana.com/members/{i}",
                        AvatarUrl = $"https://example.com/avatar_{i}.png",
                        IsOnline = i % 2 == 0,
                        AffiliatedStudio = new ModItem.ModAffiliatedStudio
                        {
                            ProfileUrl = "https://gamebanana.com/studio/1",
                            Name = "Dummy Studio",
                            FlagUrl = "https://example.com/flag.png",
                            BannerUrl = "https://example.com/banner.png"
                        }
                    }
                ]
            };
            item.Credits.Add(creditGroup);

            // Add dummy requirements
            if (i % 2 == 0)
            {
                item.Requirements.Add(["Requires XYZ", "Version > 1.0"]);
                item.Requirements.Add(["Optional: ABC"]);
            }

            items.Add(item);
        }

        return items;
    }
}
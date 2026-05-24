using System.Collections.ObjectModel;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GottaManagePlus.Models;
using GottaManagePlus.Models.UI;
using GottaManagePlus.Services;
using GottaManagePlus.Services.APIServices;
using GottaManagePlus.Utils;
using Serilog;

namespace GottaManagePlus.ViewModels;

public partial class ModSelectionDialogViewModel : DialogViewModel
{
    // ---- Private ----
    private DialogService _dialogService = null!;
    private GamebananaApiService _gamebananaApiService = null!;
    [ObservableProperty] protected internal partial ObservableCollection<ModItem> AllMods { get; set; } = [];
    public FilterTypes[] FilterOptions { get; } = Enum.GetValues<FilterTypes>();
    private int incrementalPageCounter = 1;
    public enum FilterTypes { None = 0 /* TODO: Implement filters for searching the collection */}
    
    // ---- Public ----
    [ObservableProperty]
    public partial ObservableCollection<ModItem> Mods { get; set; } = [];

    [ObservableProperty] 
    public partial ObservableCollection<ModItem> EnqueuedModsToInstall { get; set; } = [];

    [ObservableProperty]
    public partial ModItem? SelectedMod { get; set; }
    
    [ObservableProperty]
    public partial bool DisplayLoadingIndicator { get; set; }
    
    // AutoCompleteBox
    [ObservableProperty]
    public partial ModItem? CurrentSearchedMod { get; set; }
    
    [ObservableProperty]
    public partial string? SearchModText { get; set; }
    
    // ComboBox Filter
    [ObservableProperty] 
    public partial FilterTypes SelectedFilterType { get; set; } = FilterTypes.None;
    

    // Commands
    [RelayCommand]
    public void SelectMod(ModItem mod) => SelectedMod = mod;

    [RelayCommand]
    public void ToggleModToInstallQueue(ModItem mod)
    {
        // If the mod exists, remove it and unselect it.
        if (EnqueuedModsToInstall.Remove(mod))
        {
            mod.IsSelected = false;
            return;
        }
        
        // If the mod is not added, add it and select it.
        EnqueuedModsToInstall.Add(mod);
        mod.IsSelected = true;
    }

    [RelayCommand]
    public async Task AddModBunch() => await IncrementModsList();

    [RelayCommand]
    public async Task InstallAsync()
    {
        if (EnqueuedModsToInstall.Count == 0) return;

        // Close this dialog before anything
        Close();
        
        if (await _dialogService.PromptUserQuestion(
                "Confirm Installation",
                $"You are about to install {EnqueuedModsToInstall.Count} mod(s). Proceed?",
                DialogServiceUtils.QuestionAnswerType.ProceedOrCancel
                ))
        {
            // Install asynchronously (install)
            if (await _dialogService.GenerateLoadingProcess(
                    null,
                    $"{EnqueuedModsToInstall.Count} mod(s) have been installed successfully.",
                    "Installing Mods", 
                    null, 
                    (Delegate)MockInstallAsync
                    ))
            {
                
            }
        }
    }

    [RelayCommand]
    public async Task CancelAsync()
    {
        // If the dialog can install, prompt user for an "are you sure" question.
        if (EnqueuedModsToInstall.Count != 0 && !await _dialogService.PromptUserQuestion(
                "Cancel Installation",
                "You have selected mods that will not be installed. Are you sure?"
            ))
            return;
        Close();
    }

    // Mock installation task
    private async Task<bool> MockInstallAsync(CancellationToken ct, IProgress<ProgressReport>? progress)
    {
        var total = EnqueuedModsToInstall.Count;
        var completed = 0;

        foreach (var mod in EnqueuedModsToInstall)
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
    
    // Constructor for Design setup
    public ModSelectionDialogViewModel()
    {
        if (!Design.IsDesignMode) return;
        AllMods = new ObservableCollection<ModItem>(MockModsGenerator.Generate());
        SelectedMod = AllMods[0];
        ApplyFilterAndUpdateDisplay(FilterTypes.None);
    }

    // Setup method
    protected override async void Setup(params object?[]? args)
    {
        try
        {
            _dialogService = GetValueOrException<DialogService>(args, 0);
            _gamebananaApiService = GetValueOrException<GamebananaApiService>(args, 1);

            var f = await _gamebananaApiService.GetIndexedFileFromFileId(1709497);
            
            // Clear collection
            AllMods.Clear();
            
            // Fill it up
            await IncrementModsList();
            
            // Update the UI display
            ApplyFilterAndUpdateDisplay(FilterTypes.None);
        }
        catch (Exception e)
        {
            Log.Logger.Error(e, "Found error during mod selection dialog setup.");
        }
    }

    partial void OnAllModsChanged(ObservableCollection<ModItem> value)
    {
        // Update the queue to only contain mods that are selected from the Mods collection
        EnqueuedModsToInstall = new ObservableCollection<ModItem>(value.Intersect(EnqueuedModsToInstall));
    }
    
    private async Task IncrementModsList()
    {
        // If it is less than 0, then this is a final page
        if (incrementalPageCounter < 0) return;
        
        DisplayLoadingIndicator = true;
        try
        {
            // Get a list of submissions with the current counter.
            var index = await _gamebananaApiService.GetSubmissionListAsync(incrementalPageCounter++);
            
            // Add it to the Mods list.
            foreach (var modItem in 
                     index.Records.Select(mod => mod.ToModItem())
                         .Where(modItem => !AllMods.Contains(modItem)))
            {
                // Attempt to load its thumbnail image too.
                await modItem.AttemptToLoadImagesFromURLs(_gamebananaApiService, false, null);
                
                // Then, add the mod item to the list.
                AllMods.Add(modItem);
            }

            // Finally, if the metadata is complete, set an invalid page counter
            if (index.Metadata?.IsComplete == true)
                incrementalPageCounter = -1;
        }
        catch (Exception e)
        {
            incrementalPageCounter--;
            Log.Logger.Error(e, "Failed to generate a mod list from the API.");
        }
        finally
        {
            DisplayLoadingIndicator = false;
        }
    }

    private void ApplyFilterAndUpdateDisplay(FilterTypes filterType)
    {
        // Using LINQ, filter the collection
        Mods = new ObservableCollection<ModItem>(filterType switch
        {
            FilterTypes.None => AllMods,
            _ => AllMods
        });
    }
}

// Temporary mock data generator (to be replaced with API call)
internal static class MockModsGenerator
{
    public static List<ModItem> Generate()
    {
        // TODO: Use GamebananaApiService to fetch real mods
        return
        [
            new ModItem
            {
                Id = 610036,
                Name = "The RMS cast in BB+ (V4.4, 0.14.2)",
                Description = "Adds the RMS cast from the game 'The Room' to Baldi's Basics Plus.",
                Author = "Mimicry like from Times",
                Version = "4.4.0.0",
                DownloadUrl = "https://gamebanana.com/mods/download/610036",
                // Placeholder URLs – replace with real images
                ThumbnailUrl = "https://images.gamebanana.com/img/ss/mods/100-90_69ab9f7dbffff.jpg",
                ImageUrl = "https://images.gamebanana.com/img/ss/mods/530-90_69ab9f7dbffff.jpg"
            },

            new ModItem
            {
                Id = 610037,
                Name = "Custom Notebooks Plus",
                Description = "Custom notebook textures with new designs.",
                Author = "PixelGuy",
                Version = "2.1.0",
                DownloadUrl = "https://gamebanana.com/mods/download/610037",
                ThumbnailUrl = "https://images.gamebanana.com/img/ss/mods/100-90_68b69afae5212.jpg",
                ImageUrl = "https://images.gamebanana.com/img/ss/mods/530-90_68b69afae5212.jpg"
            },

            new ModItem
            {
                Id = 610038,
                Name = "Better Audio Engine",
                Description = "Enhances audio quality and adds new sound effects.",
                Author = "Onefive",
                Version = "1.3.2",
                DownloadUrl = "https://gamebanana.com/mods/download/610038",
                ThumbnailUrl = "https://images.gamebanana.com/img/ss/mods/100-90_688ab83cd464f.jpg",
                ImageUrl = "https://images.gamebanana.com/img/ss/mods/530-90_688ab83cd464f.jpg"
            },

            new ModItem
            {
                Id = 610039,
                Name = "Halloween Update 2025",
                Description = "Spooky decorations and new challenges for October.",
                Author = "bigthinker373",
                Version = "1.0.0",
                DownloadUrl = "https://gamebanana.com/mods/download/610039",
                ThumbnailUrl = "https://images.gamebanana.com/img/ss/mods/100-90_6887ed9373c34.jpg",
                ImageUrl = "https://images.gamebanana.com/img/ss/mods/530-90_6887ed9373c34.jpg"
            },

            new ModItem
            {
                Id = 610040,
                Name = "Minigame Overhaul",
                Description = "Completely reworks all minigames with new mechanics.",
                Author = "AlexBW145",
                Version = "3.0.0",
                DownloadUrl = "https://gamebanana.com/mods/download/610040",
                ThumbnailUrl = "https://images.gamebanana.com/img/ss/mods/100-90_694b1a449a3e4.jpg",
                ImageUrl = "https://images.gamebanana.com/img/ss/mods/530-90_694b1a449a3e4.jpg"
            }
        ];
    }
}
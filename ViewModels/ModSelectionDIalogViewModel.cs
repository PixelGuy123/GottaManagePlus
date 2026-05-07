using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GottaManagePlus.Models;
using GottaManagePlus.Models.UI;
using GottaManagePlus.Services;

namespace GottaManagePlus.ViewModels;

public partial class ModSelectionDialogViewModel : DialogViewModel
{
    private DialogService _dialogService = null!;

    [ObservableProperty]
    public partial string Title { get; set; } = "Select Mods to Install";

    [ObservableProperty]
    public partial ObservableCollection<ModItem> Mods { get; set; } = [];

    [ObservableProperty]
    public partial ModItem? SelectedMod { get; set; }

    [ObservableProperty]
    public partial int SelectedCount { get; set; }

    [ObservableProperty]
    public partial bool IsInstallEnabled { get; set; }

    [ObservableProperty]
    public partial string InstallButtonText { get; set; } = "Install (0)";

    // Commands
    [RelayCommand]
    public void SelectMod(ModItem mod) => SelectedMod = mod;

    [RelayCommand(CanExecute = nameof(CanInstall))]
    public async Task InstallAsync()
    {
        if (SelectedCount == 0) return;

        var confirmDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
        confirmDialog.Prepare(false, "Confirm Installation", 
            $"You are about to install {SelectedCount} mod(s). Proceed?",
            "Install", "Cancel");
        
        if (confirmDialog.Confirmed)
        {
            // Prepare loading dialog
            var loadingDialog = _dialogService.GetDialog<LoadingDialogViewModel>();
            loadingDialog.Prepare("Installing Mods", "Initializing...", 
                MockInstallAsync); // TODO: Use extension approach instead
            
            // Show loading dialog and wait for result
            var success = await loadingDialog.StartTask();
            
            if (success)
            {
                var doneDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
                doneDialog.Prepare(true, "Installation Complete", 
                    $"{SelectedCount} mod(s) have been installed successfully.");
                // await doneDialog.WaitForCloseAsync();
            }
        }
    }

    private bool CanInstall() => SelectedCount > 0;

    [RelayCommand]
    public void CancelAsync()
    {
        if (SelectedCount > 0)
        {
            // TODO: Use extension approach instead.
            var confirmDialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
            confirmDialog.Prepare(false, "Cancel Installation", 
                "You have selected mods that will not be installed. Are you sure?", 
                "Yes, cancel", "Continue");
            
            if (!confirmDialog.Confirmed)
                return;
        }
        Close();
    }

    // Mock installation task
    private async Task<bool> MockInstallAsync(CancellationToken ct, IProgress<ProgressReport>? progress)
    {
        var selectedMods = Mods.Where(m => m.IsSelected).ToList();
        var total = selectedMods.Count;
        var completed = 0;

        foreach (var mod in selectedMods)
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

    // Setup method
    protected override void Setup(params object?[]? args)
    {
        _dialogService = GetValueOrException<DialogService>(args, 0);

        // Generate mock mods (temporary)
        Mods = new ObservableCollection<ModItem>(MockModsGenerator.Generate());

        // Select first mod by default
        if (Mods.Count > 0)
            SelectedMod = Mods[0];

        UpdateSelectionCount();
    }

    // Helper to update counts
    private void UpdateSelectionCount()
    {
        SelectedCount = Mods.Count(m => m.IsSelected);
        IsInstallEnabled = SelectedCount > 0;
        InstallButtonText = $"Install ({SelectedCount})";
    }

    // TODO: Add event handler for IsSelected changes
    partial void OnModsChanged(ObservableCollection<ModItem> value)
    {
        // Subscribe to each item's property changed
        foreach (var mod in value)
        {
            mod.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ModItem.IsSelected))
                    UpdateSelectionCount();
            };
        }
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
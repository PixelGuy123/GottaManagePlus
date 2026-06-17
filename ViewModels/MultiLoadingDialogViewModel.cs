using System.Collections.ObjectModel;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using GottaManagePlus.Models;

namespace GottaManagePlus.ViewModels;

public partial class MultiLoadingDialogViewModel : DialogViewModel
{
    /// <summary>
    /// Default constructor. If the view is in design mode, 
    /// populates the dialog with sample data for preview.
    /// </summary>
    public MultiLoadingDialogViewModel()
    {
        if (!Design.IsDesignMode) return;
        
        // Create dummy child view models
        LoadingDialogs =
        [
            new LoadingDialogViewModel
            {
                Title = "Downloading Mod",
                Status = "Progress: 50%",
                ProgressMax = 100,
                ProgressValue = 50
            },

            new LoadingDialogViewModel
            {
                Title = "Installing Package",
                Status = "Extracting files...",
                ProgressMax = 10,
                ProgressValue = 7
            },

            new LoadingDialogViewModel
            {
                Title = "Processing Data",
                Status = "Completed",
                ProgressMax = 1,
                ProgressValue = 1
            }
        ];

        // Overall multi‑dialog state
        Title = "Multiple Tasks Loading";
        Status = "2/3 tasks completed";
        ProgressMax = 3;
        ProgressValue = 2;
    }
    private readonly Lock _lock = new();
    private int _completedCount = 0;
    private int _totalCount = 0;
    private readonly TaskCompletionSource<bool> _allTasksCompletedTcs = new();
    // Observable properties
    [ObservableProperty] 
    public partial ObservableCollection<LoadingDialogViewModel> LoadingDialogs { get; set; } = [];
    
    [ObservableProperty]
    public partial string Title { get; set; } = "Loading...";

    [ObservableProperty]
    public partial string? Status { get; set; } = "Loading...";
    
    [ObservableProperty]
    public partial long ProgressMax { get; set; } = 1;

    [ObservableProperty]
    public partial long ProgressValue { get; set; }

    public List<object?> Results { get; private set; } = [];
    public Task WhenAllTasksCompleted => _allTasksCompletedTcs.Task;

    public void InsertLoadingTask(LoadingDialogViewModel loadingVm)
    {
        lock (_lock)
        {
            // Add to UI collection
            LoadingDialogs.Add(loadingVm);
            
            // Increase total count for main progress
            _totalCount++;
            UpdateMainProgress();

            // Store the task
            var task = loadingVm.StartTask();

            // Continuation that runs when this individual task completes
            task.ContinueWith(_ =>
            {
                lock (_lock)
                {
                    _completedCount++;
                    UpdateMainProgress();

                    // If all tasks are done, signal completion
                    if (_completedCount >= _totalCount)
                    {
                        _allTasksCompletedTcs.TrySetResult(true);
                        // Optionally collect results from each LoadingDialogViewModel
                        Results = LoadingDialogs.Select(vm => vm.Result).ToList();
                    }
                }
            }, TaskScheduler.Default); // or use the UI scheduler if needed
        }
    }

    private void UpdateMainProgress()
    {
        ProgressMax = _totalCount;
        ProgressValue = _completedCount;
        Status = $"Loading {_completedCount}/{_totalCount}";
    }

    /// <summary>
    /// Set up the dialog with the following parameters:
    /// <list type="number">
    ///     <item><description><see cref="string"/> Title (optional)</description></item>
    ///     <item><description><see cref="string"/> Status (optional)</description></item>
    /// </list>
    /// </summary>
    /// <param name="args">The positional arguments as defined in the summary.</param>
    protected override void Setup(params object?[]? args)
    {
        // Throw if null, since there are two required arguments afterward
        ArgumentNullException.ThrowIfNull(args);

        // Update UI Elements
        Title = TryGetValue(args, 0, out string? text) ? text : "Loading...";
        Status = TryGetValue(args, 1, out text) ? text : "Loading...";
    }
}
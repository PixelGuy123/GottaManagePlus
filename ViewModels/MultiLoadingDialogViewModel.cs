using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;

namespace GottaManagePlus.ViewModels;

public partial class MultiLoadingDialogViewModel : DialogViewModel
{
    public MultiLoadingDialogViewModel()
    {
        if (!Design.IsDesignMode) return;

        // Design‑time dummy data (unchanged)
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

        Title = "Multiple Tasks Loading";
        Status = "2/3 tasks completed";
        ProgressMax = 3;
        ProgressValue = 2;
    }

    // --- Observable properties ---
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

    // --- Internal tracking ---
    private int _totalCount;
    private int _completedCount;
    private readonly Lock _progressLock = new();

    /// <summary>
    /// Adds a loading task to the dialog. Does not start the task.
    /// Call this for each sub‑operation, then call <see cref="RunTasksAsync"/> to start processing.
    /// </summary>
    public void InsertLoadingTask(LoadingDialogViewModel loadingVm) =>
        LoadingDialogs.Add(loadingVm);

    /// <summary>
    /// Starts all previously inserted tasks, updates overall progress,
    /// and closes the dialog when every task has finished (or been canceled).
    /// </summary>
    public async Task RunTasksAsync()
    {
        // Snapshot total number of tasks
        _totalCount = LoadingDialogs.Count;
        UpdateMainProgress();

        // Build a wrapper task for each LoadingDialogViewModel
        var tasks = LoadingDialogs.Select(RunSingleTaskAsync).ToList();

        try
        {
            // Wait for all wrappers to complete (none should throw)
            await Task.WhenAll(tasks);
        }
        finally
        {
            // Collect results from every view model (even if some failed)
            Results = LoadingDialogs.Select(vm => vm.Result).ToList();

            // Close the dialog on the UI thread
            if (Dispatcher.UIThread.CheckAccess())
                Close();
            else
                await Dispatcher.UIThread.InvokeAsync(Close);
        }
    }

    private async Task RunSingleTaskAsync(LoadingDialogViewModel vm)
    {
        try
        {
            await vm.StartTask();
        }
        catch (OperationCanceledException)
        {
            // Task was canceled, treat as completed
            await OnTaskCompletedAsync();
        }
        catch (Exception ex)
        {
            // Log the error; the UI can still show the failure via the child VM's Status
            Log.Logger.Error(ex, "Loading task '{Title}' failed", vm.Title);
            await OnTaskCompletedAsync();
        }
        finally
        {
            await OnTaskCompletedAsync();
        }
    }

    private async Task OnTaskCompletedAsync()
    {
        // Marshal the progress update to the UI thread
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            lock (_progressLock)
            {
                _completedCount++;
                UpdateMainProgress();
            }
        });
    }

    private void UpdateMainProgress()
    {
        // Allowed only on the UI thread (called from InvokeAsync)
        ProgressMax = _totalCount;
        ProgressValue = _completedCount;
        Status = $"{_completedCount}/{_totalCount} tasks completed";
    }

    protected override void Setup(params object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(args);
        Title = TryGetValue(args, 0, out string? text) ? text : "Loading...";
        Status = TryGetValue(args, 1, out text) ? text : "Loading...";
        LoadingDialogs.Clear();
    }
}
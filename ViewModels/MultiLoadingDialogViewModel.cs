using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using GottaManagePlus.Models.DialogManagement;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace GottaManagePlus.ViewModels;

public partial class MultiLoadingDialogViewModel : DialogViewModel
{
    // ========== Dependencies ==========
    private readonly IServiceScopeFactory _serviceProvider = null!;

    // ========== Observable Properties ==========
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

    // ========== Public Properties ==========
    public List<object?> Results { get; private set; } = [];

    // ========== Internal State ==========
    private int _totalCount;
    private int _completedCount;
    private readonly Lock _progressLock = new();

    // ========== Constructors ==========
    public MultiLoadingDialogViewModel()
    {
        if (!Design.IsDesignMode) return;
        InitializeDesignTimeData();
    }

    public MultiLoadingDialogViewModel(IServiceScopeFactory serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    // ========== Dialog Lifecycle ==========
    protected override async Task<object?> OnShow(DialogContext? context)
    {
        if (context is not MultiLoadingDialogContext multiContext) return null;

        InitializeDialog(multiContext);
        var tasks = CreateLoadingTasks(multiContext);

        try
        {
            await Task.WhenAll(tasks);
        }
        finally
        {
            await FinalizeDialogAsync();
        }

        return null;
    }

    // ========== Private Methods - Initialization ==========
    private void InitializeDesignTimeData()
    {
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

    private void InitializeDialog(MultiLoadingDialogContext context)
    {
        Title = context.Title ?? "Processing";
        Status = context.Status ?? "Loading...";
    }

    // ========== Private Methods - Task Management ==========
    private List<Task> CreateLoadingTasks(MultiLoadingDialogContext context)
    {
        using var scope = _serviceProvider.CreateScope();
        LoadingDialogs.Clear();

        var tasks = new List<Task>();
        foreach (var loadingContext in context.LoadingDialogContexts)
        {
            var loadingViewModel = scope.ServiceProvider.GetRequiredService<LoadingDialogViewModel>();
            tasks.Add(RunSingleTaskAsync(loadingViewModel, loadingContext));
            LoadingDialogs.Add(loadingViewModel);
        }

        _totalCount = LoadingDialogs.Count;
        UpdateMainProgress();

        return tasks;
    }

    private async Task RunSingleTaskAsync(LoadingDialogViewModel vm, LoadingDialogContext context)
    {
        try
        {
            await vm.Show(context);
        }
        catch (OperationCanceledException)
        {
            // Task was canceled, treat as completed
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Loading task '{Title}' failed", vm.Title);
        }
        finally
        {
            await OnTaskCompletedAsync();
        }
    }

    // ========== Private Methods - Progress Updates ==========
    private async Task OnTaskCompletedAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(IncrementCompletedCount);
    }

    private void IncrementCompletedCount()
    {
        lock (_progressLock)
        {
            _completedCount++;
            UpdateMainProgress();
        }
    }

    private void UpdateMainProgress()
    {
        ProgressMax = _totalCount;
        ProgressValue = _completedCount;
        Status = $"{_completedCount}/{_totalCount} tasks completed";
    }

    // ========== Private Methods - Finalization ==========
    private async Task FinalizeDialogAsync()
    {
        Results = LoadingDialogs.Select(vm => vm.Result).ToList();
        await CloseDialogAsync();
    }

    private async Task CloseDialogAsync()
    {
        if (Dispatcher.UIThread.CheckAccess())
            Close();
        else
            await Dispatcher.UIThread.InvokeAsync(Close);
    }
}
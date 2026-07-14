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
using GottaManagePlus.Models.DialogManagement;
using GottaManagePlus.Services;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace GottaManagePlus.ViewModels;

public partial class MultiLoadingDialogViewModel(DialogService dialogService) : DialogViewModel
{
    // ========== Dependencies ==========
    private readonly DialogService _dialogService = dialogService;

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
    public MultiLoadingDialogViewModel() : this(null!)
    {
        if (!Design.IsDesignMode) return;
        InitializeDesignTimeData();
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
            Results = LoadingDialogs.Select(vm => vm.Result).ToList();
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
        LoadingDialogs.Clear();

        var tasks = new List<Task>();
        foreach (var loadingContext in context.LoadingDialogContexts)
        {
            var loadingViewModel = _dialogService.GetUnmanagedDialog<LoadingDialogViewModel>();
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
    private async Task OnTaskCompletedAsync() =>
        await Dispatcher.UIThread.InvokeAsync(IncrementCompletedCount);
    

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
   
}
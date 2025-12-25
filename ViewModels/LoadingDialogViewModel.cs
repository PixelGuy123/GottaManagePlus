using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GottaManagePlus.ViewModels;

public partial class LoadingDialogViewModel : DialogViewModel
{
    // Cache to store the parameter layout of methods to avoid repeated reflection (concurrent for thread safety)
    private static readonly ConcurrentDictionary<MethodInfo, ParameterInfo[]> MethodCache = new();

    // Private members
    private readonly Delegate _loadingDelegate;
    private readonly object?[] _providedArgs;
    private readonly CancellationTokenSource _cts = new();
    private bool _hasAlreadyInitiated;

    // Public getters
    public bool AllowCancellation { get; }
    public bool HideProgressBar { get; }

    // Observable properties
    [ObservableProperty] private string _title = "Loading...";
    [ObservableProperty] private string? _status;
    [ObservableProperty] private string? _progressPercentage = "0.0%";
    [ObservableProperty] private string _cancelText = "Cancel";
    [ObservableProperty] private Progress<(double, string?)>? _progress;

    /// <summary>
    /// Unified constructor that accepts a Delegate and dynamic arguments.
    /// </summary>
    /// <param name="loadingFunc">The method to execute.</param>
    /// <param name="args">Optional arguments required by the method (excluding <see cref="IProgress{double}"/> and <see cref="CancellationToken"/>).</param>
    public LoadingDialogViewModel(Delegate loadingFunc, params object?[]? args)
    {
        _loadingDelegate = loadingFunc ?? throw new ArgumentNullException(nameof(loadingFunc));
        _providedArgs = args ?? [];

        // Retrieve or Cache Method Parameters
        var parameters = MethodCache.GetOrAdd(_loadingDelegate.Method, m => m.GetParameters());

        // Dynamic UI State Detection
        AllowCancellation = parameters.Any(p => p.ParameterType == typeof(CancellationToken) || p.ParameterType == typeof(CancellationToken?));
        HideProgressBar = !parameters.Any(p => typeof(IProgress<(double, string?)>).IsAssignableFrom(p.ParameterType));
        
        if (!HideProgressBar) // If there's progress bar, there's progress instance
            Progress = new Progress<(double, string?)>();
    }

    public LoadingDialogViewModel()
    {
        if (!Design.IsDesignMode) throw new InvalidOperationException("DesignMode is not active!");
        // Design-time support logic
        _loadingDelegate = null!;
        _providedArgs = null!;
        Progress = null!;
    }

    public async Task<bool> StartTask()
    {
        if (_hasAlreadyInitiated)
            throw new InvalidOperationException("Loading task has already been called!");

        _hasAlreadyInitiated = true;

        if (Progress != null)
            Progress.ProgressChanged += OnProgressChanged;

        try
        {
            // Get the params
            var parameters = MethodCache[_loadingDelegate.Method];
            var finalArgs = new object?[parameters.Length];
            var providedArgIndex = 0;

            for (var i = 0; i < parameters.Length; i++)
            {
                // Get the param type
                var pType = parameters[i].ParameterType;

                // Assign to CT if it is one
                if (pType == typeof(CancellationToken) || pType == typeof(CancellationToken?))
                {
                    finalArgs[i] = _cts.Token;
                } // Otherwise, check if it is a IProgress
                else if (typeof(IProgress<(double, string?)>).IsAssignableFrom(pType))
                {
                    finalArgs[i] = Progress;
                } // Below them, just use these as arguments
                else if (providedArgIndex < _providedArgs.Length)
                {
                    finalArgs[i] = _providedArgs[providedArgIndex++];
                }
                else
                {
                    finalArgs[i] = parameters[i].HasDefaultValue ? parameters[i].DefaultValue : null;
                }
            }

            var result = _loadingDelegate.DynamicInvoke(finalArgs);

            switch (result)
            {
                case Task<bool> boolTask:
                {
                    var success = await boolTask;
                    return success && !_cts.IsCancellationRequested;
                }
                case Task task:
                    await task;
                    return !_cts.IsCancellationRequested;
                default:
                    return true;
            }
        }
        catch (TargetInvocationException ex) when (ex.InnerException is OperationCanceledException)
        {
            return false;
        }
        catch (Exception)
        {
            // By default, unhandled stuff is skipped
            return false;
        }
        finally
        {
            if (Progress != null)
                Progress.ProgressChanged -= OnProgressChanged;
            
            Close();
        }
    }

    private void OnProgressChanged(object? sender, (double, string?) e)
    {
        ProgressPercentage = (e.Item1 * 100.0).ToString("P");
        Status = e.Item2;
    }

    [RelayCommand]
    public void Cancel() => _cts.Cancel();
}
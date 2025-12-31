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
    private Delegate _loadingDelegate = null!;
    private object?[] _providedArgs = [];
    private CancellationTokenSource _cts = new();
    private bool _hasAlreadyInitiated;

    // Public getters
    public bool AllowCancellation { get; private set; }
    public bool HideProgressBar { get; private set; }

    // Observable properties
    [ObservableProperty] private string _title = "Loading...";
    [ObservableProperty] private string? _status = "Loading...";
    [ObservableProperty] private string? _progressPercentageText;
    [ObservableProperty] private int _progressMax = 1;
    [ObservableProperty] private int _progressValue = 0;
    [ObservableProperty] private string _cancelText = "Cancel";
    [ObservableProperty] private Progress<(int, int, string?)>? _progress;

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
                else if (typeof(IProgress<(int, int, string?)>).IsAssignableFrom(pType))
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

            // Run the result in a separate thread to not affect UI
            var result = await Task.Run(() => _loadingDelegate.DynamicInvoke(finalArgs)).ConfigureAwait(false);
            bool cancellationDone;
            
            switch (result)
            {
                case Task<bool> boolTask:
                {
                    var success = await boolTask;
                    cancellationDone = _cts.IsCancellationRequested;
                    return success && !cancellationDone;
                }
                case Task task:
                    await task;
                    cancellationDone = _cts.IsCancellationRequested;
                    return !cancellationDone;
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

    private void OnProgressChanged(object? sender, (int, int, string?) e)
    {
        ProgressPercentageText = $"{e.Item1}/{e.Item2} ";
        ProgressMax = e.Item2;
        ProgressValue = e.Item1;
        Status = e.Item3;
    }

    [RelayCommand]
    public void Cancel() => _cts.Cancel();

    /// <summary>
    /// Set up the dialog with the following parameters:
    /// <list type="number">
    ///     <item><description><see cref="string"/> Title (optional)</description></item>
    ///     <item><description><see cref="string"/> Status (optional)</description></item>
    ///     <item><description><see cref="Delegate"/> loadingFunc</description></item>
    ///     <item><description><see cref="object"/>[] args (Optional)</description></item>
    /// </list>
    /// </summary>
    /// <param name="args">The positional arguments as defined in the summary.</param>
    protected override void Setup(params object?[]? args)
    {
        // Throw if null, since there are two required arguments afterward
        ArgumentNullException.ThrowIfNull(args);
        // If there are optional arguments in the beginning, increment this value by the amount of optional parameters
        const int delegateHandlingOffset = 2;
        // Reset state
        _hasAlreadyInitiated = false;
        _cts = new CancellationTokenSource();
        Progress = null;
        ProgressPercentageText = null;
        ProgressValue = 0;
        ProgressMax = 1;

        // Get arguments
        _loadingDelegate = GetValueOrException<Delegate>(args, delegateHandlingOffset);
        
        // The rest of the arguments are for the delegate
        if (args is { Length: > delegateHandlingOffset + 1 })
        {
            _providedArgs = new object?[args.Length - (delegateHandlingOffset + 1)];
            Array.Copy(args, 1, _providedArgs, 0, args.Length - (delegateHandlingOffset + 1));
        }
        else
        {
            _providedArgs = [];
        }

        // Retrieve or Cache Method Parameters
        var parameters = MethodCache.GetOrAdd(_loadingDelegate.Method, m => m.GetParameters());

        // Dynamic UI State Detection
        AllowCancellation = parameters.Any(p => p.ParameterType == typeof(CancellationToken) || p.ParameterType == typeof(CancellationToken?));
        HideProgressBar = !parameters.Any(p => typeof(IProgress<(int, int, string?)>).IsAssignableFrom(p.ParameterType));
        
        if (!HideProgressBar) // If there's progress bar, there's progress instance
            Progress = new Progress<(int, int, string?)>();
        
        // Update UI Elements
        Title = TryGetValue(args, 0, out string? text) ? text : "Loading...";
        Status = TryGetValue(args, 1, out text) ? text : "Loading...";
    }
}
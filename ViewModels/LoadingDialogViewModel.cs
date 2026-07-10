using System.Collections.Concurrent;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GottaManagePlus.Interfaces;
using GottaManagePlus.Models;
using GottaManagePlus.Models.DialogManagement;
using GottaManagePlus.Models.ModManagement;
using GottaManagePlus.Models.UI;
using Serilog;

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

    // Observable properties
    [ObservableProperty]
    public partial string Title { get; set; } = "Loading...";

    [ObservableProperty]
    public partial string? Status { get; set; } = "Loading...";
    
    [ObservableProperty]
    public partial string ProgressTextFormat { get; set; } = "{1:0}%";

    [ObservableProperty]
    public partial long ProgressMax { get; set; } = 1;

    [ObservableProperty]
    public partial long ProgressValue { get; set; }

    [ObservableProperty]
    public partial string CancelText { get; set; } = "Cancel";

    [ObservableProperty] 
    public partial bool AllowCancellation { get; set; }
    
    [ObservableProperty] 
    public partial bool HideProgressBar { get; set; }
    
    [ObservableProperty]
    public partial Progress<ProgressReport>? Progress { get; set; }
    
    public object? Result { get; private set; }

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
                else if (typeof(IProgress<ProgressReport>).IsAssignableFrom(pType))
                {
                    finalArgs[i] = Progress;
                } 
                // Below them, just use these as arguments
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
            var result = await Task.Run(() =>
            {
                try
                {
                    return _loadingDelegate.DynamicInvoke(finalArgs);
                }
                catch(Exception e)
                {
                    Log.Logger.Error(e, "A loading task has thrown an exception!");
                    return false;
                }
            }).ConfigureAwait(false);
            bool cancellationDone;
            
            switch (result)
            {
                // Installation Result check
                case Task<ModInstallationResult> installTask:
                    var installResult = await installTask;
                    Result = installResult;
                    cancellationDone = _cts.IsCancellationRequested;
                    return !cancellationDone;
                
                // Check for the boolean result
                case Task<bool> boolTask:
                {
                    var success = await boolTask;
                    cancellationDone = _cts.IsCancellationRequested;
                    return success && !cancellationDone;
                }
                
                // Check for a default task
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

    private void OnProgressChanged(object? sender, ProgressReport e)
    {
        ProgressMax = e.TasksTotal;
        ProgressValue = e.TasksCompleted;
        Status = e.CurrentStatus;
        HideProgressBar = !e.HasTaskProgression;
        ProgressTextFormat = e.UsePercentage ? "{1:0}%" : "{0}/{3}";
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
        
       

       

        

        if (!HideProgressBar) // If there's progress bar, there's progress instance
            Progress = new Progress<ProgressReport>();

        // Update UI Elements
        Title = TryGetValue(args, 0, out string? text) ? text : "Loading...";
        Status = TryGetValue(args, 1, out text) ? text : "Loading...";
    }

    protected override async Task<object?> OnShow(DialogContext? context)
    {
        if (context is not LoadingDialogContext loadingContext) return null;
        
        // Reset state
        _hasAlreadyInitiated = false;
        Result = null;
        _cts = new CancellationTokenSource();
        Progress = null;
        ProgressValue = 0;
        ProgressMax = 1;
        
        // Prepare delegate's parameters
        // The rest of the arguments are for the delegate
        var args = loadingContext.DelegateArgs;
        if (args is { Length: > 0 })
        {
            _providedArgs = new object?[args.Length];
            for (var i = 0; i < _providedArgs.Length; i++)
                _providedArgs[i] = args[i];
        }
        else
        {
            _providedArgs = [];
        }
        
        // Retrieve or Cache Method Parameters
        _loadingDelegate = loadingContext.LoadingDelegate;
        var parameters = MethodCache.GetOrAdd(_loadingDelegate.Method, m => m.GetParameters());
        
        // Dynamic UI State Detection
        AllowCancellation = parameters.Any(p => p.ParameterType == typeof(CancellationToken) || p.ParameterType == typeof(CancellationToken?));
        HideProgressBar = !parameters.Any(p => typeof(IProgress<ProgressReport>).IsAssignableFrom(p.ParameterType));
    }
}
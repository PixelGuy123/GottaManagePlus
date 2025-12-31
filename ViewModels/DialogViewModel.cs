using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading.Tasks;
using Avalonia;

namespace GottaManagePlus.ViewModels;

public abstract partial class DialogViewModel : ViewModelBase
{
    // To display app version if required (static to be run once)
    private static readonly Version ConstAppVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version("0.0.0.0"),
        ConstAvaloniaVersion = typeof(AvaloniaObject).Assembly.GetName().Version ?? new Version("0.0.0.0");

    [ObservableProperty] 
    private Version _appVersion = ConstAppVersion,
                             _avaloniaVersion = ConstAvaloniaVersion;
    [ObservableProperty]
    private bool _isDialogOpen;

    private TaskCompletionSource _closeTask = new();
    private bool _isDialogPrepared;

    public async Task WaitAsync() =>
        await _closeTask.Task;
    

    public void Show()
    {
        if (!_isDialogPrepared)
            throw new InvalidOperationException("Dialog is not prepared to show!");
        if (_closeTask.Task.IsCompleted)
            _closeTask = new TaskCompletionSource();
        
        IsDialogOpen = true;
    }

    protected void Close()
    {
        IsDialogOpen = false;
        _isDialogPrepared = false;

        _closeTask.TrySetResult();
    }

    /// <summary>
    /// A method that must be called before displaying a dialog to prepare it.
    /// </summary>
    /// <param name="args">The arguments to set up this dialog.</param>
    /// <returns>The instance of itself.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public void Prepare(params object?[]? args)
    {
        if (_isDialogPrepared)
            throw new InvalidOperationException("Dialog is already prepared.");
        
        // Setup abstraction
        Setup(args);
        
        // Dialog is prepared by default
        _isDialogPrepared = true;
    }
    
    // Protected methods
    protected abstract void Setup(params object?[]? args);
    // Protected helper methods
    protected static bool TryGetValue<T>(object?[] args, int index, [NotNullWhen(true)] out T? val)
    {
        if (index >= 0 && args.Length > index && args[index] is T t)
        {
            val = t;
            return true;
        }
        val = default;
        return false;
    }

    protected static T GetValueOrException<T>(object?[]? args, int index)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (index < 0 || args.Length <= index)
            throw new IndexOutOfRangeException($"Argument ({index}) missing.");
        var arg = args[index];
        if (arg is T t)
            return t;
        
        throw new ArgumentException($"Expected value for argument ({index}) is {typeof(T).Name}; received {arg?.GetType().Name}");
    }
}
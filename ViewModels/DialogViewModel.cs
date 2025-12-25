using System;
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

    public async Task WaitAsync()
    {
        await _closeTask.Task;
    }

    public void Show()
    {
        if (_closeTask.Task.IsCompleted)
            _closeTask = new TaskCompletionSource();
        
        IsDialogOpen = true;
    }

    public void Close()
    {
        IsDialogOpen = false;

        _closeTask.TrySetResult();
    }
}
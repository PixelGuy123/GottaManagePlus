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

    protected TaskCompletionSource CloseTask = new();

    public async Task WaitAsync()
    {
        await CloseTask.Task;
    }

    public void Show()
    {
        if (CloseTask.Task.IsCompleted)
            CloseTask = new TaskCompletionSource();
        
        IsDialogOpen = true;
    }

    public void Close()
    {
        IsDialogOpen = false;

        CloseTask.TrySetResult();
    }
}
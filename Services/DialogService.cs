using GottaManagePlus.Interfaces;
using GottaManagePlus.Models.DialogManagement;
using GottaManagePlus.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace GottaManagePlus.Services;

public sealed class DialogService(IServiceProvider serviceProvider)
{
    // Main Service Provider for Dialogs
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    
    // Use a simple Stack with a lock for thread safety
    private readonly Stack<DialogViewModel> _dialogStack = new();
    private readonly Lock _stackLock = new();
    
    private IDialogProvider? _dialogProvider;

    public void RegisterProvider(IDialogProvider provider)
    {
        lock (_stackLock)
        {
            _dialogProvider = provider;
        }
    }

    public async Task<object?> ShowDialog<TDialogViewModel>(DialogContext? context) // context is placeholder
        where TDialogViewModel : DialogViewModel
    {
        if (_dialogProvider == null)
            throw new InvalidOperationException("DialogProvider has not been registered yet.");
        
        // Get the dialog
        var dialogViewModel = _serviceProvider.GetRequiredService<TDialogViewModel>();

        // Atomically push the dialog and set the provider
        lock (_stackLock)
        {
            _dialogStack.Push(dialogViewModel);
            _dialogProvider.Dialog = dialogViewModel;
        }

        try
        {
            return await dialogViewModel.Show(context);
        }
        catch
        {
            return false;
        }
        finally
        {
            // Atomically pop and restore the previous dialog
            lock (_stackLock)
            {
                // Ensure we pop the correct dialog (it should be on top)
                if (_dialogStack.Count > 0 && _dialogStack.Peek() == dialogViewModel)
                {
                    _dialogStack.Pop();
                    _dialogProvider.Dialog = _dialogStack.Count > 0 ? _dialogStack.Peek() : null;
                }
            }
        }
    }
}
using System.Collections.Concurrent;
using GottaManagePlus.Interfaces;
using GottaManagePlus.ViewModels;

namespace GottaManagePlus.Services;

public sealed class DialogService
{
    // Cache for view models (unchanged)
    private readonly ConcurrentDictionary<Type, DialogViewModel> _dialogCache = new();
    
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

    public TDialogViewModel GetDialog<TDialogViewModel>()
        where TDialogViewModel : DialogViewModel, new()
    {
        var tDialogType = typeof(TDialogViewModel);
        if (_dialogCache.TryGetValue(tDialogType, out var dialogViewModel))
            return (TDialogViewModel)dialogViewModel;

        var tDialog = Activator.CreateInstance<TDialogViewModel>();
        _dialogCache.TryAdd(tDialogType, tDialog);
        return tDialog;
    }

    public async Task<bool> ShowDialog<TDialogViewModel>(
        TDialogViewModel dialogViewModel,
        Func<TDialogViewModel, Task<bool>>? onShowAction = null,
        CancellationToken cancellationToken = default)
        where TDialogViewModel : DialogViewModel
    {
        if (_dialogProvider == null)
            throw new InvalidOperationException("DialogProvider has not been registered yet.");

        // Atomically push the dialog and set the provider
        lock (_stackLock)
        {
            _dialogStack.Push(dialogViewModel);
            _dialogProvider.Dialog = dialogViewModel;
        }

        try
        {
            var result = true;
            // Show the dialog (this will set IsDialogOpen and reset the TCS if needed)
            dialogViewModel.Show();

            // Invoke optional async callback (e.g., for data loading) – but we still await close
            if (onShowAction != null)
                result = await onShowAction(dialogViewModel).ConfigureAwait(false);

            // Wait for the dialog to be closed (or canceled)
            await using (cancellationToken.Register(dialogViewModel.Close))
            {
                await dialogViewModel.WaitAsync().ConfigureAwait(false);
            }

            return result;
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
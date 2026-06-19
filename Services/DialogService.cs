using System.Collections.Concurrent;
using GottaManagePlus.Interfaces;
using GottaManagePlus.ViewModels;

namespace GottaManagePlus.Services;

public sealed class DialogService
{
    // Dictionary for caching view models
    // Thread-safe dictionary
    private readonly ConcurrentDictionary<Type, DialogViewModel> _dialogCache = [];
    
    private IDialogProvider? _dialogProvider;

    public void RegisterProvider(IDialogProvider provider) => _dialogProvider = provider;

    // Basic pooling system
    public TDialogViewModel GetDialog<TDialogViewModel>()
        where TDialogViewModel : DialogViewModel, new()
    {
        // Get type stored
        var tDialogType = typeof(TDialogViewModel);
        
        // Attempt to get dialog through Type
        if (_dialogCache.TryGetValue(tDialogType, out var dialogViewModel)) 
            return (TDialogViewModel)dialogViewModel;
        
        // Expects to have a default parameterless constructor
        var tDialog = (TDialogViewModel?)Activator.CreateInstance(tDialogType);
        if (tDialog == null)
            throw new NullReferenceException($"{tDialogType.Name} could not be instantiated.");
        
        _dialogCache.TryAdd(tDialogType, tDialog);
        return tDialog;
    }
    
    public async Task<bool> ShowDialog<TDialogViewModel>(TDialogViewModel dialogViewModel, Func<TDialogViewModel, Task<bool>>? onShowAction = null) 
        where TDialogViewModel : DialogViewModel
    {
        if (_dialogProvider == null)
            throw new InvalidOperationException("DialogProvider has not been registered yet.");
        
        // Open up dialog and assign it
        _dialogProvider.Dialog = dialogViewModel;
        dialogViewModel.Show();
        
        // If there's an action on show, use it
        if (onShowAction != null)
            return await onShowAction(dialogViewModel);
        
        // Wait for dialog to close
        await dialogViewModel.WaitAsync();
        return true;
    }
}
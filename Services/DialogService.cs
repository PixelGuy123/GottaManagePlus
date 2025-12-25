using System;
using System.Threading.Tasks;
using GottaManagePlus.Interfaces;
using GottaManagePlus.ViewModels;

namespace GottaManagePlus.Services;

public class DialogService
{
    private IDialogProvider? _dialogProvider;

    public void RegisterProvider(IDialogProvider provider)
    {
        _dialogProvider = provider;
    }
    
    public async Task ShowDialog<TDialogViewModel>(TDialogViewModel dialogViewModel) 
        where TDialogViewModel : DialogViewModel
    {
        if (_dialogProvider == null)
            throw new InvalidOperationException("DialogProvider has not been registered yet.");
        
        // Open up dialog and assign it
        _dialogProvider.Dialog = dialogViewModel;
        dialogViewModel.Show();
        
        // Wait for dialog to close
        await dialogViewModel.WaitAsync();
    }

    public async Task<bool> ShowLoadingDialog(LoadingDialogViewModel loadViewModel)
    {
        if (_dialogProvider == null)
            throw new InvalidOperationException("DialogProvider has not been registered yet.");
        
        // Open up dialog and assign it
        _dialogProvider.Dialog = loadViewModel;
        loadViewModel.Show();
        
        // Wait for dialog to close after loading
        return await loadViewModel.StartTask();
    }
}
using System.Threading.Tasks;
using GottaManagePlus.Interfaces;
using GottaManagePlus.ViewModels;

namespace GottaManagePlus.Services;

public class DialogService
{
    public async Task ShowDialog<THost, TDialogViewModel>(THost host, TDialogViewModel dialogViewModel) 
        where TDialogViewModel : DialogViewModel
        where THost : IDialogProvider
    {
        // Open up dialog and assign it
        host.Dialog = dialogViewModel;
        dialogViewModel.Show();
        
        // Wait for dialog to close
        await dialogViewModel.WaitAsync();
    }
}
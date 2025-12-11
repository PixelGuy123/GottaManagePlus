using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GottaManagePlus.ViewModels;

public partial class ConfirmDialogViewModel : DialogViewModel
{
    [ObservableProperty]
    private string _title = "Confirm?";
    [ObservableProperty]
    private string _message = "Are you sure?";
    [ObservableProperty]
    private string _confirmText = "Confirm";
    [ObservableProperty]
    private string _cancelText = "Cancel";
    
    [ObservableProperty] 
    private bool _confirmed;
    
    [RelayCommand]
    public void Confirm()
    {
        Confirmed = true;
        Close();
    }
    
    [RelayCommand]
    public void Cancel()
    {
        Confirmed = false;
        Close();
    }
}
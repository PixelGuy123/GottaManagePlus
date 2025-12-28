using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GottaManagePlus.ViewModels;

public partial class CreateProfileDialogViewModel : DialogViewModel
{
    [ObservableProperty]
    private string _title = "Creating a new profile...", _cancelText = "Cancel", _createText = "Create Profile";
    
    [ObservableProperty] 
    private bool _confirmed, _canCreateProfile = true;
    
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
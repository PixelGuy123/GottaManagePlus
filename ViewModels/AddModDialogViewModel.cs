using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GottaManagePlus.ViewModels;

public partial class AddModDialogViewModel : DialogViewModel
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private bool _canAddMod;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAddMod))]
    private string? _modImportPath; // The path to the mod
    [ObservableProperty]
    private string _title = "Add new Mod";
    [ObservableProperty]
    private string _confirmText = "Add";
    [ObservableProperty]
    private string _cancelText = "Cancel";
    
    [ObservableProperty] 
    private bool _addModConfirmed;
    
    [RelayCommand]
    public void Confirm()
    {
        AddModConfirmed = true;
        Close();
    }
    
    [RelayCommand]
    public void Cancel()
    {
        AddModConfirmed = false;
        Close();
    }
    protected override void Setup(params object?[]? args) { }
}
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GottaManagePlus.ViewModels;

public partial class AddModDialogViewModel : DialogViewModel
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    public partial bool CanAddMod { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAddMod))]
    public partial string? ModImportPath { get; set; }

    [ObservableProperty]
    public partial string Title { get; set; } = "Add new Mod";

    [ObservableProperty]
    public partial string ConfirmText { get; set; } = "Add";

    [ObservableProperty]
    public partial string CancelText { get; set; } = "Cancel";

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
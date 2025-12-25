using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GottaManagePlus.ViewModels;

public partial class ConfirmDialogViewModel(bool confirmBtnOnly = false, bool centerAlignment = false) : DialogViewModel
{
    // Designer mode
    public ConfirmDialogViewModel() : this(false) { }
    // Only confirm button
    public bool OnlyConfirmButton { get; } = confirmBtnOnly;

    // Good for some cases where formatted text looks ugly
    public TextAlignment DescriptionAlignment { get; } = 
        centerAlignment ? TextAlignment.Center : TextAlignment.Left;
    
    [ObservableProperty]
    private string _title = "Confirm?";
    [ObservableProperty]
    private string _message = "Are you sure?";
    [ObservableProperty]
    private string _confirmText = confirmBtnOnly ? "Ok" : "Confirm";
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
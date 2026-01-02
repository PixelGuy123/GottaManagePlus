using Avalonia.Media;
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
    private bool _onlyConfirmButton;
    [ObservableProperty] 
    private TextAlignment _descriptionAlignment = TextAlignment.Center;
    
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

    /// <summary>
    /// Set up the dialog with the following parameters:
    /// <list type="number">
    ///     <item><description><see cref="bool"/> OnlyConfirmButton (Optional)</description></item>
    ///     <item><description><see cref="string"/> Title (Optional)</description></item>
    ///     <item><description><see cref="string"/> Message (Optional)</description></item>
    ///     <item><description><see cref="string"/> Confirm Text (Optional)</description></item>
    ///     <item><description><see cref="string"/> Cancel Text (Optional)</description></item>
    ///     <item><description><see cref="TextAlignment"/> DescriptionAlignment (Optional)</description></item>
    /// </list>
    /// </summary>
    /// <param name="args">The positional arguments as defined in the summary.</param>
    protected override void Setup(params object?[]? args)
    {
        // Reset this to false
        Confirmed = false;
        
        // Get the optional parameters ready
        if (args == null) return;
        
        // OnlyConfirmButton
        OnlyConfirmButton = TryGetValue(args, 0, out bool? isOkDialog) && isOkDialog.Value;
        // Title
        if (TryGetValue(args, 1, out string? text))
            Title = text;
        // Message
        if (TryGetValue(args, 2, out text))
            Message = text;
        // Confirm Text
        if (TryGetValue(args, 3, out text))
            ConfirmText = text;
        else
            ConfirmText = isOkDialog.GetValueOrDefault() ? "Ok" : "Confirm";
        // Cancel Text
        if (TryGetValue(args, 4, out text))
            CancelText = text;
        // Text Alignment
        if (TryGetValue(args, 5, out TextAlignment? textAlignment))
            DescriptionAlignment = textAlignment.Value;
    }
}
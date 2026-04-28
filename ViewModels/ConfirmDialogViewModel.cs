using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GottaManagePlus.ViewModels;

public partial class ConfirmDialogViewModel : DialogViewModel
{

    [ObservableProperty]
    public partial string Title { get; set; } = "Confirm?";

    [ObservableProperty]
    public partial string Message { get; set; } = "Are you sure?";

    [ObservableProperty]
    public partial string ConfirmText { get; set; } = "Confirm";

    [ObservableProperty]
    public partial string CancelText { get; set; } = "Cancel";

    [ObservableProperty]
    public partial bool OnlyConfirmButton { get; set; }

    [ObservableProperty]
    public partial TextAlignment DescriptionAlignment { get; set; } = TextAlignment.Center;

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
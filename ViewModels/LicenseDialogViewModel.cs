using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GottaManagePlus.Models.UI;
using GottaManagePlus.Services.ExplorerServices;

namespace GottaManagePlus.ViewModels;

public partial class LicenseDialogViewModel : DialogViewModel
{
    private FileLauncher _fileLauncher = null!;
    public LicenseDialogViewModel()
    {
        if (!Design.IsDesignMode) return;
    
        // Set defaults for the dialog
        Title = "Design Preview";
        Message = "This is how th";
    }
    
    [ObservableProperty]
    public partial string Title { get; set; } = "Confirm?";

    [ObservableProperty]
    public partial string Message { get; set; } = "Are you sure?";

    [ObservableProperty]
    public partial string ConfirmText { get; set; } = "Confirm";

    [ObservableProperty]
    public partial bool Confirmed { get; set; }

    [RelayCommand]
    public void Confirm()
    {
        Confirmed = true;
        Close();
    }

    [RelayCommand]
    public async Task OpenLicense()
    {
        if (!await _fileLauncher.TryLaunchFileInfo(new FileInfo(Constants.LicensePath)))
        {
            
        }
    }

    /// <summary>
    /// Set up the dialog with the following parameters:
    /// <list type="number">
    ///     <item><description><see cref="FileLauncher"/> FileLauncher </description></item>
    ///     <item><description><see cref="string"/> Title (Optional)</description></item>
    ///     <item><description><see cref="string"/> Message (Optional)</description></item>
    ///     <item><description><see cref="string"/> Confirm Text (Optional)</description></item>
    /// </list>
    /// </summary>
    /// <param name="args">The positional arguments as defined in the summary.</param>
    protected override void Setup(params object?[]? args)
    {
        // Reset this to false
        Confirmed = false;

        // File Launcher
        _fileLauncher = GetValueOrException<FileLauncher>(args, 0);
        
        // Get the optional parameters ready
        if (args == null) return;
        
        // Title
        if (TryGetValue(args, 1, out string? text))
            Title = text;
        
        // Message
        if (TryGetValue(args, 2, out text))
            Message = text;
        
        // Confirm Text
        if (TryGetValue(args, 3, out text))
            ConfirmText = text;
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GottaManagePlus.Interfaces;
using GottaManagePlus.Models;
using GottaManagePlus.Services;

namespace GottaManagePlus.ViewModels;

public partial class PreviewProfileDialogViewModel : DialogViewModel
{
    // Private members
    private IFilesService _filesService = null!;
    private DialogService _dialogService = null!;
    
    // Observables
    [ObservableProperty]
    private ProfileItem _profile = null!;
    [ObservableProperty]
    private string _closeText = "Close", _deleteText = "Delete", _exportText = "Export as package";

    [ObservableProperty]
    private bool _allowProfileDeletion;
    
    // Public getters
    public bool ShouldDeleteProfile { get; private set; }
    public bool ShouldExportProfile { get; private set; }
    public DialogViewModel? SubDialogView { get; private set; }

    public PreviewProfileDialogViewModel()
    {
        if (Design.IsDesignMode)
        {
            Profile = new ProfileItem(0, "Some cool long profile name that has never been used before and shall never be used with this length, Holy moly, this is huge!");
            AllowProfileDeletion = true;
        }
    }
    
    // Commands
    [RelayCommand]
    public void CloseAndDeleteProfile()
    {
        ShouldDeleteProfile = true;
        Close();
    }

    [RelayCommand]
    public void CloseWithoutChanges() => Close();

    [RelayCommand]
    public void OpenProfilePath()
    {
        const string profileFixSuggestion = "Try reloading the profiles list.";
        if (!File.Exists(Profile.FullOsPath))
        {
            var dialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
            dialog.Prepare(true, Constants.FailDialog, $"The path to the profile is somehow invalid!\n{profileFixSuggestion}");
            SubDialogView = dialog;
            
            Debug.WriteLine("Failed to open profile path due to invalid path.", Constants.DebugError);
            Debug.WriteLine(Profile.FullOsPath, Constants.DebugError);
            Close();
            return;
        }
        
        if (!_filesService.OpenFileInfo(new FileInfo(Profile.FullOsPath)))
        {
            var dialog = _dialogService.GetDialog<ConfirmDialogViewModel>();
            dialog.Prepare(true, Constants.FailDialog, $"Failed to open the path to the profile due to an unknown error!\n{profileFixSuggestion}");
            SubDialogView = dialog;
            
            Debug.WriteLine("Failed to open profile path due to unknown error.", Constants.DebugError);
            Close();
        }
    }
    
    // Internal/Public methods
    public void ResetState()
    {
        SubDialogView = null;
        ShouldExportProfile = false;
        ShouldDeleteProfile = false;
        IsDialogOpen = false;
    }

    [RelayCommand]
    public void ExportProfile()
    {
        ShouldExportProfile = true;
        Close();
    }

    /// <summary>
    /// Set up the dialog with the following parameters:
    /// <list type="number">
    ///     <item><description><see cref="ProfileItem"/> profile</description></item>
    ///     <item><description><see cref="bool"/> allowProfileDeletion</description></item>
    ///     <item><description><see cref="IFilesService"/> filesService</description></item>
    ///     <item><description><see cref="DialogService"/> dialogService</description></item>
    /// </list>
    /// </summary>
    /// <param name="args">The positional arguments as defined in the summary.</param>
    protected override void Setup(params object?[]? args)
    {
        ResetState();
        
        Profile = GetValueOrException<ProfileItem>(args, 0);
        AllowProfileDeletion = GetValueOrException<bool>(args, 1);
        _filesService = GetValueOrException<IFilesService>(args, 2);
        _dialogService = GetValueOrException<DialogService>(args, 3);
    }
}

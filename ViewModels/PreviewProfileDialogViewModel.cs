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

namespace GottaManagePlus.ViewModels;

public partial class PreviewProfileDialogViewModel(ProfileItem profile, bool allowProfileDeletion, IFilesService filesService) : DialogViewModel
{
    // Private members
    private readonly IFilesService _filesService = filesService;
    
    // For previewer
    [Obsolete("Used for designer. Use PreviewProfileDialogViewModel(ProfileItem profile) instead.", true)]
    public PreviewProfileDialogViewModel() : this(
        new ProfileItem(0, "Some cool long profile name that has never been used before and shall never be used with this length, Holy moly, this is huge!"),
        true, null!)
    {
        if (!Design.IsDesignMode)
            throw new NotSupportedException("Design mode is not enabled!");
    }
    
    // Observables
    [ObservableProperty]
    private ProfileItem _profile = profile;
    [ObservableProperty]
    private string _closeText = "Close", _deleteText = "Delete", _exportText = "Export as package";

    public bool AllowProfileDeletion { get; } = allowProfileDeletion;
    
    // Public getters
    public bool ShouldDeleteProfile { get; private set; }
    public bool ShouldExportProfile { get; private set; }
    public DialogViewModel? SubDialogView { get; private set; }
    
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
    public async Task OpenProfilePath()
    {
        const string profileFixSuggestion = "Try reloading the profiles list.";
        var path = Path.GetDirectoryName(Profile.FullOsPath);
        if (!Directory.Exists(path))
        {
            SubDialogView = new ConfirmDialogViewModel(true)
            {
              Title = Constants.FailDialog,
              Message = $"The path to the profile is somehow invalid!\n{profileFixSuggestion}"
            };
            Debug.WriteLine("Failed to open profile path due to invalid path.", Constants.DebugError);
            Debug.WriteLine(path, Constants.DebugError);
            Close();
            return;
        }
        
        if (!await _filesService.OpenDirectoryInfo(new DirectoryInfo(path)))
        {
            SubDialogView = new ConfirmDialogViewModel(true)
            {
                Title = Constants.FailDialog,
                Message = $"Failed to open the path to the profile due to an unknown error!\n{profileFixSuggestion}"
            };
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
}
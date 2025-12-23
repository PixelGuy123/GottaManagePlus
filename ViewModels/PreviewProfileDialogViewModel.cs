using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
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
    private string _closeText = "Close", _deleteText = "Delete";

    public bool AllowProfileDeletion { get; } = allowProfileDeletion;
    
    // Public getters
    public bool ShouldDeleteProfile { get; set; }
    
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
        if (string.IsNullOrEmpty(Profile.FullOsPath) || !Directory.Exists(Profile.FullOsPath))
        {
            // TODO: Display fail dialog due to invalid path
            Debug.WriteLine("Failed to open profile path due to invalid path.", Constants.DebugError);
            return;
        }

        if (!await _filesService.OpenDirectoryInfo(new DirectoryInfo(Profile.FullOsPath)))
        {
            // TODO: Display fail dialog due to unknown error
            Debug.WriteLine("Failed to open profile path due to unknown error.", Constants.DebugError);
        }
    }


    // Internal/Public methods
    public void ResetState()
    {
        ShouldDeleteProfile = false;
        IsDialogOpen = false;
    }
}
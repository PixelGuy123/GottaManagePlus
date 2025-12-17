using System;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GottaManagePlus.Models;

namespace GottaManagePlus.ViewModels;

public partial class PreviewProfileDialogViewModel(ProfileItem profile) : DialogViewModel
{
    // For previewer
    [Obsolete("Used for designer. Use PreviewProfileDialogViewModel(ProfileItem profile) instead.", true)]
    public PreviewProfileDialogViewModel() : this(
        new ProfileItem(0, "Some cool long profile name that has never been used before and shall never be used with this length, Holy moly, this is huge!")
        )
    {
        if (!Design.IsDesignMode)
            throw new NotSupportedException("Design mode is not enabled!");
    }
    
    // Observables
    [ObservableProperty]
    private ProfileItem _profile = profile;
    [ObservableProperty]
    private string _closeText = "Close", _deleteText = "Delete";
 
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
    public void OpenProfilePath()
    {
        
    }
    
    // Internal/Public methods
    public void ResetState()
    {
        ShouldDeleteProfile = false;
        IsDialogOpen = false;
    }
}
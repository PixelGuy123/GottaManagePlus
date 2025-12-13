using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GottaManagePlus.ViewModels;

public partial class AppInfoDialogViewModel : DialogViewModel
{
    [ObservableProperty]
    private string _title = "About";
    [ObservableProperty]
    private string _confirmText = "Ok";
    [ObservableProperty] 
    private Uri _kofiLink = new("https://ko-fi.com/pixelguy"),
        _discordLink = new("https://discord.gg/GneZs7n2Gp");
    
    [RelayCommand]
    public void Ok()
    {
        Close();
    }
}
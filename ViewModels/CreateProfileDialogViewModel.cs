/*
This file is part of GottaManagePlus (https://github.com/PixelGuy123/GottaManagePlus)

    Copyright (C) 2026 PixelGuy123

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.

*/

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GottaManagePlus.Models.DialogManagement;
using GottaManagePlus.Services.ExplorerServices;

namespace GottaManagePlus.ViewModels;

public partial class CreateProfileDialogViewModel : DialogViewModel
{
    // Constants
    private static readonly char[] InvalidPathChars = [.. Path.GetInvalidFileNameChars(), '_'];
    private const int ProfileNameLengthLimit = 48;
    
    // Observables
    [ObservableProperty]
    public partial string? Title { get; set; }
    
    [ObservableProperty]
    public partial string? CancelText { get; set; }
    
    [ObservableProperty]
    public partial string? CreateText { get; set; }
    
    [ObservableProperty]
    public partial string ErrorText { get; set; } = $"This name must be unique (not duplicate), below or equal to {ProfileNameLengthLimit} characters and shall not contain one of these invalid symbols ({string.Join(", ", InvalidPathChars
        .Where(c => !char.IsControl(c))
        .Select(c => $"'{c}'"))}).";
        
    [ObservableProperty]
    public partial bool Confirmed { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    public partial bool CanCreateProfile { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreateProfile))]
    public partial int SelectedTabIndex { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreateProfile))]
    public partial string? ProfileName { get; set; }
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreateProfile))]
    public partial string? CloneProfileName { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreateProfile))]
    public partial string? ProfileImportPath { get; set; }

    [ObservableProperty]
    public partial int? ProfileIndexToClone { get; set; } = 0;

    [ObservableProperty]
    public partial ObservableCollection<string> ExistingProfiles { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreateProfile))]
    public partial string? SelectedExistingProfile { get; set; }

    // private members
    private FilePicker _filesService = null!;
    
    // Constructors
    public CreateProfileDialogViewModel()
    {
        if (Design.IsDesignMode)
            ExistingProfiles = ["Default", "Only packs", "Gaming"];
    }

    partial void OnSelectedTabIndexChanged(int value) => Validate();
    partial void OnProfileNameChanged(string? value) => Validate();
    partial void OnCloneProfileNameChanged(string? value) => Validate();
    
    partial void OnProfileImportPathChanged(string? value) => Validate();

    partial void OnSelectedExistingProfileChanged(string? value)
    {
        // If the value exists, add _Clone to it
        if (!string.IsNullOrEmpty(value))
        {
            var newName = $"{value}-Clone";
            while (ExistingProfiles.Contains(newName))
                newName += "-Clone";
            CloneProfileName = newName;
            ProfileIndexToClone = ExistingProfiles.IndexOf(value);
        }
        Validate();
    }

    [RelayCommand]
    public async Task BrowseFile()
    {
        // Try to get the file
        var file = await _filesService.OpenSingleFileAsync(
            "Import Profile",
                filterChoices: Constants.ExportedProfileFilter
        );
        
        // Set as import path
        ProfileImportPath = file?.TryGetLocalPath();
    }

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

    private void Validate()
    {
        CanCreateProfile = SelectedTabIndex switch
        {
            0 => // New
                IsValidFilename(ProfileName) && !ExistingProfiles.Contains(ProfileName),
            1 => // Clone
                IsValidFilename(CloneProfileName) && !ExistingProfiles.Contains(CloneProfileName) && !string.IsNullOrEmpty(SelectedExistingProfile),
            2 => // Import
                !string.IsNullOrWhiteSpace(ProfileImportPath) && File.Exists(ProfileImportPath) &&
                ProfileImportPath.EndsWith(Constants.ExportedProfileExtension),
            _ => false
        };
    }

    private static bool IsValidFilename([NotNullWhen(true)] string? name) // Annotation to make compiler happy
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > ProfileNameLengthLimit)
            return false;
        
        return name.IndexOfAny(InvalidPathChars) < 0;
    }
    
    // Setup method
    /// <summary>
    /// Set up the dialog with the following parameters:
    /// <list type="number">
    ///     <item><description><see cref="FilePicker"/> filesService</description></item>
    ///     <item><description><see cref="IEnumerable{string}"/> Existing Profiles</description></item>
    /// </list>
    /// </summary>
    /// <param name="args">The positional arguments as defined in the summary.</param>
    protected override void Setup(params object?[]? args)
    {
        Confirmed = false;
        
        // _filesService
        _filesService = GetValueOrException<FilePicker>(args, 0);
        // _existingProfiles
        ExistingProfiles = new ObservableCollection<string>(GetValueOrException<IEnumerable<string>>(args, 1));
        
        // Update profile name
        ProfileName = "My Profile";
        
        var counter = 2;
        while (ExistingProfiles.Contains(ProfileName))
            ProfileName = $"My Profile #{counter++}";
        
        Validate();
    }
}
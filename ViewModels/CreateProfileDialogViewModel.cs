using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GottaManagePlus.Interfaces;

namespace GottaManagePlus.ViewModels;

public partial class CreateProfileDialogViewModel : DialogViewModel
{
    // Observables
    [ObservableProperty]
    private string _title = "Creating a new profile...", _cancelText = "Cancel", _createText = "Create Profile",
        _errorText = $"This name already exists or contains one of the invalid characters ({string.Join(", ", Path.GetInvalidFileNameChars()
            .Where(c => !char.IsControl(c))
            .Select(c => $"'{c}'"))}).";
    
    [ObservableProperty]
    private bool _confirmed;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private bool _canCreateProfile;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreateProfile))]
    private int _selectedTabIndex; // This field will help know what mode the user is on for the data provided

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreateProfile))]
    private string? _profileName, _cloneProfileName;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreateProfile))]
    private string? _profileImportPath;

    [ObservableProperty]
    private int? _profileIndexToClone = 0;

    [ObservableProperty] private ObservableCollection<string> _existingProfiles = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreateProfile))]
    private string? _selectedExistingProfile;
    
    // private members
    private readonly IFilesService _filesService = null!;
    
    // Constructors
    public CreateProfileDialogViewModel()
    {
        if (Design.IsDesignMode)
            ExistingProfiles = ["Default", "Only packs", "Gaming"];
        else
            throw new InvalidOperationException("Design Mode is disabled!");
    }

    public CreateProfileDialogViewModel(IFilesService filesService, params IEnumerable<string> profiles)
    {
        _filesService = filesService;
        ExistingProfiles = new ObservableCollection<string>(profiles);
        ProfileName = "My Profile";
        
        var counter = 2;
        while (ExistingProfiles.Contains(ProfileName))
            ProfileName = $"My Profile #{counter++}";
        
        Validate();
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
            var newName = $"{value}_Clone";
            while (ExistingProfiles.Contains(newName))
                newName += "_Clone";
            CloneProfileName = newName;
            ProfileIndexToClone = ExistingProfiles.IndexOf(value);
        }
        Validate();
    }

    [RelayCommand]
    public async Task BrowseFile()
    {
        // Try to get the file
        var file = await _filesService.OpenFileAsync(
            "Import Profile",
                fileChoices: Constants.ExportedProfileFilter
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
        if (string.IsNullOrWhiteSpace(name))
            return false;
        
        return name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
    }
}
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GottaManagePlus.Models;
using GottaManagePlus.Services;

namespace GottaManagePlus.ViewModels;

public partial class ProfilesViewModel : ViewModelBase
{
    private readonly DialogService _dialogService = null!;
    private readonly List<ProfileItem> _allProfiles = null!;
    private ProfileItem? _lastSelectedItem;
    
    // Public readonly properties
    public IReadOnlyList<ProfileItem> ProfilesList => _allProfiles;
    
    // Observable Properties
    [ObservableProperty]
    private ObservableCollection<ProfileItem> _observableProfiles = null!;
    [ObservableProperty]
    private ProfileItem? _currentProfileItem;
    [ObservableProperty]
    private string? _text;
    
    // From the generator. https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/observableproperty
    partial void OnCurrentProfileItemChanged(ProfileItem? value) => UpdateProfilesList(value); 
    
    [RelayCommand]
    public void ResetSearch() => Text = null;

    [RelayCommand]
    public async Task OpenProfileMetadata(int id) => await OpenProfileMetaDataAndHandleActions(id);

    // Previewer Constructor
    public ProfilesViewModel()
    {
        if (!Design.IsDesignMode) return;
        
        // Initialize Data
        _allProfiles =
        [
            new ProfileItem(0, "Profile 1"),
            new ProfileItem(1, "Profile 2"),
            new ProfileItem(2, "Profile 3"),
            new ProfileItem(3, "The Ultimate ModPack"),
            new ProfileItem(4, "Biggest Modpack ever"),
            new ProfileItem(5, "Funny mod pack with the longest trollest biggest hugest name that you've ever seen.")
        ];
        
        // Initialize collections
        ObservableProfiles = new ObservableCollection<ProfileItem>(_allProfiles);
    }
    
    // DI Constructor
    public ProfilesViewModel(DialogService dialogService)
    {
        // Initialize Data
        _allProfiles =
        [
            new ProfileItem(0, "Profile 1"),
            new ProfileItem(1, "Profile 2"),
            new ProfileItem(2, "Profile 3"),
            new ProfileItem(3, "The Ultimate ModPack"),
            new ProfileItem(4, "Biggest Modpack ever"),
            new ProfileItem(5, "Funny mod pack with the longest trollest biggest hugest name that you've ever seen.")
        ];
        
        // Initialize collections
        ObservableProfiles = new ObservableCollection<ProfileItem>(_allProfiles);
        
        // Dialog Service
        _dialogService = dialogService;
    }
    
    // Private methods
    private void UpdateProfilesList(ProfileItem? highlightedItem)
    {
        // If item is not null, insert it at the top
        if (highlightedItem != null)
        {
            // Fix last selected item if needed
            int index;
            if (_lastSelectedItem != null)
            {
                index = _allProfiles.IndexOf(_lastSelectedItem);
                if (index != -1)
                {
                    ObservableProfiles.RemoveAt(0); // Presumably where the selected item is located at
                    ObservableProfiles.Insert(index, _lastSelectedItem);
                }
            }

            _lastSelectedItem = highlightedItem;
            index = ObservableProfiles.IndexOf(highlightedItem);
            if (index != -1)
            {
                ObservableProfiles.RemoveAt(index);
                ObservableProfiles.Insert(0, highlightedItem);
                return;
            }
        }
        // If highlighted item is null or not found, just reset the whole list
        _lastSelectedItem = null;
        ObservableProfiles.Clear();
        foreach (var item in _allProfiles)
            ObservableProfiles.Add(item);
    }

    private void ResetListVisibleConfigurations() // Basically reset the observable list
    {
        UpdateProfilesList(null);
        ResetSearch();
    }

    private async Task OpenProfileMetaDataAndHandleActions(int id)
    {
        var index = _allProfiles.FindIndex(item => item.Id == id);
        if (index == -1) // If the item doesn't exist, skip
        {
            // TODO: Open dialog for not succeeding opening it
            return;
        }

        var profileViewer = new PreviewProfileDialogViewModel(_allProfiles[index]);
        while (true)
        {
            profileViewer.ResetState(); // Reset dialog state (IsDialogOpen, for example)
            await _dialogService.ShowDialog(profileViewer);
            
            if (profileViewer.ShouldDeleteProfile)
            {
                // If deleting the item works, then we don't need to loop back
                if (await DeleteProfileItemUiAsync(id)) break;

                continue; // Loop back, since that wasn't a normal close
            }

            break;
        }
    }
    
    private async Task<bool> DeleteProfileItemUiAsync(int index) // Delete asynchronously the items
    {
        var confirmViewModel = new ConfirmDialogViewModel()
        {
            Title = $"Delete {_allProfiles[index].ProfileName}?",
            Message = "Are you sure you want to delete this profile?",
            ConfirmText = "Yes",
            CancelText = "No"
        };

        await _dialogService.ShowDialog(confirmViewModel);
        
        // Do not if not accepted
        if (!confirmViewModel.Confirmed)
            return false;
        
        _allProfiles.RemoveAt(index);
        ResetListVisibleConfigurations();
        return true;
    }
}
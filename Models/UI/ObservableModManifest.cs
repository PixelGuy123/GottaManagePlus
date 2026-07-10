using CommunityToolkit.Mvvm.ComponentModel;
using GottaManagePlus.Models.ModManagement;

namespace GottaManagePlus.Models.UI;

public class ObservableModManifest(ModManifest manifest) : ObservableObject
{
    // ModManifest.Metadata.Activated
    public bool IsActivated 
    {
        get => InnerManifest.Metadata.Activated;
        set
        {
            InnerManifest.Metadata.Activated = value;
            OnPropertyChanged(); // Notify UI to refresh
        }
    }

    // For binding other ModManifest properties in XAML
    public ModManifest InnerManifest { get; } = manifest;
    
    // For implicit conversion
    public static implicit operator ObservableModManifest(ModManifest manifest) => new(manifest);
}
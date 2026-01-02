using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GottaManagePlus.Models;

public partial class ModItem(int id, string modName) : ItemWithPath(id)
{
    // Observables
    [ObservableProperty]
    private string _modName = modName;
    [ObservableProperty] 
    private ModMetadata? _metaData;
    [ObservableProperty] 
    private Bitmap? _thumbnail;
    [ObservableProperty] 
    private bool _supportsCurrentVersion;
    
    // Public methods
    public override string ToString() => $"Mod: {ModName}";
}
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using GottaManagePlus.Models.JsonContext;
using GottaManagePlus.Services;

namespace GottaManagePlus.Models;

public class SaveState : INotifyPropertyChanged // An "observable" AppSettings;
                                                // Manually implements INotify due to source generators issue (https://github.com/AvaloniaUI/Avalonia/discussions/18593)
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        TypeInfoResolver = SaveStateContext.Default
    };
    private SettingsService _settingsService = null!;
    private string _savedState = null!; // Json
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    
    // Static Constructor
    public static SaveState InitializeState(SettingsService settingsService)
    {
        // Create State
        var state = new SaveState
        {
            _settingsService = settingsService
        };
        
        // Update State Fields
        var currentSettings = settingsService.CurrentSettings;
        state.GameExecutablePath = currentSettings.BaldiPlusExecutablePath;
        state.NumberOfModsPerRow = currentSettings.NumberOfRowsPerMod;
        
        // Serialize last saved state
        state._savedState = JsonSerializer.Serialize(state, DefaultOptions);
        return state;
    }
    
    // *** Observables ***
    // Observable Private Members
    private string? _gameExecutablePath;
    private int _numberOfModsPerRow = 6;
    
    // Observable Properties
    public string? GameExecutablePath
    {
        get => _gameExecutablePath;
        set { _gameExecutablePath = value; OnPropertyChanged(nameof(GameExecutablePath)); OnPropertyChanged(nameof(HasChanged)); }
    }
    
    public int NumberOfModsPerRow
    {
        get => _numberOfModsPerRow;
        set { _numberOfModsPerRow = value; OnPropertyChanged(nameof(NumberOfModsPerRow)); OnPropertyChanged(nameof(HasChanged)); }
    }

    [JsonIgnore]
    public SaveState LastSavedState {
        get
        {
            if (string.IsNullOrEmpty(_savedState)) // If current state is empty, just give back itself
                return this;
            
            var deserializedState = JsonSerializer.Deserialize<SaveState>(_savedState, DefaultOptions);
            if (deserializedState == null) 
                return this;
            deserializedState._settingsService = _settingsService;
            deserializedState._savedState = _savedState;
            return deserializedState;
        }
    }
    [JsonIgnore]
    public bool HasChanged => !Design.IsDesignMode && _savedState != JsonSerializer.Serialize(this, DefaultOptions); // Tell whether the state has changed

    public void UpdateSavedState()
    {
        _savedState = JsonSerializer.Serialize(this, DefaultOptions);
        OnPropertyChanged(nameof(HasChanged));
    }
}
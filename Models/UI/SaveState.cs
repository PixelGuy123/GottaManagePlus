using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Controls;
using GottaManagePlus.Services;
using SaveStateContext = GottaManagePlus.Models.SourceGenerators.SaveStateContext;

namespace GottaManagePlus.Models.UI;

public class SaveState : INotifyPropertyChanged // An "observable" AppSettings;
                                                // Manually implements INotify due to source generators issue (https://github.com/AvaloniaUI/Avalonia/discussions/18593)
{
    private string _savedState = null!; // Json
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    
    // Static Constructor
    public static SaveState InitializeState(SettingsService settingsService)
    {
        // Create State
        var state = new SaveState();
        
        // Update State Fields
        var currentSettings = settingsService.CurrentSettings;
        state.GameExecutablePath = currentSettings.BaldiPlusExecutablePath;
        state.NumberOfModsPerRow = currentSettings.NumberOfRowsPerMod;
        state.Theme = currentSettings.Theme;
        
        // Serialize last saved state
        state._savedState = JsonSerializer.Serialize(state, SaveStateContext.Default.SaveState);
        return state;
    }
    
    // *** Observables ***
    // Observable Private Members
    private string? _gameExecutablePath;
    private int _numberOfModsPerRow = 6;
    private string _theme = "Dark";
    
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

    public string Theme
    {
        get => _theme;
        set { _theme = value; OnPropertyChanged(nameof(Theme)); OnPropertyChanged(nameof(HasChanged)); }
    }

    [JsonIgnore]
    public SaveState LastSavedState {
        get
        {
            if (string.IsNullOrEmpty(_savedState)) // If current state is empty, just give back itself
                return this;
            
            var deserializedState = JsonSerializer.Deserialize<SaveState>(_savedState, SaveStateContext.Default.SaveState);
            if (deserializedState == null) 
                return this;
            deserializedState._savedState = _savedState;
            return deserializedState;
        }
    }
    [JsonIgnore]
    public bool HasChanged => !Design.IsDesignMode && _savedState != JsonSerializer.Serialize(this, SaveStateContext.Default.SaveState); // Tell whether the state has changed

    public void UpdateSavedState()
    {
        _savedState = JsonSerializer.Serialize(this, SaveStateContext.Default.SaveState);
        OnPropertyChanged(nameof(HasChanged));
    }
}
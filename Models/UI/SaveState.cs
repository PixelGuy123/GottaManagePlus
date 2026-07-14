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

using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Controls;
using GottaManagePlus.Services;
using SaveStateContext = GottaManagePlus.Utils.SourceGenerators.SaveStateContext;

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
        state.CancelOnSecurityIssues = currentSettings.CancelOnSecurityIssues;
        
        // Serialize last saved state
        state._savedState = JsonSerializer.Serialize(state, SaveStateContext.Default.SaveState);
        return state;
    }
    
    // *** Observables ***
    // Observable Private Members

    // Observable Properties
    public string? GameExecutablePath
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(GameExecutablePath));
            OnPropertyChanged(nameof(HasChanged));
        }
    }

    public int NumberOfModsPerRow
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(NumberOfModsPerRow));
            OnPropertyChanged(nameof(HasChanged));
        }
    } = 6;

    public string Theme
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(Theme));
            OnPropertyChanged(nameof(HasChanged));
        }
    } = "Dark";

    public bool CancelOnSecurityIssues
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(CancelOnSecurityIssues));
            OnPropertyChanged(nameof(HasChanged));
        }
    } = false;

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
    public bool HasChangesThatRequiresRestart(AppSettings.ReadonlyAppSettings appSettings) => Theme != appSettings.Theme;

    public void UpdateSavedState()
    {
        _savedState = JsonSerializer.Serialize(this, SaveStateContext.Default.SaveState);
        OnPropertyChanged(nameof(HasChanged));
    }
}
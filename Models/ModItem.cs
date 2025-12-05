using System;

namespace GottaManagePlus.Models;

public class ModItem
{
    private const int MaxModNameCutLength = 44;
    private string _modName, _cutModName;

    public string FullModName { get => _modName; set => UpdateModName(value); }
    public string ModName { get => _cutModName; set => UpdateModName(value); }
    public string ModVersionString => ModVersion.ToString();
    
    public Version ModVersion { get; set; } = new(0, 0, 0); // Default is basically 0
    
    public ModItem()
    {
        _modName = "Mod #";
        _cutModName = _modName;
    }
    // Private/Internal Methods
    private void UpdateModName(string newName)
    {
        _modName = newName;
        _cutModName = 
            newName.Length > MaxModNameCutLength ? 
                string.Concat(_modName.AsSpan(0, MaxModNameCutLength - 3), "...") : // If the name is longer, cut it and insert '...'
                newName;
    }
    
    // Public Methods
    public void UpdateCutModName() => UpdateModName(_modName); // Just reform the string
}
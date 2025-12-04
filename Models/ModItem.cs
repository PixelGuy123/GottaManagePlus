using System;

namespace GottaManagePlus.Models;

public class ModItem
{
    private const int MaxModNameCutLength = 40;
    private string _modName, _cutModName;

    public string FullModName { get => _modName; set => UpdateModName(value); }
    public string ModName { get => _cutModName; set => UpdateModName(value); }
    
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
}
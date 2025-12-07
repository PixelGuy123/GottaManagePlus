using System;
using System.Diagnostics;

namespace GottaManagePlus.Models;

public class ModItem
{
    private const int MaxModNameCutLength = 44;
    private string _modName, _cutModName;
    /// <summary>
    /// The full mod's name. 
    /// </summary>
    public string FullModName { get => _modName; set => UpdateModName(value); }
    /// <summary>
    /// The sanitized ModName, automatically cut to fit the screen.
    /// </summary>
    public string ModName { get => _cutModName; set => UpdateModName(value); }
    public string ModVersionString => ModVersion.ToString();

    /// <summary>
    /// The mod's <see cref="Version"/>.
    /// </summary>
    public Version ModVersion { get; set; } = new(0, 0, 0); // Default is basically 0
    
    public ModItem()
    {
        _modName = "Mod #";
        _cutModName = _modName;
    }
    
    // Private/Internal Methods
    private void UpdateModName(string newName, double newWidth = -1)
    {
        _modName = newName;
        var length = MaxModNameCutLength;
        length += Math.Max(0, (int)newWidth - 720);
        _cutModName = newName.Length > length ? 
                string.Concat(_modName.AsSpan(0, length - 3), "...") : // If the name is longer, cut it and insert '...'
                newName;
    }
    
    // Public Methods
    /// <summary>
    /// Forces the class to update the mod name displayed.
    /// </summary>
    /// <param name="newWidth">Refers to the new application's width.</param>
    public void UpdateCutModName(double newWidth) => UpdateModName(_modName, newWidth); // Just reform the string
}
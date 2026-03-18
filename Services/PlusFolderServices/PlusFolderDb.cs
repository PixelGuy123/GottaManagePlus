using GottaManagePlus.Models;

namespace GottaManagePlus.Services.PlusFolderServices;

/// <summary>
/// A database that stores some data that can be retrieved from the game's folder.
/// </summary>
public sealed class PlusFolderDb
{
    /// <summary>
    /// The BALDI_Data folder that is found inside the game's folder.
    /// </summary>
    public string BaldiDataFolder { get; set; } = string.Empty;
    /// <summary>
    /// The version of the game.
    /// </summary>
    public WrappedGameVersion GameVersion { get; set; } = new("0.0.0.0");
    /// <summary>
    /// The root path to the game's folder (based on the executable's location).
    /// </summary>
    public string RootPath { get; set; } = string.Empty;
}
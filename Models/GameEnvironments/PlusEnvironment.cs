using GottaManagePlus.Interfaces.GameEnvironment;

namespace GottaManagePlus.Models.GameEnvironments;

/// <summary>
/// A representation that stores some data that can be retrieved from the game's folder.
/// </summary>
public sealed record PlusEnvironment(string RootPath, string UnityDataFolder, WrappedGameVersion GameVersion) : IGameEnvironment;
using GottaManagePlus.Interfaces.GameEnvironment;

namespace GottaManagePlus.Models.GameEnvironments;

/// <summary>
/// A representation that stores some data that can be retrieved from the game's folder.
/// </summary>
public sealed record PlusEnvironment(OsPath RootPath, OsPath UnityDataFolder, OsPath ExecutablePath, WrappedGameVersion GameVersion) : IGameEnvironment;
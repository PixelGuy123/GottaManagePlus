using GottaManagePlus.Models;
using GottaManagePlus.Models.GameEnvironments;
using GottaManagePlus.Models.System;

namespace GottaManagePlus.Interfaces.GameEnvironment;

public interface IGameEnvironment
{
    OsPath RootPath { get; }
    OsPath ExecutablePath { get; }
    OsPath UnityDataFolder { get; }
    WrappedGameVersion GameVersion { get; }
}
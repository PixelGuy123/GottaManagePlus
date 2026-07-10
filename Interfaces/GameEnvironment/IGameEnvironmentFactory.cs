using GottaManagePlus.Models;
using GottaManagePlus.Models.System;

namespace GottaManagePlus.Interfaces.GameEnvironment;

public interface IGameEnvironmentFactory
{
    IGameEnvironment? CreateEnvironment(OsPath executablePath);
}
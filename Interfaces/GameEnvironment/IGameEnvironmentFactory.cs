using GottaManagePlus.Models;

namespace GottaManagePlus.Interfaces.GameEnvironment;

public interface IGameEnvironmentFactory
{
    IGameEnvironment? CreateEnvironment(OsPath executablePath);
}
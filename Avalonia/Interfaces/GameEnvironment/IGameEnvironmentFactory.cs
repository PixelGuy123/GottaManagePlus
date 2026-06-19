namespace GottaManagePlus.Interfaces.GameEnvironment;

public interface IGameEnvironmentFactory
{
    IGameEnvironment? CreateEnvironment(string executablePath);
}
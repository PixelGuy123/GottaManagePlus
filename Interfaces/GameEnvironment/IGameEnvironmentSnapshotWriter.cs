namespace GottaManagePlus.Interfaces.GameEnvironment;

public interface IGameEnvironmentSnapshotWriter
{
    Task WriteSnapshotAsync(string rootPath, string writeToPath);
}
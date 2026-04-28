using System.Threading;
using System.Threading.Tasks;

namespace GottaManagePlus.Interfaces.GameEnvironment;

public interface IGameEnvironmentSnapshotWriter
{
    Task WriteSnapshotAsync(string rootPath, string writeToPath);
}
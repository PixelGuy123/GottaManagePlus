using System.IO;

namespace GottaManagePlus.Utils;

public static class UnixUtils
{
    public static bool CheckIfUnixFileIsExecutable(FileInfo fileInfo)
    {
        var mode = fileInfo.UnixFileMode;
        
        // Check if it has an executable permission
        return mode.HasFlag(UnixFileMode.UserExecute) || 
               mode.HasFlag(UnixFileMode.GroupExecute) || 
               mode.HasFlag(UnixFileMode.OtherExecute);
    }

    public static bool CheckIfUnixFileIsExecutable(string filePath) => CheckIfUnixFileIsExecutable(new FileInfo(filePath));
}
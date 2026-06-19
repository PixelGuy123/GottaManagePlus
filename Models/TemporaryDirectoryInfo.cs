using Serilog;

namespace GottaManagePlus.Models;

/// <summary>
/// A representation of a temporary <see cref="DirectoryInfo"/> with a disposable pattern.
/// </summary>
public class TemporaryDirectoryInfo(DirectoryInfo directoryInfo, ILogger? logger) : IDisposable
{
    // ---- Private ----
    private readonly ILogger? _logger = logger;
    
    // ---- Public ----
    /// <summary>
    /// The active directory info.
    /// </summary>
    public DirectoryInfo DirectoryInfo { get; } = directoryInfo;
    
    public void Dispose()
    {
        try
        {
            _logger?.Information("Cleaning up temporary directory '{dir}'.", DirectoryInfo.FullName);
            if (DirectoryInfo.Exists) DirectoryInfo.Delete();
        }
        catch { /* Suppress */ }
    }
}
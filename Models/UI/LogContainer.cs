using System.Collections.ObjectModel;

namespace GottaManagePlus.Models.UI;

/// <summary>
/// Represents the type of a log entry.
/// </summary>
public enum LogType
{
    Warning,
    Error,
    Information
}

/// <summary>
/// Manages a collection of log entries with their types.
/// Logs are stored in a single collection and filtered by type when needed.
/// </summary>
public class LogContainer
{
    /// <summary>
    /// The collection of all log entries.
    /// </summary>
    public ObservableCollection<LogTreeNode> Logs { get; } = new();

    /// <summary>
    /// Adds a log entry to the container.
    /// </summary>
    /// <param name="type">The type of log (Warning, Error, or Information).</param>
    /// <param name="title">The title of the log entry.</param>
    /// <param name="message">Optional detailed message.</param>
    public void AddLog(LogType type, string title, string? message = null)
    {
        Logs.Add(new LogTreeNode(type, title, message));
    }

    /// <summary>
    /// Adds a warning log entry.
    /// </summary>
    /// <param name="title">The title of the warning.</param>
    /// <param name="message">Optional detailed message.</param>
    public void AddWarning(string title, string? message = null)
    {
        AddLog(LogType.Warning, title, message);
    }

    /// <summary>
    /// Adds an error log entry.
    /// </summary>
    /// <param name="title">The title of the error.</param>
    /// <param name="message">Optional detailed message.</param>
    public void AddError(string title, string? message = null)
    {
        AddLog(LogType.Error, title, message);
    }

    /// <summary>
    /// Adds an information log entry.
    /// </summary>
    /// <param name="title">The title of the information.</param>
    /// <param name="message">Optional detailed message.</param>
    public void AddInformation(string title, string? message = null)
    {
        AddLog(LogType.Information, title, message);
    }

    /// <summary>
    /// Clears all logs from the container.
    /// </summary>
    public void Clear()
    {
        Logs.Clear();
    }

    /// <summary>
    /// Gets the total count of all logs.
    /// </summary>
    public int TotalCount => Logs.Count;

    /// <summary>
    /// Checks if the container has any logs at all.
    /// </summary>
    public bool HasLogs => TotalCount > 0;

    /// <summary>
    /// Gets the count of logs for a specific type.
    /// </summary>
    /// <param name="type">The log type to count.</param>
    /// <returns>The number of logs of the specified type.</returns>
    public int GetCountByType(LogType type)
    {
        return Logs.Count(log => log.LogType == type);
    }

    /// <summary>
    /// Gets all logs of a specific type.
    /// </summary>
    /// <param name="type">The log type to filter by.</param>
    /// <returns>An enumerable of log nodes matching the specified type.</returns>
    public IEnumerable<LogTreeNode> GetLogsByType(LogType type)
    {
        return Logs.Where(log => log.LogType == type);
    }
}

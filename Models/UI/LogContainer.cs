--- Models/UI/LogContainer.cs (原始)


+++ Models/UI/LogContainer.cs (修改后)
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
/// Represents a node in the log hierarchy tree.
/// Can be a category header, a log entry, or a log message.
/// The node generates its own display text based on its type by overriding ToString().
/// </summary>
public partial class LogTreeNode
{
    /// <summary>
    /// The type of this log node (used for log entries only).
    /// </summary>
    public LogType? LogType { get; }

    /// <summary>
    /// The title of the log (used for log entries only).
    /// </summary>
    public string? Title { get; }

    /// <summary>
    /// The message of the log (used for log entries only).
    /// </summary>
    public string? Message { get; }

    /// <summary>
    /// Child nodes (for hierarchical display).
    /// </summary>
    public ObservableCollection<LogTreeNode> Children { get; } = new();

    /// <summary>
    /// Creates a category header node.
    /// </summary>
    /// <param name="type">The log type for this category.</param>
    /// <param name="count">The number of logs in this category.</param>
    public LogTreeNode(LogType type, int count)
    {
        LogType = type;
        Count = count;
    }

    /// <summary>
    /// Creates a log entry node.
    /// </summary>
    /// <param name="type">The type of log (Warning, Error, or Information).</param>
    /// <param name="title">The title of the log entry.</param>
    /// <param name="message">Optional detailed message.</param>
    public LogTreeNode(LogType type, string title, string? message = null)
    {
        LogType = type;
        Title = title;
        Message = message;
    }

    /// <summary>
    /// Creates a message sub-node (child of a log entry).
    /// </summary>
    /// <param name="message">The message text to display.</param>
    public LogTreeNode(string message)
    {
        Message = message;
    }

    /// <summary>
    /// The count of logs in a category (used for category headers only).
    /// </summary>
    public int Count { get; } = 0;

    /// <summary>
    /// Generates the display text for this node based on its type.
    /// </summary>
    /// <returns>The formatted display text.</returns>
    public override string ToString()
    {
        // Category header node
        if (LogType.HasValue && Count > 0 && string.IsNullOrEmpty(Title))
        {
            var typeName = LogType.Value switch
            {
                LogType.Warning => "Warnings",
                LogType.Error => "Errors",
                LogType.Information => "Information",
                _ => "Unknown"
            };
            return $"({Count}) {typeName}";
        }

        // Log entry node (has Title)
        if (!string.IsNullOrEmpty(Title))
        {
            var prefix = LogType.Value switch
            {
                LogType.Warning => "(WARNING)",
                LogType.Error => "(ERROR)",
                LogType.Information => "(INFO)",
                _ => "(UNKNOWN)"
            };
            return $"{prefix} {Title}";
        }

        // Message sub-node (just the message text)
        return Message ?? string.Empty;
    }
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

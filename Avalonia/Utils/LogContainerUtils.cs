using GottaManagePlus.Models.UI;

namespace GottaManagePlus.Utils;

/// <summary>
/// Static class filled with utilities for <see cref="LogContainer"/> objects.
/// </summary>
public static class LogContainerUtils
{
    /// <summary>
    /// Merges together all logs from other containers into a single <see cref="LogContainer"/> instance.
    /// </summary>
    /// <param name="logs">The other <see cref="LogContainer"/> instances to be merged.</param>
    /// <returns>A merged <see cref="LogContainer"/>.</returns>
    public static LogContainer MergeAll(this IEnumerable<LogContainer> logs)
    {
        var mergedLog = new LogContainer();
        foreach (var container in logs)
            foreach (var type in Enum.GetValues<LogType>())
                foreach (var log in container.GetLogsByType(type))
                    mergedLog.AddLog(type, log.Title ?? string.Empty, log.Message);
        return mergedLog;
    }

    /// <summary>
    /// Converts an <see cref="IEnumerable{string}"/> into a <see cref="LogContainer"/>.
    /// </summary>
    /// <param name="individualLogs">The logs to be processed.</param>
    /// <param name="title">The title of each log.</param>
    /// <param name="type">The type of the logs.</param>
    /// <returns>A new instance of <see cref="LogContainer"/>.</returns>
    public static LogContainer ToLogContainer(this IEnumerable<string> individualLogs, string title, LogType type)
    {
        var logContainer = new LogContainer();
        var counter = 1;
        foreach (var log in individualLogs)
            logContainer.AddLog(type, $"{title} ({counter++})", log);
        return logContainer;
    }
}
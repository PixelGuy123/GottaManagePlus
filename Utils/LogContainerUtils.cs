/*
This file is part of GottaManagePlus (https://github.com/PixelGuy123/GottaManagePlus)

    Copyright (C) 2026 PixelGuy123

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.

*/

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
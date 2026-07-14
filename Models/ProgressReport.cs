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

namespace GottaManagePlus.Models;

/// <summary>
/// A data structure that represents important data for the UI to render progress reports.
/// </summary>
/// <param name="numOfTasksCompleted">The amount of tasks to be completed.</param>
/// <param name="totalAmountOfTasks">The total amount of tasks.</param>
/// <param name="statusPrefix">The prefix that comes before the status to indicate a category.</param>
/// <param name="currentStatus">The current status of the progress.</param>
public readonly struct ProgressReport(
    long numOfTasksCompleted,
    long totalAmountOfTasks,
    string? statusPrefix = null,
    string? currentStatus = null,
    bool usePercentage = false)
{
    public ProgressReport(string statusPrefix, string currentStatus) : this(-1, -1, statusPrefix, currentStatus) { }
    public ProgressReport(string currentStatus) : this(-1, -1, null, currentStatus) { }
    
    public readonly long TasksCompleted = numOfTasksCompleted;
    public readonly long TasksTotal = totalAmountOfTasks;
    public readonly string? CurrentStatus = statusPrefix == null ? currentStatus : $"{statusPrefix}: {currentStatus}";
    public readonly bool UsePercentage = usePercentage;
    public bool HasTaskProgression => TasksTotal > 0 && TasksCompleted <= TasksTotal;
}
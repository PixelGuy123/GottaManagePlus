namespace GottaManagePlus.Models;

/// <summary>
/// A data structure that represents important data for the UI to render progress reports.
/// </summary>
/// <param name="NumOfTasksCompleted">The amount of tasks to be completed.</param>
/// <param name="TotalAmountOfTasks">The total amount of tasks.</param>
/// <param name="StatusPrefix">The prefix that comes before the status to indicate a category.</param>
/// <param name="CurrentStatus">The current status of the progress.</param>
public struct ProgressReport(int NumOfTasksCompleted, int TotalAmountOfTasks, string? StatusPrefix = null, string? CurrentStatus = null);
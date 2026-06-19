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
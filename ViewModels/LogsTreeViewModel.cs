using System.Collections.ObjectModel;
using Avalonia.Controls.Models.TreeDataGrid;
using CommunityToolkit.Mvvm.ComponentModel;
using GottaManagePlus.Models.UI;

namespace GottaManagePlus.ViewModels;

/// <summary>
/// Represents a node in the log hierarchy tree.
/// Can be a category header, a log entry, or a log message.
/// The node generates its own display text based on its type by overriding ToString().
/// </summary>
public partial class LogTreeNode : ObservableObject
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
/// View model for the logs TreeDataGrid in ConfirmDialogView.
/// Transforms a LogContainer into a hierarchical tree structure.
/// This class is designed to be instantiated by the DI container through the ViewLocator pattern.
/// The actual LogContainer is passed via the Prepare method from ConfirmDialogViewModel.
/// </summary>
public partial class LogsTreeViewModel : ObservableObject
{
    /// <summary>
    /// The hierarchical tree data source for the TreeDataGrid.
    /// </summary>
    public HierarchicalTreeDataGridSource<LogTreeNode> Source { get; private set; }

    /// <summary>
    /// The root nodes of the tree (categories with logs).
    /// </summary>
    public ObservableCollection<LogTreeNode> RootNodes { get; } = new();

    /// <summary>
    /// Creates a new LogsTreeViewModel with an empty tree.
    /// Use Prepare() to populate the tree with a LogContainer.
    /// </summary>
    public LogsTreeViewModel()
    {
        // Initialize the hierarchical source with children selector
        Source = new HierarchicalTreeDataGridSource<LogTreeNode>(RootNodes)
        {
            Columns =
            {
                new HierarchicalExpanderColumn<LogTreeNode>(
                    new TextColumn<LogTreeNode, string>("", x => x.ToString()!),
                    x => x.Children),
            },
        };
    }

    /// <summary>
    /// Prepares the tree with the specified log container.
    /// Called by ConfirmDialogViewModel after construction.
    /// </summary>
    /// <param name="logContainer">The log container to visualize. If null or empty, the tree will be empty.</param>
    public void Prepare(LogContainer? logContainer)
    {
        RootNodes.Clear();
        
        if (logContainer != null && logContainer.HasLogs)
        {
            BuildTree(logContainer);
        }
    }

    /// <summary>
    /// Builds the hierarchical tree structure from the log container.
    /// </summary>
    /// <param name="logContainer">The log container to process.</param>
    private void BuildTree(LogContainer logContainer)
    {
        RootNodes.Clear();

        // Add Warnings category if it has logs
        var warningCount = logContainer.GetCountByType(LogType.Warning);
        if (warningCount > 0)
        {
            var warningsNode = new LogTreeNode(LogType.Warning, warningCount);
            foreach (var log in logContainer.GetLogsByType(LogType.Warning))
            {
                var logNode = new LogTreeNode(LogType.Warning, log.Title!, log.Message);
                
                // Add message as child if it exists and is not empty
                if (!string.IsNullOrWhiteSpace(log.Message))
                {
                    logNode.Children.Add(new LogTreeNode(log.Message));
                }
                
                warningsNode.Children.Add(logNode);
            }
            RootNodes.Add(warningsNode);
        }

        // Add Errors category if it has logs
        var errorCount = logContainer.GetCountByType(LogType.Error);
        if (errorCount > 0)
        {
            var errorsNode = new LogTreeNode(LogType.Error, errorCount);
            foreach (var log in logContainer.GetLogsByType(LogType.Error))
            {
                var logNode = new LogTreeNode(LogType.Error, log.Title!, log.Message);
                
                // Add message as child if it exists and is not empty
                if (!string.IsNullOrWhiteSpace(log.Message))
                {
                    logNode.Children.Add(new LogTreeNode(log.Message));
                }
                
                errorsNode.Children.Add(logNode);
            }
            RootNodes.Add(errorsNode);
        }

        // Add Information category if it has logs
        var infoCount = logContainer.GetCountByType(LogType.Information);
        if (infoCount > 0)
        {
            var infoNode = new LogTreeNode(LogType.Information, infoCount);
            foreach (var log in logContainer.GetLogsByType(LogType.Information))
            {
                var logNode = new LogTreeNode(LogType.Information, log.Title!, log.Message);
                
                // Add message as child if it exists and is not empty
                if (!string.IsNullOrWhiteSpace(log.Message))
                {
                    logNode.Children.Add(new LogTreeNode(log.Message));
                }
                
                infoNode.Children.Add(logNode);
            }
            RootNodes.Add(infoNode);
        }
    }
}

using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;

namespace GottaManagePlus.Models.UI;

/// <summary>
/// Container for the logs TreeDataGrid in ConfirmDialogView.
/// Transforms a LogContainer into a hierarchical tree structure.
/// This is a simple data container, not a view model.
/// </summary>
public class LogsTreeContainer
{
    /// <summary>
    /// The hierarchical tree data source for the TreeDataGrid.
    /// </summary>
    public HierarchicalTreeDataGridSource<LogTreeNode> Source { get; private set; }

    /// <summary>
    /// The root nodes of the tree (categories with logs).
    /// </summary>
    public ObservableCollection<LogTreeNode> RootNodes { get; } = [];

    /// <summary>
    /// Creates a new LogsTreeContainer with an empty tree.
    /// Use Prepare() to populate the tree with a LogContainer.
    /// </summary>
    public LogsTreeContainer()
    {
        // Initialize the hierarchical source with children selector
        Source = new HierarchicalTreeDataGridSource<LogTreeNode>(RootNodes)
        {
            Columns =
            {
                new HierarchicalExpanderColumn<LogTreeNode>(
                    new TextColumn<LogTreeNode, string>(string.Empty, x => x.ToString()),
                    x => x.Children)
            },
        };
        
        Source.ExpandAll(); // Expanded by default
    }

    /// <summary>
    /// Prepares the tree with the specified log container.
    /// Called by ConfirmDialogViewModel after construction.
    /// </summary>
    /// <param name="logContainer">The log container to visualize. If null or empty, the tree will be empty.</param>
    public void Prepare(LogContainer? logContainer)
    {
        RootNodes.Clear();

        if (logContainer is { HasLogs: true })
            BuildTree(logContainer);
    }

    /// <summary>
    /// Builds the hierarchical tree structure from the log container.
    /// </summary>
    /// <param name="logContainer">The log container to process.</param>
    private void BuildTree(LogContainer logContainer)
    {
        RootNodes.Clear();
    
        foreach (var logType in new[] { LogType.Warning, LogType.Error, LogType.Information })
        {
            var count = logContainer.GetCountByType(logType);
            if (count == 0) continue;
    
            var categoryNode = new LogTreeNode(logType, count);
    
            foreach (var log in logContainer.GetLogsByType(logType))
            {
                var logNode = new LogTreeNode(logType, log.Title!, log.Message);
    
                if (!string.IsNullOrWhiteSpace(log.Message))
                {
                    logNode.Children.Add(new LogTreeNode(log.Message));
                }
    
                categoryNode.Children.Add(logNode);
            }
    
            RootNodes.Add(categoryNode);
        }
    }
}

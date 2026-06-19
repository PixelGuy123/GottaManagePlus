namespace GottaManagePlus.Models.ModManagement;

public class ModInstallationResult(CancellationToken cancellationToken)
{
    // Public Getters
    public bool Success { get; set; }
    public ModManifest? Metadata { get; set; }
    public List<string> SecurityIssues { get; } = [];
    public bool HasSecurityIssues => SecurityIssues.Count != 0;
    public bool Cancelled => _cancellationToken.IsCancellationRequested;
    
    // Private Fields
    private readonly CancellationToken _cancellationToken = cancellationToken;
}
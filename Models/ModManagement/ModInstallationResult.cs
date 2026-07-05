namespace GottaManagePlus.Models.ModManagement;

public class ModInstallationResult
{
    // Public Getters
    public bool Success { get; set; }
    public ModManifest? Metadata { get; set; }
    public List<string> SecurityIssues { get; } = [];
    public bool HasSecurityIssues => SecurityIssues.Count != 0;
}
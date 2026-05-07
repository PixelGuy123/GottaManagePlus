namespace GottaManagePlus.Models.UI;

public class ModInstallationResult
{
    public bool Success { get; set; }
    public ModManifest? Metadata { get; set; }
    public List<string> SecurityIssues { get; } = [];
    public bool HasSecurityIssues => SecurityIssues.Count != 0;
}
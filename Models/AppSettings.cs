namespace GottaManagePlus.Models;

public class AppSettingsWrapper
{
    public required AppSettings AppSettings { get; init; }
}
public class AppSettings
{
    public required string BaldiPlusFilePath { get; set; }
    public required string BookmarkId { get; set; }
}
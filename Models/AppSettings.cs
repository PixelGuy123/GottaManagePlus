namespace GottaManagePlus.Models;

public class AppSettingsWrapper
{
    public required AppSettings AppSettings { get; init; }
}
public class AppSettings
{
    public required string BaldiPlusExecutablePath { get; set; }
    public required string CurrentProfileSet { get; set; }
}
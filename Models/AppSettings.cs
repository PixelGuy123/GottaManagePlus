namespace GottaManagePlus.Models;

/// <summary>
/// Core settings from the application.
/// </summary>
public class AppSettings
{
    public string BaldiPlusExecutablePath { get; set; } = string.Empty;
    public string CurrentProfileSet { get; set; } = string.Empty;
    public int NumberOfRowsPerMod { get; set; } = 4;
    public string Theme { get; set; } = "Dark";

    /// <summary>
    /// Readonly Variant of <see cref="AppSettings"/>.
    /// </summary>
    public record struct ReadonlyAppSettings(
        string BaldiPlusExecutablePath,
        string CurrentProfileSet,
        int NumberOfRowsPerMod,
        string Theme)
    {
        public ReadonlyAppSettings(AppSettings settings) : this(
            settings.BaldiPlusExecutablePath,
            settings.CurrentProfileSet, 
            settings.NumberOfRowsPerMod,
            settings.Theme) 
        { }
    };
}
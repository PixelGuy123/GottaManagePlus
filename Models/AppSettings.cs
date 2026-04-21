namespace GottaManagePlus.Models;

/// <summary>
/// Core settings from the application.
/// </summary>
public class AppSettings
{
    public string BaldiPlusExecutablePath { get; set; } = string.Empty;
    public string CurrentProfileSet { get; set; } = string.Empty;
    public int NumberOfRowsPerMod { get; set; } = 4;

    /// <summary>
    /// Readonly Variant of <see cref="AppSettings"/>.
    /// </summary>
    public record struct ReadonlyAppSettings(
        string BaldiPlusExecutablePath,
        string CurrentProfileSet,
        int NumberOfRowsPerMod)
    {
        public ReadonlyAppSettings(AppSettings settings) : this(
            settings.BaldiPlusExecutablePath,
            settings.CurrentProfileSet, 
            settings.NumberOfRowsPerMod) 
        { }
    };
}
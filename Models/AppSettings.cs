using System;

namespace GottaManagePlus.Models;

public class AppSettings
{
    public required string BaldiPlusExecutablePath { get; set; }
    public required string CurrentProfileSet { get; set; }
    public required int NumberOfRowsPerMod { get; set; }
}
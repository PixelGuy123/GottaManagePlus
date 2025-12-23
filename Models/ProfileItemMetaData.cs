namespace GottaManagePlus.Models;


public class ProfileItemMetaData
{
    public required string ProfileName { get; set; }
    public required string[] AllUsedDirectoryPaths { get; set; } // Basically all the *other* paths used by the profile that will be copied when the profile is created.
                                                                // From this array, the Plugins and Configs folder must be identified.
}
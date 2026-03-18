using GottaManagePlus.Interfaces;
using GottaManagePlus.Services.PlusFolderServices;

namespace GottaManagePlus.Services.ProfileServices;

public class ProfileStorage(PlusFolderBrowser folderBrowser)
{
    // ----- Private API -----
    public ProfileMemoryDb ProfileMemoryDb { get; } = new();
    private readonly PlusFolderBrowser _plusFolderBrowser = folderBrowser;
    
    // ----- Public API -----
    /// <summary>
    /// This method empties out the Profile Database to read all the profiles from the local storage again and insert back into the Db.
    /// </summary>
    public void LoadProfilesFromLocalIntoMemory()
    {
        // Get all files in Profiles folder
        
    }

    /// <summary>
    /// This method saves all profiles from memory into local storage, removing unused profiles in the process.
    /// </summary>
    public void SaveProfilesFromMemoryIntoLocal()
    {
        // TODO: Make the algorithm save profiles and the rest of the list that hasn't been touched, delete it
    }

    /// <summary>
    /// Attempts to read a profile and add it to the database.
    /// </summary>
    /// <param name="reader">The reader instance to be used to read the profile's data structure.</param>
    /// <param name="path">The path the reader will focus on.</param>
    public void ReadProfileFromLocal(IProfileReader reader, string path)
    {
        // TODO: Read locally a profile and attempt to add it to the database at the same time
    }
}
namespace GottaManagePlus.Services.GamebananaAPI;

/// <summary>
/// All section types that are useful for finding submissions.
/// </summary>
public enum SubmissionType
{
    MOD = 0,
    WIP = 1
}

/// <summary>
/// Class that contains a handful amount of extensions to manipulate the <see cref="SubmissionType"/> enum.
/// </summary>
public static class SubmissionType_Extensions
{
    public static string ToSection(this SubmissionType sub) => sub switch
    {
        SubmissionType.MOD => "Mod",
        SubmissionType.WIP => "Wip",
        _ => throw new System.ArgumentException("SubmissionType is invalid!")
    };
}

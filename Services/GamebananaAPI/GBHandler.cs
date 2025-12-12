using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace GottaManagePlus.Services.GamebananaAPI;

/// <summary>
/// This class will be responsable for handling all interactions with the GameBanana API, while also manipulating the retrieved <see cref="JsonObject"/> instances for collecting useful data.
/// </summary>
public static class GBHandler
{
    const string BASE_URL_PATH = "https://gamebanana.com/apiv11/{section}/{id}?_csvProperties=@gbprofile";
    // TODO: Add documentation and workflow
    public async static Task<JsonObject> GetSubmissionData(SubmissionType subType, int id)
    {
        throw new System.NotImplementedException();
    }
}
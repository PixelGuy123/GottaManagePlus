using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Serilog;

namespace GottaManagePlus.Services.APIServices;

public class GamebananaApiService(IHttpClientFactory httpClientFactory)
{
    private const string ClientName = "GameBanana";
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    // ----- PUBLIC API ------
    /// <summary>
    /// Attempts to retrieve the Gamebanana's submission data.
    /// </summary>
    /// <param name="subType">The submission type to look for.</param>
    /// <param name="id">The id of the submission.</param>
    /// <returns>A <see cref="JsonDocument"/> with the data from the request.</returns>
    /// <exception cref="HttpRequestException">Thrown if the request fails.</exception>
    public async Task<JsonDocument> GetSubmissionDataAsync(SubmissionType subType, int id)
    {
        // Try to request to GB.
        var response = await _httpClientFactory.CreateClient(ClientName)
            .GetAsync($"{SubmissionToString(subType)}/{id}?_csvProperties=@gbprofile");

        // If successful, get the document.
        if (response.IsSuccessStatusCode) return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        
        // Otherwise, throw an error.
        Log.Logger.Error("Failed to retrieve the data from {sub}/{id}", SubmissionToString(subType), id);
        throw new HttpRequestException($"API Error: {response.StatusCode}");
    }

    // ----- PRIVATE API -----
    public enum SubmissionType
    {
        Mod = 0,
        Wip = 1
    }

    private static string SubmissionToString(SubmissionType type) => type switch
    {
        SubmissionType.Mod => "Mod",
        SubmissionType.Wip => "Wip",
        _ => string.Empty
    };
}
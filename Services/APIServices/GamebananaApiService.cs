using System.Text.Json;
using Avalonia.Media.Imaging;
using GottaManagePlus.Models;
using GottaManagePlus.Models.UI;
using Serilog;

namespace GottaManagePlus.Services.APIServices;

public class GamebananaApiService(IHttpClientFactory httpClientFactory)
{
    private const string
        ClientName = "GameBanana",
        apiVersion = "apiv12"; // Current API Version
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    // ----- Public ------
    /// <summary>
    /// Attempts to retrieve the Gamebanana submission data.
    /// </summary>
    /// <param name="id">The id of the submission.</param>
    /// <returns>A <see cref="Result{T}"/> containing the <see cref="ModItem"/> if successful, or an error message if failed.</returns>
    public async Task<Result<ModItem>> GetSubmissionDataAsync(int id)
    {
        // Try to request to GB.
        // URL: https://gamebanana.com/apiv12/Mod/{ModID}/ProfilePage
        var response = await _httpClientFactory.CreateClient(ClientName)
            .GetAsync($"{apiVersion}/Mod/{id}/ProfilePage");

        // If successful, get the document.
        if (response.IsSuccessStatusCode) return Result<ModItem>.Success(ModItem.FromJson(JsonDocument.Parse(await response.Content.ReadAsStringAsync())));
        
        // Otherwise, return failure.
        Log.Logger.Error("Failed to retrieve the data from Mod/{id}", id);
        return Result<ModItem>.Failure($"API Error: {response.StatusCode}");
    }
    
    // TODO: Integrate a Result<T> pattern for safe exception handling of all Gamebanana API service calls.
    // Then, include a "Failed to Load" indicator to the ModSelectionDialogView.axaml file, located exactly where the loading indicator is.
    /// <summary>
    /// Attempts to search through Gamebanana API.
    /// </summary>
    /// <param name="page">The page to follow up.</param>
    /// <param name="searchTerm">The term to be used for filtering the search.</param>
    /// <returns>A <see cref="GameBananaIndex"/> with the data from the request.</returns>
    /// <exception cref="HttpRequestException">Thrown if the request fails.</exception>
    public async Task<Result<GameBananaIndex>> GetSubmissionListAsync(int page, string? searchTerm = null)
    {
        // Try to request to GB.
        var urlToUse = string.IsNullOrWhiteSpace(searchTerm)
            ? $"/Mod/Index?_nPerpage=15&_aFilters[Generic_Category]=4609&_nPage={page}"
            : $"/Mod/Index?_nPerpage=15&_aFilters[Generic_Category]=4609&_aFilters[Generic_Name]=contains,{searchTerm}&_nPage={page}";
        // URL: https://gamebanana.com/apiv12/Mod/Index?_nPerpage=15&_aFilters[Generic_Category]=4609&_nPage={page}&_aFilters[Generic_Name]=contains,{searchTerm}
        var response = await _httpClientFactory.CreateClient(ClientName)
            .GetAsync($"{apiVersion}{urlToUse}");

        // If successful, get the document.
        if (response.IsSuccessStatusCode) return Result<GameBananaIndex>.Success(GameBananaIndex.FromJson(JsonDocument.Parse(await response.Content.ReadAsStringAsync())));
        
        // Otherwise, return failure.
        Log.Logger.Error("Failed to retrieve the data from list ({page}).", page);
        return Result<GameBananaIndex>.Failure($"API Error: {response.StatusCode}");
    }

    /// <summary>
    /// Attempts to get an <see cref="IndexedFile"/> instance from a determined id.
    /// </summary>
    /// <param name="id">The id of the file.</param>
    /// <returns>A <see cref="Result{T}"/> containing the <see cref="IndexedFile"/> if successful, or an error message if failed.</returns>
    public async Task<Result<IndexedFile>> GetIndexedFileFromFileId(int id)
    {
        // Try to request to GB.
        // URL: https://gamebanana.com/apiv12/File/{id}
        var response = await _httpClientFactory.CreateClient(ClientName)
            .GetAsync($"{apiVersion}/File/{id}");

        // If successful, get the document.
        if (response.IsSuccessStatusCode) return Result<IndexedFile>.Success(IndexedFile.CreateOrGetIndexedFile(JsonDocument.Parse(await response.Content.ReadAsStringAsync())));
        
        // Otherwise, return failure.
        Log.Logger.Error("Failed to retrieve the data from file id ({id}).", id);
        return Result<IndexedFile>.Failure($"API Error: {response.StatusCode}");
    }

    /// <summary>
    /// Attempts to retrieve an image from Gamebanana.
    /// </summary>
    /// <param name="uri">The URL for the image (absolute or relative).</param>
    /// <param name="progress">For reporting download progress.</param>
    /// <param name="cancellationToken">For cancelling the bitmap load.</param>
    /// <returns>A <see cref="Result{T}"/> containing the <see cref="Bitmap"/> if successful, or an error message if failed.</returns>
    public async Task<Result<Bitmap>> GetImageAsync(
        string uri,
        IProgress<ProgressReport>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var httpClient = _httpClientFactory.CreateClient(ClientName);

        using var response = await httpClient.GetAsync(
            uri,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return Result<Bitmap>.Failure($"HTTP Error: {response.StatusCode}");
        }

        var contentLength = response.Content.Headers.ContentLength;
        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);

        var buffer = new byte[8192]; // 8KB chunks
        var totalBytesRead = 0L;
        using var memoryStream = new MemoryStream();

        int bytesRead;
        while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            memoryStream.Write(buffer, 0, bytesRead);
            totalBytesRead += bytesRead;

            cancellationToken.ThrowIfCancellationRequested();

            // Report progress if provider is set and content length is known
            if (progress == null || !contentLength.HasValue) continue;
            progress.Report(
                new ProgressReport(totalBytesRead, contentLength.Value * 100,
                    currentStatus: "Retrieving image from URL...", usePercentage: true));
        }

        memoryStream.Position = 0;
        return Result<Bitmap>.Success(new Bitmap(memoryStream));
    }
}
using System.Text.Json;
using Avalonia.Media.Imaging;
using GottaManagePlus.Models;
using GottaManagePlus.Models.UI;
using GottaManagePlus.Services.GameEnvironmentServices;
using GottaManagePlus.Utils;
using Serilog;

namespace GottaManagePlus.Services.APIServices;

public class GamebananaApiService(IHttpClientFactory httpClientFactory)
{
    private const string
        ClientName = "GameBanana",
        apiVersion = "apiv12"; // Current API Version
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private const int defaultStreamAllocationBuffer = 81920;

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
    /// Downloads a file from the specified URL to a temporary directory and returns a <see cref="FileStream"/> for reading the downloaded content.
    /// </summary>
    /// <param name="file">The mod file containing download metadata, including <see cref="ModItem.ModFile.DownloadUrl"/> and <see cref="ModItem.ModFile.FileName"/>.</param>
    /// <param name="fileDestinationPath">The intended final destination path for the file (DIRECTORY-ONLY).</param>
    /// <param name="controller">The game environment controller used to create a temporary download directory.</param>
    /// <param name="progress">Optional progress reporter that receives download progress updates when content length is available.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous download operation.</param>
    /// <param name="logger">Optional logger for error reporting.</param>
    /// <returns>
    /// A <see cref="Result"/> containing a path (<see langword="string"/>) of the downloaded file. 
    /// or a failure result with an error message if the download fails.
    /// </returns>
    /// <remarks>
    /// The returned <see cref="FileStream"/> is opened for reading and must be disposed by the caller.
    /// The temporary directory will be cleaned up when the stream is disposed and the temp directory handle is released.
    /// </remarks>
    public async Task<Result<string>> DownloadFile(ModItem.ModFile file, string fileDestinationPath, GameEnvironmentController controller, IProgress<ProgressReport>? progress = null,
        ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        if (!File.GetAttributes(fileDestinationPath).HasFlag(FileAttributes.Directory))
            return Result<string>.Failure("Destination Path is not a directory.");
        
        // HTTP Request
        var httpClient = _httpClientFactory.CreateClient(ClientName);

        using var response = await httpClient.GetAsync(
            file.DownloadUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
            return Result<string>.Failure($"HTTP Error: {response.StatusCode}");
        
        // Prepare Temporary Download Location
        using var tempDir = controller.CreateTempSubdirectory(logger);
        
        // Create file for writing (disposed manually in finally block)
        var tempFilePath = Path.Combine(tempDir.DirectoryInfo.FullName, file.FileName);
        var destinationPath = Path.Combine(fileDestinationPath, file.FileName);
        
        logger?.Information("Initialized download of file ({file})", file.ToString());

        try
        {
            // Download file to temp folder
            await using (var fileStream = File.OpenWrite(tempFilePath))
            {
                // Download with Progress Reporting
                var contentLength = response.Content.Headers.ContentLength;
                await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);

                var buffer = new byte[defaultStreamAllocationBuffer];
                var totalBytesRead = 0L;

                int bytesRead;
                while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                    totalBytesRead += bytesRead;

                    // Report progress if provider is set and content length is known
                    if (progress == null || !contentLength.HasValue) continue;
                    progress.Report(
                        new ProgressReport(totalBytesRead, contentLength.Value * 100,
                            currentStatus: "Retrieving image from URL...", usePercentage: true));
                    logger?.Information("Download Progress: {bytes}/{left} bytes", totalBytesRead, contentLength.Value);
                }
            }
            
            // Move the written file it to the right destination.
            File.Move(tempFilePath, destinationPath);
            logger?.Information("Moved file to {path}", destinationPath);
            
            return Result<string>.Success(fileDestinationPath);
        }
        catch (Exception e)
        {
            // Error Handling
            logger?.Error(e, "Failed to download file ('{file}')", file.FileName);
            return Result<string>.Failure($"Download Failure: {e.Message}");
        }
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
            return Result<Bitmap>.Failure($"HTTP Error: {response.StatusCode}");
        

        var contentLength = response.Content.Headers.ContentLength;
        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);

        var buffer = new byte[defaultStreamAllocationBuffer];
        var totalBytesRead = 0L;
        using var memoryStream = new MemoryStream();

        int bytesRead;
        while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await memoryStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            totalBytesRead += bytesRead;

            // Report progress if provider is set and content length is known
            if (progress == null || !contentLength.HasValue) continue;
            progress.Report(
                new ProgressReport(totalBytesRead, contentLength.Value * 100,
                    currentStatus: "Retrieving image from URL...", usePercentage: true));
        }

        return Result<Bitmap>.Success(new Bitmap(memoryStream));
    }
}
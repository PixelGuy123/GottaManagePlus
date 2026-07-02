using GottaManagePlus.Models;
using GottaManagePlus.Models.UI;
using GottaManagePlus.Services.APIServices;
using GottaManagePlus.Services.GameEnvironmentServices;

namespace GottaManagePlus.Utils;

/// <summary>
/// Provides conversion extensions between the detailed <see cref="ModItem"/> model
/// and the summary <see cref="GameBananaIndex"/> model (including nested types).
/// </summary>
public static class GamebananaModelUtils
{
    // ========== Top-level conversions ==========

    /// <summary>
    /// Converts an <see cref="IndexMod"/> (summary record) into a partial <see cref="ModItem"/>.
    /// Only fields present in both models are filled; others retain default values.
    /// </summary>
    public static async Task<ModItem> ToModItem(this IndexMod indexMod, GamebananaApiService gamebananaApiService, GameEnvironmentController controller, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(indexMod);
        ArgumentNullException.ThrowIfNull(gamebananaApiService);

        // Get all the data from this id
        var result = await gamebananaApiService.GetSubmissionDataAsync(indexMod.Id, cancellationToken);
        if (result.IsFailure)
            throw new NullReferenceException(result.Error);

        var modItem = result.Value!;
        // # Use the service to implement fetching calls.
        // Attempt to load its thumbnail image too.
        await modItem.AttemptToLoadImagesFromURLs(gamebananaApiService, false, null, cancellationToken);
        
        // Attempt to get the IndexedFile features
        foreach (var file in modItem.AllFiles)
            file.IndexedFile = (await gamebananaApiService.GetIndexedFileFromFileId(file.Id, cancellationToken)).Value ?? 
                               throw new NullReferenceException($"IndexedFile from {modItem} failed to load.");
        
        // Update mod item's environment files
        modItem.UpdateEnvironmentallyValidFiles(controller);
        
        return modItem;
    }

    /// <summary>
    /// Converts a <see cref="ModItem"/> (detailed model) into a summary <see cref="IndexMod"/>.
    /// Only fields that exist in <see cref="IndexMod"/> are copied.
    /// </summary>
    public static IndexMod ToIndexMod(this ModItem modItem)
    {
        ArgumentNullException.ThrowIfNull(modItem);

        return new IndexMod
        {
            Id = modItem.Id,
            ModelName = string.Empty,
            SingularTitle = string.Empty,
            IconClasses = string.Empty,
            Name = modItem.Name,
            ProfileUrl = string.Empty,
            DateAdded = modItem.DateAdded,
            DateModified = modItem.DateModified ?? DateTime.UtcNow,
            HasFiles = modItem.Files.Any(f => !f.IsArchived),
            PayType = modItem.PayType,
            Tags = [.. modItem.Tags.Select(t => t.Value)],
            PreviewMedia = modItem.PreviewMedia?.ToIndexPreviewMedia(),
            Submitter = modItem.Submitter?.ToIndexSubmitter(),
            Game = null,
            RootCategory = null,
            SubCategory = null,
            Version = modItem.Version,
            IsObsolete = false,
            InitialVisibility = modItem.InitialVisibility,
            HasContentRatings = false,
            LikeCount = modItem.LikeCount ?? 0,
            PostCount = modItem.PostCount,
            WasFeatured = false,
            ViewCount = modItem.ViewCount ?? 0,
            IsOwnedByAccessor = false,
        };
    }

    // ========== ModSubmitter conversions ==========

    /// <summary>
    /// Converts a GameBanana index submitter to the simplified <see cref="ModItem"/> submitter.
    /// </summary>
    public static ModItem.ModSubmitter ToModItemSubmitter(this ModSubmitter? indexSubmitter)
    {
        if (indexSubmitter == null)
            return null!;

        return new ModItem.ModSubmitter
        {
            Name = indexSubmitter.Name
        };
    }

    /// <summary>
    /// Converts a <see cref="ModItem"/> submitter to a GameBanana index submitter, using defaults for missing fields.
    /// </summary>
    public static ModSubmitter ToIndexSubmitter(this ModItem.ModSubmitter? modSubmitter)
    {
        if (modSubmitter == null)
            return null!;

        return new ModSubmitter
        {
            Id = 0,
            Name = modSubmitter.Name,
            IsOnline = false,
            HasRipe = false,
            ProfileUrl = string.Empty,
            AvatarUrl = string.Empty,
            HdAvatarUrl = null,
            UpicUrl = null,
            ClearanceLevels = []
        };
    }

    // ========== ModPreviewMedia conversions ==========

    /// <summary>
    /// Converts a GameBanana index preview media to the <see cref="ModItem"/> format.
    /// </summary>
    public static ModItem.ModPreviewMedia ToModItemPreviewMedia(this ModPreviewMedia? indexMedia)
    {
        if (indexMedia == null)
            return null!;

        return new ModItem.ModPreviewMedia
        {
            Images = indexMedia.Images.Select(img => img.ToModItemImage()).ToList() ?? []
        };
    }

    /// <summary>
    /// Converts a <see cref="ModItem"/> preview media to the GameBanana index format.
    /// </summary>
    public static ModPreviewMedia ToIndexPreviewMedia(this ModItem.ModPreviewMedia? modMedia)
    {
        if (modMedia == null)
            return null!;
        return new ModPreviewMedia
        {
            Images = modMedia.Images.Select(img => img.ToIndexImage()).ToList() ?? []
        };
    }

    // ========== ModImage conversions ==========

    /// <summary>
    /// Converts a GameBanana index image (which contains multiple sizes) to the simpler
    /// <see cref="ModItem"/> image (which only knows about 100px and 530px URLs).
    /// </summary>
    public static ModItem.ModImage ToModItemImage(this ModImage? indexImage)
    {
        if (indexImage == null)
            return null!;

        return new ModItem.ModImage
        {
            BaseUrl = indexImage.BaseUrl,
            File = indexImage.File,
            File100 = indexImage.File100 ?? string.Empty,
            File530 = indexImage.File530 ?? string.Empty
        };
    }

    /// <summary>
    /// Converts a <see cref="ModItem"/> image (only BaseUrl, File, File100, File530) to the richer GameBanana index image.
    /// Unavailable fields (type, caption, dimensions, other sizes) are left default.
    /// </summary>
    public static ModImage ToIndexImage(this ModItem.ModImage? modImage)
    {
        if (modImage == null)
            return null!;

        return new ModImage
        {
            Type = string.Empty,
            BaseUrl = modImage.BaseUrl,
            Caption = string.Empty,
            File = modImage.File,
            File220 = null,
            Height220 = null,
            Width220 = null,
            File530 = modImage.File530,
            Height530 = null,
            Width530 = null,
            File100 = modImage.File100,
            Height100 = null,
            Width100 = null,
            File800 = null,
            Height800 = null,
            Width800 = null
        };
    }
    
    /// <summary>
    /// Gathers dependency files from GameBanana by parsing requirement URLs from the specified mod item,
    /// fetching the corresponding submission data, and converting each valid file into an indexed file.
    /// </summary>
    /// <param name="modItem">The mod item containing requirement URLs to process.</param>
    /// <param name="gamebananaApiService">The API service used to fetch submission data and indexed files from GameBanana.</param>
    /// <returns>A task that represents the asynchronous operation, containing a dictionary of results of mods with their respective indexed files for all valid dependencies.</returns>
    public static async Task<Dictionary<ModItem, ModItem.ModFile>> GatherDependencyFiles(this ModItem modItem, GamebananaApiService gamebananaApiService, CancellationToken cancellationToken = default)
    {
        // Look up for Gamebanana.
        const string gamebanana_lookup_string = "https://gamebanana.com/mods/";
        Dictionary<ModItem, ModItem.ModFile> dependencies = [];
        var allRequirementUrls = modItem.Requirements.ConvertAll(r => r[1]); // Index 1 is always the link
        
        // Look for GameBanana links.
        foreach (var id in 
                 from url in allRequirementUrls
                 where url.StartsWith(gamebanana_lookup_string) 
                 select url.Substring(gamebanana_lookup_string.Length, url.Length - gamebanana_lookup_string.Length))
        {
            // Search the mod item in gamebanana and get the files from it.
            var searchedModItem = await gamebananaApiService.GetSubmissionDataAsync(int.Parse(id), cancellationToken);

            // Skip if this fails to be loaded.
            if (searchedModItem.IsFailure) continue;

            var modItemFiles = searchedModItem.Value.AllEnvironmentallyValidFiles;
            
            // For each ModFile, add a converted IndexFile.
            foreach (var file in modItemFiles)
                file.IndexedFile = (await gamebananaApiService.GetIndexedFileFromFileId(file.Id, cancellationToken)).Value;
            

            // Add to the dictionary.
            dependencies.Add(searchedModItem.Value, modItemFiles[0]);
        }
        
        return dependencies;
    }
}
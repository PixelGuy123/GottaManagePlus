using System;
using System.Collections.Generic;
using System.Linq;
using GottaManagePlus.Models.UI;
using GottaManagePlus.Services.APIServices;
using GottaManagePlus.Services.GameEnvironmentServices;

namespace GottaManagePlus.Utils;

/// <summary>
/// Provides conversion extensions between the detailed <see cref="ModItem"/> model
/// and the summary <see cref="GameBananaIndex"/> model (including nested types).
/// </summary>
public static class GamebananaModelConversionUtils
{
    // ========== Top-level conversions ==========

    /// <summary>
    /// Converts an <see cref="IndexMod"/> (summary record) into a partial <see cref="ModItem"/>.
    /// Only fields present in both models are filled; others retain default values.
    /// </summary>
    public static async Task<ModItem> ToModItem(this IndexMod indexMod, GamebananaApiService gamebananaApiService, GameEnvironmentController controller)
    {
        ArgumentNullException.ThrowIfNull(indexMod);
        ArgumentNullException.ThrowIfNull(gamebananaApiService);

        // Get all the data from this id
        var result = await gamebananaApiService.GetSubmissionDataAsync(indexMod.Id);
        if (result.IsFailure)
            throw new NullReferenceException(result.Error);

        var modItem = result.Value!;
        // # Use the service to implement fetching calls.
        // Attempt to load its thumbnail image too.
        await modItem.AttemptToLoadImagesFromURLs(gamebananaApiService, false, null);
        
        // Attempt to get the IndexedFile features
        foreach (var file in modItem.AllFiles)
            file.IndexedFile = (await gamebananaApiService.GetIndexedFileFromFileId(file.Id)).Value ?? 
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
            ProfileUrl = string.Empty,            // Not directly in ModItem
            DateAdded = modItem.DateAdded,
            DateModified = modItem.DateModified,
            HasFiles = modItem.Files.Any(f => !f.IsArchived),
            PayType = modItem.PayType,
            Tags = [.. modItem.Tags.Select(t => t.Value)],
            PreviewMedia = modItem.PreviewMedia?.ToIndexPreviewMedia(),
            Submitter = modItem.Submitter?.ToIndexSubmitter(),
            Game = null,                          // Not present in ModItem
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
            Images = indexMedia.Images?.Select(img => img.ToModItemImage()).ToList() ?? []
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
            Images = modMedia.Images?.Select(img => img.ToIndexImage()).ToList() ?? []
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
}
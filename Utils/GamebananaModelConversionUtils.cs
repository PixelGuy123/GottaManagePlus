using System;
using System.Collections.Generic;
using System.Linq;
using GottaManagePlus.Models.UI;

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
    public static ModItem ToModItem(this IndexMod indexMod)
    {
        ArgumentNullException.ThrowIfNull(indexMod);

        return new ModItem
        {
            Id = indexMod.Id,
            Name = indexMod.Name,
            Description = string.Empty,          // Not available in index
            Submitter = indexMod.Submitter?.ToModItemSubmitter(),
            DateModified = indexMod.DateModified,
            DateAdded = indexMod.DateAdded,
            DownloadCount = 0,                   // Not in index
            ViewCount = indexMod.ViewCount,
            LikeCount = indexMod.LikeCount,
            PreviewMedia = indexMod.PreviewMedia?.ToModItemPreviewMedia(),
            IsPrivate = false,                   // Not in index
            IsTrashed = false,                   // Not in index
            Version = indexMod.Version,
            CommentsMode = "open",
            UpdatesCount = 0,
            HasUpdates = false,
            AllTodosCount = 0,
            HasTodos = false,
            PostCount = indexMod.PostCount,
            Tags = indexMod.Tags.ToList(),
            CreatedBySubmitter = false,
            IsPorted = false,
            ThanksCount = 0,
            InitialVisibility = indexMod.InitialVisibility,
            PayType = indexMod.PayType,
            GenerateTableOfContents = false,
            Text = string.Empty,
            ShowRipePromo = false,
            FollowLinks = false,
            AccessorHasUnliked = false,
            AccessorHasLiked = false,
            AccessorHasThanked = false,
            AccessorIsSubscribed = false,
            AccessorSubscriptionRowId = 0,
            AdvancedRequirementsExist = false,
            Requirements = [],
            Files = [],
            ArchivedFiles = [],
            Credits = [],
            // DownloadUrl, ImageUrl, ThumbnailUrl – leave empty (would need separate fetch)
        };
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
            Tags = modItem.Tags.ToList(),
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
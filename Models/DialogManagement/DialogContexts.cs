using Avalonia.Controls;
using Avalonia.Media;
using GottaManagePlus.Models.GameEnvironments;
using GottaManagePlus.Models.UI;

namespace GottaManagePlus.Models.DialogManagement;

/// <summary>
/// Base record for all dialog contexts. Provides a common type for dialog parameter passing.
/// </summary>
public abstract record DialogContext();

/// <summary>
/// Context for dialogs that display a title.
/// </summary>
public record TitleContext(string? Title = null) : DialogContext;

/// <summary>
/// Context for dialogs that display a title and a message.
/// </summary>
public record TitleMessageContext(string? Title = null, string? Message = null) : TitleContext(Title);

/// <summary>
/// Context for confirmation dialogs with customizable buttons and optional log display.
/// </summary>
/// <param name="OnlyConfirmButton">If true, only shows the confirm button.</param>
/// <param name="Title">The dialog title.</param>
/// <param name="Message">The dialog message.</param>
/// <param name="ConfirmText">Text for the confirm button.</param>
/// <param name="CancelText">Text for the cancel button.</param>
/// <param name="DescriptionAlignment">Text alignment for the description.</param>
/// <param name="LogContainer">Optional log container for displaying categorized logs.</param>
public record ConfirmDialogContext(
    bool OnlyConfirmButton = false,
    string? Title = null,
    string? Message = null,
    string? ConfirmText = null,
    string? CancelText = null,
    TextAlignment DescriptionAlignment = TextAlignment.Center,
    LogContainer? LogContainer = null
) : TitleMessageContext(Title, Message);

/// <summary>
/// Context for loading dialogs with a delegate to execute.
/// </summary>
/// <param name="LoadingDelegate">The delegate to execute during loading.</param>
/// <param name="DelegateArgs">Additional arguments to pass to the loading delegate.</param>
/// <param name="Title">The dialog title.</param>
/// <param name="Status">The initial status text.</param>
public record LoadingDialogContext(
    Delegate LoadingDelegate,
    object?[]? DelegateArgs = null,
    string? Title = null,
    string? Status = null
) : TitleContext(Title);

/// <summary>
/// Context for multi-loading dialogs that manage multiple loading tasks.
/// </summary>
/// <param name="Title">The dialog title.</param>
/// <param name="Status">The initial status text.</param>
public record MultiLoadingDialogContext(
    string? Title = null,
    string? Status = null,
    params LoadingDialogContext[] LoadingDialogContexts
) : TitleContext(Title);

/// <summary>
/// Context for creating profiles, requiring file picker and existing profiles list.
/// </summary>
/// <param name="FilePicker">The file picker service for browsing files.</param>
/// <param name="ExistingProfiles">Collection of existing profile names.</param>
public record CreateProfileDialogContext(
    Services.ExplorerServices.FilePicker FilePicker,
    IEnumerable<string> ExistingProfiles
) : DialogContext;

/// <summary>
/// Context for mod selection dialog, requiring various services.
/// </summary>
/// <param name="DialogService">The dialog service instance.</param>
/// <param name="GamebananaApiService">The Gamebanana API service.</param>
/// <param name="GameEnvironmentController">The game environment controller.</param>
/// <param name="ModInstaller">The mod installer service.</param>
public record ModSelectionDialogContext(
    Services.DialogService DialogService,
    Services.APIServices.GamebananaApiService GamebananaApiService,
    Services.GameEnvironmentServices.GameEnvironmentController GameEnvironmentController,
    Services.ModServices.ModInstaller ModInstaller
) : DialogContext;

/// <summary>
/// Context for previewing profile details, requiring profile metadata and services.
/// </summary>
/// <param name="Profile">The profile metadata to display.</param>
/// <param name="AllowProfileDeletion">Whether profile deletion is allowed.</param>
/// <param name="DirectoryLauncher">Service for launching directory info.</param>
/// <param name="DialogService">The dialog service instance.</param>
/// <param name="GameEnvironmentController">The game environment controller.</param>
public record PreviewProfileDialogContext(
    ProfileMetadata Profile,
    bool AllowProfileDeletion = false,
    Services.ExplorerServices.DirectoryLauncher? DirectoryLauncher = null,
    Services.DialogService? DialogService = null,
    Services.GameEnvironmentServices.GameEnvironmentController? GameEnvironmentController = null
) : DialogContext;
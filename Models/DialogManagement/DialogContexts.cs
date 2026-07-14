/*
This file is part of GottaManagePlus (https://github.com/PixelGuy123/GottaManagePlus)

    Copyright (C) 2026 PixelGuy123

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.

*/

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
/// <param name="Title">The dialog title.</param>
/// <param name="Message">The dialog message.</param>
/// <param name="ConfirmText">Text for the confirm button.</param>
/// <param name="CancelText">Text for the cancel button.</param>
/// <param name="DescriptionAlignment">Text alignment for the description.</param>
/// <param name="LogContainer">Optional log container for displaying categorized logs.</param>
public record ConfirmDialogContext(
    string? Title = null,
    string? Message = null,
    string ConfirmText = "Ok",
    string? CancelText = null,
    TextAlignment DescriptionAlignment = TextAlignment.Center,
    LogContainer? LogContainer = null
) : TitleMessageContext(Title, Message)
{
    /// <summary>
    /// The type of answers a prompted user can respond.
    /// </summary>
    public enum QuestionAnswerType
    {
        YesOrNo = 0,
        AllowOrDisallow,
        ProceedOrCancel,
        AdaptOrIgnore
    }

    /// <summary>
    /// Creates a confirmation dialog with only a confirm button using a predefined answer type.
    /// </summary>
    /// <param name="answerType">The type of confirmation button to display.</param>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The dialog message.</param>
    /// <param name="descriptionAlignment">Text alignment for the description.</param>
    /// <param name="logContainer">Optional log container for displaying categorized logs.</param>
    public ConfirmDialogContext(
        QuestionAnswerType answerType,
        string? title = null,
        string? message = null,
        TextAlignment descriptionAlignment = TextAlignment.Center,
        LogContainer? logContainer = null
    ) : this(title, message, AnswerToString(answerType).Yes, null, descriptionAlignment, logContainer)
    {
    }

    // ---- Private -----
    /// <summary>
    /// Converts a <see cref="QuestionAnswerType"/> to its <see langword="string"/> form.
    /// </summary>
    /// <param name="answerType">The answer type for conversion.</param>
    /// <returns>A <see cref="Tuple{string, string}"/> for a <c>Yes</c> and a <c>No</c>.</returns>
    private static (string Yes, string No) AnswerToString(QuestionAnswerType answerType) => answerType switch
    {
        QuestionAnswerType.YesOrNo => ("Yes", "No"), // TODO: Here needs localization
        QuestionAnswerType.AllowOrDisallow => ("Allow", "Disallow"),
        QuestionAnswerType.ProceedOrCancel => ("Proceed", "Cancel"),
        QuestionAnswerType.AdaptOrIgnore => ("Adapt", "Ignore"),
        _ => ("Yes", "No")
    };
}

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
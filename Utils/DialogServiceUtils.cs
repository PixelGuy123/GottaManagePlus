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

using GottaManagePlus.Models.DialogManagement;
using GottaManagePlus.Models.UI;
using GottaManagePlus.Services;
using GottaManagePlus.ViewModels;

namespace GottaManagePlus.Utils;

public static class DialogServiceUtils
{
    /// <summary>
    /// Variations of the same user choice selection.
    /// </summary>
    public enum QuestionAnswerType
    {
        YesOrNo,
        AllowOrDisallow,
        ProceedOrCancel,
        AdaptOrIgnore
    }
    
    extension(DialogService dialogService)
    {
        /// <summary>
        /// Prompts the user with a question dialog and returns their confirmation response.
        /// </summary>
        /// <param name="title">The title of the dialog.</param>
        /// <param name="message">The message to display in the dialog.</param>
        /// <param name="answerType">The type of answer options to present to the user.</param>
        /// <param name="container">Optional log container for tracking dialog interactions.</param>
        /// <returns>True if the user confirmed the dialog, false otherwise.</returns>
        public async Task<bool> PromptUserQuestion(string title,
            string message,
            QuestionAnswerType answerType = QuestionAnswerType.YesOrNo,
            LogContainer? container = null)
        {
            var result = await dialogService.ShowDialog<ConfirmDialogViewModel>(
                new ConfirmDialogContext(answerType, title, message, logContainer: container));
            return result?.Confirmed ?? false;
        }

        /// <summary>
        /// Displays a notification dialog to the user without requiring a confirmation choice.
        /// </summary>
        /// <param name="title">The title of the notification dialog.</param>
        /// <param name="message">The message to display in the notification.</param>
        /// <param name="container">Optional log container for tracking dialog interactions.</param>
        /// <returns>The dialog result containing user interaction information.</returns>
        public Task<DialogResult?> NotifyUser(string title,
            string message,
            LogContainer? container = null) =>
            dialogService.ShowDialog<ConfirmDialogViewModel>(
                new ConfirmDialogContext(
                    Title: title,
                    Message: message,
                    CancelText: null,
                    LogContainer: container));

        /// <summary>
        /// Generates and displays a generic loading dialog that executes a delegate asynchronously.
        /// </summary>
        /// <param name="title">The title of the loading dialog.</param>
        /// <param name="status">The status text to display during the loading process.</param>
        /// <param name="loadingDelegate">The delegate to execute asynchronously.</param>
        /// <param name="args">Arguments to pass to the delegate.</param>
        /// <returns>The dialog result containing the operation outcome.</returns>
        public Task<DialogResult?> GenerateGenericLoadingProcess(string? title,
            string? status,
            Delegate loadingDelegate,
            params object?[] args) =>
            dialogService.ShowDialog<LoadingDialogViewModel>(
                new LoadingDialogContext(loadingDelegate, args, title, status));

        /// <summary>
        /// Generates a loading dialog that executes a delegate and returns a boolean result indicating success.
        /// </summary>
        /// <param name="title">The title of the loading dialog.</param>
        /// <param name="status">The status text to display during the loading process.</param>
        /// <param name="loadingDelegate">The delegate to execute asynchronously.</param>
        /// <param name="args">Arguments to pass to the delegate.</param>
        /// <returns>True if the operation completed successfully and the user confirmed, false otherwise.</returns>
        public async Task<bool> GenerateBooleanLoadingProcess(string? title,
            string? status,
            Delegate loadingDelegate,
            params object?[] args)
        {
            var result = await dialogService.GenerateGenericLoadingProcess(title,
                status,
                loadingDelegate,
                args);
            return result is { Confirmed: true } &&
                   (result.Result is not bool value || value);
        }
    }
}

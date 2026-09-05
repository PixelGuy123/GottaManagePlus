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

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GottaManagePlus.Models.DialogManagement;

namespace GottaManagePlus.ViewModels;

public abstract partial class DialogViewModel : ViewModelBase
{
    #region Constructor

    protected DialogViewModel()
    {
        ConfirmCommand = new RelayCommand(() => 
            _completionSource?.TrySetResult(new DialogResult(Confirmed: true)));
        CancelCommand = new RelayCommand(() => 
            _completionSource?.TrySetResult(new DialogResult(Confirmed: false)));
    }

    #endregion
    
    #region Commands

    public IRelayCommand ConfirmCommand { get; }
    public IRelayCommand CancelCommand { get; }

    #endregion

    #region Private Fields

    private TaskCompletionSource<DialogResult>? _completionSource;

    #endregion
    
    #region Properties

    [ObservableProperty]
    public partial bool IsDialogOpen { get; set; }

    #endregion

    #region Public Methods

    public async Task<DialogResult?> Show(DialogContext? context)
    {
        if (IsDialogOpen)
            return null;

        IsDialogOpen = true;
        try
        {
            return await OnShow(context);
        }
        finally
        {
            IsDialogOpen = false;
            OnClose();
        }
    }

    #endregion

    #region Protected Abstract Methods

    protected abstract Task<DialogResult?> OnShow(DialogContext? context);

    protected virtual void OnClose()
    {
    }

    protected void Complete(bool confirmed) =>
        _completionSource?.TrySetResult(new DialogResult(confirmed));

    protected void Close() => Complete(false);
    
    protected static T ExpectContext<T>(DialogContext? context) where T : DialogContext
    {
        var t = ExpectContextOrNull<T>(context);
        ArgumentNullException.ThrowIfNull(t, nameof(context));
        return t;
    }
    
    protected static T? ExpectContextOrNull<T>(DialogContext? context) where T : DialogContext
    {
        return context switch
        {
            null => null,
            T t => t,
            _ => throw new InvalidCastException(
                $"Expected context '{typeof(T).Name}' does not match given context '{context.GetType().Name}'.")
        };
    }

    #endregion
    
    #region Protected Completion API

    /// <summary>
    /// Returns a task that completes when a user action is done.
    /// Typical usage: await this inside <see cref="OnShow"/>.
    /// </summary>
    protected Task<DialogResult> WaitForCompletionAsync()
    {
        _completionSource = new TaskCompletionSource<DialogResult>();
        return _completionSource.Task;
    }

    #endregion
}
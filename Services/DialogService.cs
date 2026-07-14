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

using GottaManagePlus.Interfaces;
using GottaManagePlus.Models.DialogManagement;
using GottaManagePlus.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace GottaManagePlus.Services;

public sealed class DialogService(IServiceProvider serviceProvider)
{
    // Main Service Provider for Dialogs
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    
    // Use a simple Stack with a lock for thread safety
    private readonly Stack<DialogViewModel> _dialogStack = new();
    private readonly Lock _stackLock = new();
    
    private IDialogProvider? _dialogProvider;

    public void RegisterProvider(IDialogProvider provider)
    {
        lock (_stackLock)
        {
            _dialogProvider = provider;
        }
    }

    public TDialog GetUnmanagedDialog<TDialog>()
        where TDialog : DialogViewModel =>  _serviceProvider.GetRequiredService<TDialog>();

    public async Task<object?> ShowDialog<TDialog>(DialogContext? context) // context is placeholder
        where TDialog : DialogViewModel
    {
        if (_dialogProvider == null)
            throw new InvalidOperationException("DialogProvider has not been registered yet.");
        
        // Get the dialog
        var dialogViewModel = _serviceProvider.GetRequiredService<TDialog>();

        // Atomically push the dialog and set the provider
        lock (_stackLock)
        {
            _dialogStack.Push(dialogViewModel);
            _dialogProvider.Dialog = dialogViewModel;
        }

        try
        {
            return await dialogViewModel.Show(context);
        }
        catch
        {
            return false;
        }
        finally
        {
            // Atomically pop and restore the previous dialog
            lock (_stackLock)
            {
                // Ensure we pop the correct dialog (it should be on top)
                if (_dialogStack.Count > 0 && _dialogStack.Peek() == dialogViewModel)
                {
                    _dialogStack.Pop();
                    _dialogProvider.Dialog = _dialogStack.Count > 0 ? _dialogStack.Peek() : null;
                }
            }
        }
    }
}
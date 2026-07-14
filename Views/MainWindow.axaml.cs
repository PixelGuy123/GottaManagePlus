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
using GottaManagePlus.ViewModels;
using Serilog;

namespace GottaManagePlus.Views;

public partial class MainWindow : Window
{
    private bool _canClose;
    
    public MainWindow()
    {
        InitializeComponent();
    }

    // Main Window Close
    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        try
        {
            if (_canClose)
            {
                base.OnClosing(e);
                return;
            }
            
            if (DataContext is not MainWindowViewModel viewModel) return;
        
            e.Cancel = true; // Cancels the closing event
            if (!await viewModel.HandleSettingsSave(!e.IsProgrammatic)) return;
        
            _canClose = true;
            Close(); // Manually close
        }
        catch (Exception ex)
        {
            Log.Logger.Error("{exception}", ex);
            _canClose = true;
            Close();
        }
    }
}
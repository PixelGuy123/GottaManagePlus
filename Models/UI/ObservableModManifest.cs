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

namespace GottaManagePlus.Models.UI;

public class ObservableModManifest(ModManifest manifest) : ObservableObject
{
    // ModManifest.Metadata.Activated
    public bool IsActivated 
    {
        get => InnerManifest.Metadata.Activated;
        set
        {
            InnerManifest.Metadata.Activated = value;
            OnPropertyChanged(); // Notify UI to refresh
        }
    }

    // For binding other ModManifest properties in XAML
    public ModManifest InnerManifest { get; } = manifest;
    
    // For implicit conversion
    public static implicit operator ObservableModManifest(ModManifest manifest) => new(manifest);
}
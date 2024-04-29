using System;
using Avalonia.Controls;
using Avalonia.Controls.Platform;

namespace Avalonia.Dialogs
{
    public record ManagedFileDialogOptions
    {
        public bool AllowDirectorySelection { get; set; }

        /// <summary>
        /// Allows to redefine how root volumes are populated in the dialog. 
        /// </summary>
        public IMountedVolumeInfoProvider? CustomVolumeInfoProvider { get; set; }

        /// <summary>
        /// Allows to redefine content root.
        /// Can be a custom Window or any ContentControl (Popup hosted).   
        /// </summary>
        public Func<ContentControl>? ContentRootFactory { get; set; } 
    }
}

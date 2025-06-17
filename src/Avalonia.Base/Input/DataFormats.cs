using System;
using System.ComponentModel;
using Avalonia.Input.Platform;
using Avalonia.Metadata;

namespace Avalonia.Input
{
    public static class DataFormats
    {
        /// <summary>
        /// Dataformat for plaintext
        /// </summary>
        [Obsolete($"Use {nameof(DataFormat)}.{nameof(DataFormat.Text)} instead.")]
        public static readonly string Text = DataFormat.NameText;

        /// <summary>
        /// Dataformat for one or more files.
        /// </summary>
        [Obsolete($"Use {nameof(DataFormat)}.{nameof(DataFormat.File)} instead.")]
        public static readonly string Files = DataFormat.NameFiles;
        
        /// <summary>
        /// Dataformat for one or more filenames
        /// </summary>
        /// <remarks>
        /// This data format is supported only on desktop platforms.
        /// </remarks>
        [Unstable("Use DataFormats.Files, this format is supported only on desktop platforms. And it will be removed in 12.0."), EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly string FileNames = DataFormat.NameFileNames;
    }
}

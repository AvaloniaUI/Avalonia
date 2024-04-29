using System;
using System.ComponentModel;
using Avalonia.Metadata;

namespace Avalonia.Input
{
    public static class DataFormats
    {
        /// <summary>
        /// Dataformat for plaintext
        /// </summary>
        public static readonly string Text = nameof(Text);

        /// <summary>
        /// Dataformat for one or more files.
        /// </summary>
        public static readonly string Files = nameof(Files);
        
        /// <summary>
        /// Dataformat for one or more filenames
        /// </summary>
        /// <remarks>
        /// This data format is supported only on desktop platforms.
        /// </remarks>
        [Unstable("Use DataFormats.Files, this format is supported only on desktop platforms. And it will be removed in 12.0."), EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly string FileNames = nameof(FileNames);
    }
}

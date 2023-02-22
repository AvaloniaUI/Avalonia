using System;

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
        [Obsolete("Use DataFormats.Files, this format is supported only on desktop platforms.")]
        public static readonly string FileNames = nameof(FileNames);
    }
}

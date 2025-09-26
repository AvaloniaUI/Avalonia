using System;
using System.ComponentModel;
using Avalonia.Input.Platform;

namespace Avalonia.Input
{
    public static class DataFormats
    {
        /// <summary>
        /// Dataformat for plaintext
        /// </summary>
        [Obsolete($"Use {nameof(DataFormat)}.{nameof(DataFormat.Text)} instead.")]
        public static readonly string Text = nameof(Text);

        /// <summary>
        /// Dataformat for one or more files.
        /// </summary>
        [Obsolete($"Use {nameof(DataFormat)}.{nameof(DataFormat.File)} instead.")]
        public static readonly string Files = nameof(Files);
        
        /// <summary>
        /// Dataformat for one or more filenames
        /// </summary>
        /// <remarks>
        /// This data format is supported only on desktop platforms.
        /// </remarks>
        [Obsolete($"Use {nameof(DataFormat)}.{nameof(DataFormat.File)} instead."), EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly string FileNames = nameof(FileNames);

#pragma warning disable CS0618 // Type or member is obsolete

        internal static DataFormat ToDataFormat(string format)
        {
            if (format == Text)
                return DataFormat.Text;

            if (format == Files || format == FileNames)
                return DataFormat.File;

            return DataFormat.CreateBytesPlatformFormat(format);
        }

        internal static string ToString(DataFormat format)
        {
            if (DataFormat.Text.Equals(format))
                return Text;

            if (DataFormat.File.Equals(format))
                return Files;

            return format.Identifier;
        }

#pragma warning restore CS0618 // Type or member is obsolete
    }
}

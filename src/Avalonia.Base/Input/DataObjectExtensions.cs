using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Platform.Storage;

namespace Avalonia.Input
{
    public static class DataObjectExtensions
    {
        /// <summary>
        /// Returns a list of files if the DataObject contains files or filenames.
        /// <seealso cref="DataFormats.Files"/>.
        /// </summary>
        /// <returns>
        /// Collection of storage items - files or folders. If format isn't available, returns null.
        /// </returns>
        public static IEnumerable<IStorageItem>? GetFiles(this IDataObject dataObject)
        {
            return dataObject.Get(DataFormats.Files) as IEnumerable<IStorageItem>;
        }

        /// <summary>
        /// Returns a list of filenames if the DataObject contains filenames.
        /// <seealso cref="DataFormats.FileNames"/>
        /// </summary>
        /// <returns>
        /// Collection of file names. If format isn't available, returns null.
        /// </returns>
        [System.Obsolete("Use GetFiles, this method is supported only on desktop platforms."), EditorBrowsable(EditorBrowsableState.Never)]
        public static IEnumerable<string>? GetFileNames(this IDataObject dataObject)
        {
            return (dataObject.Get(DataFormats.FileNames) as IEnumerable<string>)
                ?? dataObject.GetFiles()?
                .Select(f => f.TryGetLocalPath())
                .Where(p => !string.IsNullOrEmpty(p))
                .OfType<string>();
        }

        /// <summary>
        /// Returns the dragged text if the DataObject contains any text.
        /// <seealso cref="DataFormats.Text"/>
        /// </summary>
        /// <returns>
        /// A text string. If format isn't available, returns null.
        /// </returns>
        public static string? GetText(this IDataObject dataObject)
        {
            return dataObject.Get(DataFormats.Text) as string;
        }
    }
}

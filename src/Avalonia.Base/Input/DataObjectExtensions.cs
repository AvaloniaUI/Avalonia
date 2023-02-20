using System.Collections.Generic;
using System.IO;
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
        [System.Obsolete("Use GetFiles, this method is supported only on desktop platforms.")]
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

        /// <summary>
        /// Returns a stream if the DataObject contains any stream.
        /// <seealso cref="DataFormats.Stream"/>.
        /// </summary>
        /// <returns>
        /// A stream. If not available returns null
        /// </returns>
        public static Stream? GetStream(this IDataObject dataObject)
        {
            return dataObject.Get(DataFormats.Stream) as Stream;
        }

        /// <summary>
        /// Wraps a string in a DataObject
        /// </summary>
        /// <param name="text">The string to store in the DataObject</param>
        /// <returns></returns>
        public static DataObject ToDataObject(this string text)
        {
            var dataObject = new DataObject();
            dataObject.Set(DataFormats.Text, text);
            return dataObject;
        }

        /// <summary>
        /// Wraps a collection of files in a DataObject
        /// </summary>
        /// <param name="files">A collection of <see cref="IStorageItem"/> to store in the DataObject</param>
        /// <returns></returns>
        public static DataObject ToDataObject(this IEnumerable<IStorageItem> files)
        {
            var dataObject = new DataObject();
            dataObject.Set(DataFormats.Files, files);
            return dataObject;
        }

        /// <summary>
        /// Wraps a stream in a DataObject
        /// </summary>
        /// <param name="stream">The stream to store in the DataObject</param>
        /// <param name="tag">The string tag to associate with the stream</param>
        /// <returns></returns>
        public static DataObject ToDataObject(this Stream stream, string tag = "")
        {
            var dataObject = new DataObject();
            dataObject.Set(DataFormats.Stream, stream);
            dataObject.Set(DataFormats.Text, tag);
            return dataObject;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace Avalonia.Input;

// Keep DataTransferExtensions.TryGetXxx methods in sync with AsyncDataTransferExtensions.TryGetXxxAsync ones.

/// <summary>
/// Contains extension methods for <see cref="IDataTransfer"/>.
/// </summary>
public static class DataTransferExtensions
{
    [Obsolete]
    internal static IDataObject ToLegacyDataObject(this IDataTransfer dataTransfer)
        => (dataTransfer as DataObjectToDataTransferWrapper)?.DataObject
           ?? new DataTransferToDataObjectWrapper(dataTransfer);

    /// <summary>
    /// Gets whether a <see cref="IDataTransfer"/> supports a specific format.
    /// </summary>
    /// <param name="dataTransfer">The <see cref="IDataTransfer"/> instance.</param>
    /// <param name="format">The format to check.</param>
    /// <returns>true if <paramref name="format"/> is supported, false otherwise.</returns>
    public static bool Contains(this IDataTransfer dataTransfer, DataFormat format)
    {
        var formats = dataTransfer.Formats;
        var count = formats.Count;

        for (var i = 0; i < count; ++i)
        {
            if (format == formats[i])
                return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the list of <see cref="IDataTransferItem"/> contained in this object, filtered by a given format.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Some platforms (such as Windows and X11) may only support a single data item for all formats
    /// except <see cref="DataFormat.File"/>.
    /// </para>
    /// <para>Items returned by this property must stay valid until the <see cref="IDataTransfer"/> is disposed.</para>
    /// </remarks>
    public static IEnumerable<IDataTransferItem> GetItems(this IDataTransfer dataTransfer, DataFormat format)
    {
        var items = dataTransfer.Items;
        var count = items.Count;

        for (var i = 0; i < count; ++i)
        {
            var item = items[i];
            if (item.Contains(format))
                yield return item;
        }
    }

    /// <summary>
    /// Tries to get a value for a given format from a <see cref="IDataTransfer"/>.
    /// </summary>
    /// <param name="dataTransfer">The <see cref="IDataTransfer"/> instance.</param>
    /// <param name="format">The format to retrieve.</param>
    /// <returns>A value for <paramref name="format"/>, or null if the format is not supported.</returns>
    /// <remarks>
    /// If the <see cref="IDataTransfer"/> contains several items supporting <paramref name="format"/>,
    /// the first matching one will be returned.
    /// </remarks>
    public static T? TryGetValue<T>(this IDataTransfer dataTransfer, DataFormat<T> format)
        where T : class
        => dataTransfer.GetItems(format).FirstOrDefault() is { } item ?
            item.TryGetValue(format) :
            null;

    /// <summary>
    /// Tries to get multiple values for a given format from a <see cref="IDataTransfer"/>.
    /// </summary>
    /// <param name="dataTransfer">The <see cref="IDataTransfer"/> instance.</param>
    /// <param name="format">The format to retrieve.</param>
    /// <returns>A list of values for <paramref name="format"/>, or null if the format is not supported.</returns>
    public static T[]? TryGetValues<T>(this IDataTransfer dataTransfer, DataFormat<T> format)
        where T : class
    {
        List<T>? results = null;

        foreach (var item in dataTransfer.GetItems(format))
        {
            var result = item.TryGetValue(format);
            if (result is null)
                continue;

            results ??= [];
            results.Add(result);
        }

        return results?.ToArray();
    }

    /// <summary>
    /// Returns a text, if available, from a <see cref="IDataTransfer"/> instance.
    /// </summary>
    /// <param name="dataTransfer">The <see cref="IDataTransfer"/> instance.</param>
    /// <returns>A string, or null if the format isn't available.</returns>
    /// <seealso cref="DataFormat.Text"/>.
    public static string? TryGetText(this IDataTransfer dataTransfer)
        => dataTransfer.TryGetValue(DataFormat.Text);

    /// <summary>
    /// Returns a file, if available, from a <see cref="IDataTransfer"/> instance.
    /// </summary>
    /// <param name="dataTransfer">The <see cref="IDataTransfer"/> instance.</param>
    /// <returns>An <see cref="IStorageItem"/> (file or folder), or null if the format isn't available.</returns>
    /// <seealso cref="DataFormat.File"/>.
    public static IStorageItem? TryGetFile(this IDataTransfer dataTransfer)
        => dataTransfer.TryGetValue(DataFormat.File);

    /// <summary>
    /// Returns a list of files, if available, from a <see cref="IDataTransfer"/> instance.
    /// </summary>
    /// <param name="dataTransfer">The <see cref="IDataTransfer"/> instance.</param>
    /// <returns>An array of <see cref="IStorageItem"/> (files or folders), or null if the format isn't available.</returns>
    /// <seealso cref="DataFormat.File"/>.
    public static IStorageItem[]? TryGetFiles(this IDataTransfer dataTransfer)
        => dataTransfer.TryGetValues(DataFormat.File);

    /// <summary>
    /// Returns a bitmap, if available, from a <see cref="IDataTransfer"/> instance.
    /// </summary>
    /// <param name="dataTransfer">The <see cref="IDataTransfer"/> instance.</param>
    /// <returns>A <see cref="Bitmap"/>, or null if the format isn't available.</returns>
    /// <seealso cref="DataFormat.Bitmap"/>.
    public static Bitmap? TryGetBitmap(this IDataTransfer dataTransfer)
        => dataTransfer.TryGetValue(DataFormat.Bitmap);
}

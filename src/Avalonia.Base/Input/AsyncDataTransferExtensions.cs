using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Logging;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace Avalonia.Input;

// Keep AsyncDataTransferExtensions.TryGetXxxAsync methods in sync with DataTransferExtensions.TryGetXxx ones.

/// <summary>
/// Contains extension methods for <see cref="IAsyncDataTransfer"/>.
/// </summary>
public static class AsyncDataTransferExtensions
{
    internal static IDataTransfer ToSynchronous(this IAsyncDataTransfer asyncDataTransfer, string logArea)
    {
        if (asyncDataTransfer is IDataTransfer dataTransfer)
            return dataTransfer;

        Logger.TryGet(LogEventLevel.Warning, logArea)?.Log(
            null,
            $"Using a synchronous wrapper for {nameof(IAsyncDataTransferItem)} {{Type}}. Consider implementing {nameof(IDataTransfer)} instead.",
            asyncDataTransfer.GetType());

        return new AsyncToSyncDataTransfer(asyncDataTransfer);
    }

    /// <summary>
    /// Gets whether a <see cref="IAsyncDataTransfer"/> supports a specific format.
    /// </summary>
    /// <param name="dataTransfer">The <see cref="IAsyncDataTransfer"/> instance.</param>
    /// <param name="format">The format to check.</param>
    /// <returns>true if <paramref name="format"/> is supported, false otherwise.</returns>
    public static bool Contains(this IAsyncDataTransfer dataTransfer, DataFormat format)
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
    /// Gets the list of <see cref="IAsyncDataTransferItem"/> contained in this object, filtered by a given format.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Some platforms (such as Windows and X11) may only support a single data item for all formats
    /// except <see cref="DataFormat.File"/>.
    /// </para>
    /// <para>Items returned by this property must stay valid until the <see cref="IAsyncDataTransfer"/> is disposed.</para>
    /// </remarks>
    public static IEnumerable<IAsyncDataTransferItem> GetItems(this IAsyncDataTransfer dataTransfer, DataFormat format)
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
    /// Tries to get a value for a given format from a <see cref="IAsyncDataTransfer"/>.
    /// </summary>
    /// <param name="dataTransfer">The <see cref="IAsyncDataTransfer"/> instance.</param>
    /// <param name="format">The format to retrieve.</param>
    /// <returns>A value for <paramref name="format"/>, or null if the format is not supported.</returns>
    /// <remarks>
    /// If the <see cref="IAsyncDataTransfer"/> contains several items supporting <paramref name="format"/>,
    /// the first matching one will be returned.
    /// </remarks>
    public static Task<T?> TryGetValueAsync<T>(this IAsyncDataTransfer dataTransfer, DataFormat<T> format)
        where T : class
        => dataTransfer.GetItems(format).FirstOrDefault() is { } item ?
            item.TryGetValueAsync(format) :
            Task.FromResult<T?>(null);

    /// <summary>
    /// Tries to get multiple values for a given format from a <see cref="IAsyncDataTransfer"/>.
    /// </summary>
    /// <param name="dataTransfer">The <see cref="IAsyncDataTransfer"/> instance.</param>
    /// <param name="format">The format to retrieve.</param>
    /// <returns>A list of values for <paramref name="format"/>, or null if the format is not supported.</returns>
    public static async Task<T[]?> TryGetValuesAsync<T>(this IAsyncDataTransfer dataTransfer, DataFormat<T> format)
        where T : class
    {
        List<T>? results = null;

        foreach (var item in dataTransfer.GetItems(format))
        {
            // No ConfigureAwait(false) here: we want TryGetAsync() for next items to be called on the initial thread.
            var result = await item.TryGetValueAsync(format);
            if (result is null)
                continue;

            results ??= [];
            results.Add(result);
        }

        return results?.ToArray();
    }

    /// <summary>
    /// Returns a text, if available, from a <see cref="IAsyncDataTransfer"/> instance.
    /// </summary>
    /// <param name="dataTransfer">The <see cref="IAsyncDataTransfer"/> instance.</param>
    /// <returns>A string, or null if the format isn't available.</returns>
    /// <seealso cref="DataFormat.Text"/>.
    public static Task<string?> TryGetTextAsync(this IAsyncDataTransfer dataTransfer)
        => dataTransfer.TryGetValueAsync(DataFormat.Text);

    /// <summary>
    /// Returns a file, if available, from a <see cref="IAsyncDataTransfer"/> instance.
    /// </summary>
    /// <param name="dataTransfer">The <see cref="IAsyncDataTransfer"/> instance.</param>
    /// <returns>An <see cref="IStorageItem"/> (file or folder), or null if the format isn't available.</returns>
    /// <seealso cref="DataFormat.File"/>.
    public static Task<IStorageItem?> TryGetFileAsync(this IAsyncDataTransfer dataTransfer)
        => dataTransfer.TryGetValueAsync(DataFormat.File);

    /// <summary>
    /// Returns a list of files, if available, from a <see cref="IAsyncDataTransfer"/> instance.
    /// </summary>
    /// <param name="dataTransfer">The <see cref="IAsyncDataTransfer"/> instance.</param>
    /// <returns>An array of <see cref="IStorageItem"/> (files or folders), or null if the format isn't available.</returns>
    /// <seealso cref="DataFormat.File"/>.
    public static Task<IStorageItem[]?> TryGetFilesAsync(this IAsyncDataTransfer dataTransfer)
        => dataTransfer.TryGetValuesAsync(DataFormat.File);

    /// <summary>
    /// Returns a bitmap, if available, from a <see cref="IAsyncDataTransfer"/> instance.
    /// </summary>
    /// <param name="dataTransfer">The <see cref="IAsyncDataTransfer"/> instance.</param>
    /// <returns>A <see cref="Bitmap"/>, or null if the format isn't available.</returns>
    /// <seealso cref="DataFormat.Bitmap"/>.
    public static Task<Bitmap?> TryGetBitmapAsync(this IAsyncDataTransfer dataTransfer)
        => dataTransfer.TryGetValueAsync(DataFormat.Bitmap);
}

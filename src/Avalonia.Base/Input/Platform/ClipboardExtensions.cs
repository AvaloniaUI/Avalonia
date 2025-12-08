using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace Avalonia.Input.Platform;

/// <summary>
/// Contains extension methods related to <see cref="IClipboard"/>.
/// </summary>
public static class ClipboardExtensions
{
    /// <summary>
    /// Gets a list containing the formats currently available from the clipboard.
    /// </summary>
    /// <returns>A list of formats. It can be empty if the clipboard is empty.</returns>
    public static async Task<IReadOnlyList<DataFormat>> GetDataFormatsAsync(this IClipboard clipboard)
    {
        using var dataTransfer = await clipboard.TryGetDataAsync();
        return dataTransfer is null ? [] : dataTransfer.Formats;
    }

    /// <summary>
    /// Tries to get a value for a given format from the clipboard.
    /// </summary>
    /// <param name="clipboard">The <see cref="IClipboard"/> instance.</param>
    /// <param name="format">The format to retrieve.</param>
    /// <returns>A value for <paramref name="format"/>, or null if the format is not supported.</returns>
    /// <remarks>
    /// If the <see cref="IClipboard"/> contains several items supporting <paramref name="format"/>,
    /// the first matching one will be returned.
    /// </remarks>
    public static async Task<T?> TryGetValueAsync<T>(this IClipboard clipboard, DataFormat<T> format)
        where T : class
    {
        // No ConfigureAwait(false) here: we want TryGetValueAsync() below to be called on the initial thread.
        using var dataTransfer = await clipboard.TryGetDataAsync();
        if (dataTransfer is null)
            return null;

        // However, ConfigureAwait(false) is fine here: we're not doing anything after.
        return await dataTransfer.TryGetValueAsync(format).ConfigureAwait(false);
    }

    /// <summary>
    /// Tries to get multiple values for a given format from the clipboard.
    /// </summary>
    /// <param name="clipboard">The <see cref="IClipboard"/> instance.</param>
    /// <param name="format">The format to retrieve.</param>
    /// <returns>A list of values for <paramref name="format"/>, or null if the format is not supported.</returns>
    public static async Task<T[]?> TryGetValuesAsync<T>(this IClipboard clipboard, DataFormat<T> format)
        where T : class
    {
        // No ConfigureAwait(false) here: we want TryGetValuesAsync() below to be called on the initial thread.
        using var dataTransfer = await clipboard.TryGetDataAsync();
        if (dataTransfer is null)
            return null;

        // However, ConfigureAwait(false) is fine here: we're not doing anything after.
        return await dataTransfer.TryGetValuesAsync(format).ConfigureAwait(false);
    }

    /// <summary>
    /// Places a single value on the clipboard in the specified format.
    /// </summary>
    /// <param name="clipboard">The clipboard instance.</param>
    /// <param name="format">The data format.</param>
    /// <param name="value">The value to place on the clipboard.</param>
    /// <remarks>
    /// <para>By calling this method, the clipboard will get cleared of any possible previous data.</para>
    /// <para>
    /// If <paramref name="value"/> is null, nothing will get placed on the clipboard and this method
    /// will be equivalent to <see cref="IClipboard.ClearAsync"/>.
    /// </para>
    /// </remarks>
    public static Task SetValueAsync<T>(this IClipboard clipboard, DataFormat<T> format, T? value)
        where T : class
    {
        if (value is null)
            return clipboard.ClearAsync();

        var dataTransfer = new DataTransfer();
        dataTransfer.Add(DataTransferItem.Create(format, value));
        return clipboard.SetDataAsync(dataTransfer);
    }

    /// <summary>
    /// Places multiple values on the clipboard in the specified format.
    /// </summary>
    /// <param name="clipboard">The clipboard instance.</param>
    /// <param name="format">The data format.</param>
    /// <param name="values">The values to place on the clipboard.</param>
    /// <remarks>
    /// <para>By calling this method, the clipboard will get cleared of any possible previous data.</para>
    /// <para>
    /// If <paramref name="values"/> is null or empty, nothing will get placed on the clipboard and this method
    /// will be equivalent to <see cref="IClipboard.ClearAsync"/>.
    /// </para>
    /// </remarks>
    public static Task SetValuesAsync<T>(this IClipboard clipboard, DataFormat<T> format, IEnumerable<T>? values)
        where T : class
    {
        if (values is null)
            return clipboard.ClearAsync();

        var dataTransfer = new DataTransfer();

        foreach (var value in values)
            dataTransfer.Add(DataTransferItem.Create(format, value));

        return dataTransfer.Items.Count == 0
            ? clipboard.ClearAsync()
            : clipboard.SetDataAsync(dataTransfer);
    }

    /// <summary>
    /// Returns a text, if available, from the clipboard.
    /// </summary>
    /// <param name="clipboard">The clipboard instance.</param>
    /// <returns>A string, or null if the format isn't available.</returns>
    /// <seealso cref="DataFormat.Text"/>
    public static Task<string?> TryGetTextAsync(this IClipboard clipboard)
        => clipboard.TryGetValueAsync(DataFormat.Text);

    /// <summary>
    /// Returns a file, if available, from the clipboard.
    /// </summary>
    /// <param name="clipboard">The clipboard instance.</param>
    /// <returns>An <see cref="IStorageItem"/> (file or folder), or null if the format isn't available.</returns>
    /// <seealso cref="DataFormat.File"/>.
    public static Task<IStorageItem?> TryGetFileAsync(this IClipboard clipboard)
        => clipboard.TryGetValueAsync(DataFormat.File);

    /// <summary>
    /// Returns a list of files, if available, from the clipboard.
    /// </summary>
    /// <param name="clipboard">The clipboard instance.</param>
    /// <returns>An array of <see cref="IStorageItem"/> (files or folders), or null if the format isn't available.</returns>
    /// <seealso cref="DataFormat.File"/>.
    public static Task<IStorageItem[]?> TryGetFilesAsync(this IClipboard clipboard)
        => clipboard.TryGetValuesAsync(DataFormat.File);

    /// <summary>
    /// Returns a bitmap, if available, from the clipboard.
    /// </summary>
    /// <param name="clipboard">The clipboard instance.</param>
    /// <returns>A <see cref="Bitmap"/>, or null if the format isn't available.</returns>
    /// <seealso cref="DataFormat.Bitmap"/>.
    public static Task<Bitmap?> TryGetBitmapAsync(this IClipboard clipboard)
        => clipboard.TryGetValueAsync(DataFormat.Bitmap);

    /// <summary>
    /// Places a text on the clipboard.
    /// </summary>
    /// <param name="clipboard">The clipboard instance.</param>
    /// <param name="text">The value to place on the clipboard.</param>
    /// <remarks>
    /// <para>By calling this method, the clipboard will get cleared of any possible previous data.</para>
    /// <para>
    /// If <paramref name="text"/> is null, nothing will get placed on the clipboard and this method
    /// will be equivalent to <see cref="IClipboard.ClearAsync"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="DataFormat.Text"/>
    public static Task SetTextAsync(this IClipboard clipboard, string? text)
        => clipboard.SetValueAsync(DataFormat.Text, text);

    /// <summary>
    /// Places a file on the clipboard.
    /// </summary>
    /// <param name="clipboard">The clipboard instance.</param>
    /// <param name="file">The file to place on the clipboard.</param>
    /// <remarks>
    /// <para>By calling this method, the clipboard will get cleared of any possible previous data.</para>
    /// <para>
    /// If <paramref name="file"/> is null, nothing will get placed on the clipboard and this method
    /// will be equivalent to <see cref="IClipboard.ClearAsync"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="DataFormat.File"/>
    public static Task SetFileAsync(this IClipboard clipboard, IStorageItem? file)
        => clipboard.SetValueAsync(DataFormat.File, file);

    /// <summary>
    /// Places a list of files on the clipboard.
    /// </summary>
    /// <param name="clipboard">The clipboard instance.</param>
    /// <param name="files">The files to place on the clipboard.</param>
    /// <remarks>
    /// <para>By calling this method, the clipboard will get cleared of any possible previous data.</para>
    /// <para>
    /// If <paramref name="files"/> is null or empty, nothing will get placed on the clipboard and this method
    /// will be equivalent to <see cref="IClipboard.ClearAsync"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="DataFormat.File"/>
    public static Task SetFilesAsync(this IClipboard clipboard, IEnumerable<IStorageItem>? files)
        => clipboard.SetValuesAsync(DataFormat.File, files);

    /// <summary>
    /// Places a bitmap on the clipboard.
    /// </summary>
    /// <param name="clipboard">The clipboard instance.</param>
    /// <param name="bitmap">The bitmap to place on the clipboard.</param>
    /// <remarks>
    /// <para>By calling this method, the clipboard will get cleared of any possible previous data.</para>
    /// <para>
    /// If <paramref name="bitmap"/> is null, nothing will get placed on the clipboard and this method
    /// will be equivalent to <see cref="IClipboard.ClearAsync"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="DataFormat.Bitmap"/>
    public static Task SetBitmapAsync(this IClipboard clipboard, Bitmap? bitmap)
        => clipboard.SetValueAsync(DataFormat.Bitmap, bitmap);
}

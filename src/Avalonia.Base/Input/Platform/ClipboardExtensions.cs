using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace Avalonia.Input.Platform;

/// <summary>
/// Contains extension methods related to <see cref="IClipboard"/>.
/// </summary>
public static class ClipboardExtensions
{
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
    public static async Task<T?> TryGetValueAsync<T>(this IClipboard clipboard, DataFormat format)
    {
        // No ConfigureAwait(false) here: we want TryGetAsync() below to be called on the initial thread.
        using var dataTransfer = await clipboard.TryGetDataAsync([format]);
        if (dataTransfer is null)
            return default;

        // However, ConfigureAwait(false) is fine here: we're not doing anything after.
        return await dataTransfer.TryGetValueAsync<T>(format).ConfigureAwait(false);
    }

    /// <summary>
    /// Tries to get multiple values for a given format from the clipboard.
    /// </summary>
    /// <param name="clipboard">The <see cref="IClipboard"/> instance.</param>
    /// <param name="format">The format to retrieve.</param>
    /// <returns>A list of values for <paramref name="format"/>, or null if the format is not supported.</returns>
    public static async Task<T[]?> TryGetValuesAsync<T>(this IClipboard clipboard, DataFormat format)
    {
        // No ConfigureAwait(false) here: we want TryGetAsync() below to be called on the initial thread.
        using var dataTransfer = await clipboard.TryGetDataAsync([format]);
        if (dataTransfer is null)
            return null;

        // However, ConfigureAwait(false) is fine here: we're not doing anything after.
        return await dataTransfer.TryGetValuesAsync<T>(format).ConfigureAwait(false);
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
    public static Task SetValueAsync<T>(this IClipboard clipboard, DataFormat format, T? value)
    {
        if (value is null)
            return clipboard.ClearAsync();

        var dataTransfer = new DataTransfer();
        dataTransfer.Items.Add(DataTransferItem.Create(format, value));
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
    public static Task SetValuesAsync<T>(this IClipboard clipboard, DataFormat format, IEnumerable<T>? values)
    {
        if (values is null)
            return clipboard.ClearAsync();

        var dataTransfer = new DataTransfer();

        foreach (var value in values)
            dataTransfer.Items.Add(DataTransferItem.Create(format, value));

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
        => clipboard.TryGetValueAsync<string>(DataFormat.Text);

    /// <summary>
    /// Returns a list of files, if available, from the clipboard.
    /// </summary>
    /// <param name="clipboard">The data transfer instance.</param>
    /// <returns>An array of <see cref="IStorageItem"/> (files or folders), or null if the format isn't available.</returns>
    /// <seealso cref="DataFormat.File"/>.
    public static Task<IStorageItem[]?> TryGetFilesAsync(this IClipboard clipboard)
        => clipboard.TryGetValuesAsync<IStorageItem>(DataFormat.File);

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
    /// Places a list of files on the clipboard.
    /// </summary>
    /// <param name="clipboard">The clipboard instance.</param>
    /// <param name="files"></param>
    /// <remarks>
    /// <para>By calling this method, the clipboard will get cleared of any possible previous data.</para>
    /// <para>
    /// If <paramref name="files"/> is null, nothing will get placed on the clipboard and this method
    /// will be equivalent to <see cref="IClipboard.ClearAsync"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="DataFormat.File"/>
    public static Task SetFilesAsync(this IClipboard clipboard, IEnumerable<IStorageItem> files)
        => clipboard.SetValuesAsync(DataFormat.File, files);
}

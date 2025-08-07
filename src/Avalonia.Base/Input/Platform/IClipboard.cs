using System;
using System.Threading.Tasks;
using Avalonia.Metadata;

namespace Avalonia.Input.Platform
{
    /// <summary>
    /// Represents the system clipboard.
    /// </summary>
    [NotClientImplementable]
    public interface IClipboard
    {
        // TODO12: remove, ClipboardExtensions.TryGetTextAsync exists
        /// <summary>
        /// Returns a string containing the text data on the clipboard.
        /// </summary>
        /// <returns>A string containing text data, or null if no corresponding text data is available.</returns>
        [Obsolete($"Use {nameof(ClipboardExtensions)}.{nameof(ClipboardExtensions.TryGetTextAsync)} instead")]
        Task<string?> GetTextAsync();

        // TODO12: remove, ClipboardExtensions.SetTextAsync exists
        /// <summary>
        /// Places a text on the clipboard.
        /// </summary>
        /// <param name="text">The text value to set.</param>
        /// <remarks>
        /// <para>By calling this method, the clipboard will get cleared of any possible previous data.</para>
        /// <para>
        /// If <paramref name="text"/> is null or empty, nothing will get placed on the clipboard and this method
        /// will be equivalent to <see cref="ClearAsync"/>.
        /// </para>
        /// </remarks>
        Task SetTextAsync(string? text);

        /// <summary>
        /// Clears any data from the system clipboard.
        /// </summary>
        Task ClearAsync();

        /// <summary>
        /// Places a specified non-persistent data object on the system Clipboard.
        /// </summary>
        /// <param name="data">A data object (an object that implements <see cref="IDataObject"/>) to place on the system Clipboard.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="data"/> is null.</exception>
        [Obsolete($"Use {nameof(SetDataAsync)} instead.")]
        Task SetDataObjectAsync(IDataObject data);

        /// <summary>
        /// Places a data object on the clipboard.
        /// The data object is responsible for providing supported formats and data upon request.
        /// </summary>
        /// <param name="dataTransfer">The data object to set on the clipboard.</param>
        /// <remarks>
        /// <para>
        /// If <paramref name="dataTransfer"/> is null, nothing will get placed on the clipboard and this method
        /// will be equivalent to <see cref="ClearAsync"/>.
        /// </para>
        /// <para>
        /// The <see cref="IAsyncDataTransfer"/> must NOT be disposed by the caller after this call.
        /// The clipboard will dispose of it automatically when it becomes unused.
        /// </para>
        /// </remarks>
        Task SetDataAsync(IAsyncDataTransfer? dataTransfer);

        /// <summary>
        /// Permanently adds the data that is on the Clipboard so that it is available after the data's original application closes.
        /// </summary>
        /// <returns></returns>
        /// <remarks>This method is only supported on the Windows platform. This method will do nothing on other platforms.</remarks>
        Task FlushAsync();

        /// <summary>
        /// Get list of available Clipboard format.
        /// </summary>
        [Obsolete($"Use {nameof(ClipboardExtensions.GetDataFormatsAsync)} instead.")]
        Task<string[]> GetFormatsAsync();

        /// <summary>
        /// Retrieves data in a specified format from the Clipboard.
        /// </summary>
        /// <param name="format">A string that specifies the format of the data to retrieve. For a set of predefined data formats, see the <see cref="DataFormats"/> class.</param>
        /// <returns></returns>
        [Obsolete($"Use {nameof(TryGetDataAsync)} instead.")]
        Task<object?> GetDataAsync(string format);

        /// <summary>
        /// Retrieves data from the clipboard.
        /// </summary>
        /// <remarks>
        /// <para>The returned <see cref="IAsyncDataTransfer"/> MUST be disposed by the caller.</para>
        /// <para>
        /// Avoid storing the returned <see cref="IAsyncDataTransfer"/> instance for a long time:
        /// use it, then dispose it as soon as possible.
        /// </para>
        /// </remarks>
        Task<IAsyncDataTransfer?> TryGetDataAsync();

        /// <summary>
        /// If clipboard contains the IDataObject that was set by a previous call to <see cref="SetDataObjectAsync(Avalonia.Input.IDataObject)"/>,
        /// return said IDataObject instance. Otherwise, return null.
        /// Note that not every platform supports that method, on unsupported platforms this method will always return
        /// null
        /// </summary>
        /// <returns></returns>
        [Obsolete($"Use {nameof(TryGetInProcessDataAsync)} instead.")]
        Task<IDataObject?> TryGetInProcessDataObjectAsync();

        /// <summary>
        /// Retrieves the exact instance of a <see cref="IAsyncDataTransfer"/> previously placed on the clipboard
        /// by <see cref="SetDataAsync"/>, if any.
        /// </summary>
        /// <returns>The data transfer object if present, null otherwise.</returns>
        /// <remarks>
        /// <para>This method cannot be used to retrieve a <see cref="IAsyncDataTransfer"/> set by another process.</para>
        /// <para>This method is only supported on Windows, macOS and X11 platforms. Other platforms will always return null.</para>
        /// <para>
        /// Contrary to <see cref="TryGetDataAsync"/>, the returned <see cref="IAsyncDataTransfer"/> must NOT be disposed
        /// by the caller since it's still owned by the clipboard.
        /// </para>
        /// </remarks>
        Task<IAsyncDataTransfer?> TryGetInProcessDataAsync();
    }
}

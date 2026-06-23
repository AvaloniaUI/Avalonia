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
        /// <summary>
        /// Clears any data from the system clipboard.
        /// </summary>
        Task ClearAsync();

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

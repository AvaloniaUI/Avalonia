using System;
using System.Collections.Generic;
using Avalonia.Input.Platform;

namespace Avalonia.Input;

/// <summary>
/// Represents an object providing a list of <see cref="IAsyncDataTransferItem"/> usable by the clipboard.
/// </summary>
/// <seealso cref="DataTransfer"/>
/// <remarks>
/// <list type="bullet">
/// <item>
/// When an implementation of this interface is put into the clipboard using <see cref="IClipboard.SetDataAsync"/>,
/// it must NOT be disposed by the caller. The system will dispose of it automatically when it becomes unused.
/// </item>
/// <item>
/// When an implementation of this interface is returned from the clipboard via <see cref="IClipboard.TryGetDataAsync"/>,
/// it MUST be disposed the caller.
/// </item>
/// <item>
/// This interface is mostly used during clipboard operations. However, several platforms only support synchronous
/// clipboard manipulation and will try to use <see cref="IDataTransfer"/> if the underlying type also implements it.
/// For this reason, custom implementations should ideally implement both <see cref="IAsyncDataTransfer"/> and
/// <see cref="IDataTransfer"/>.
/// </item>
/// </list>
/// </remarks>
public interface IAsyncDataTransfer : IDisposable
{
    /// <summary>
    /// Gets the formats supported by a <see cref="IAsyncDataTransfer"/>.
    /// </summary>
    IReadOnlyList<DataFormat> Formats { get; }

    /// <summary>
    /// Gets a list of <see cref="IAsyncDataTransferItem"/> contained in this object.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Some platforms (such as Windows and X11) may only support a single data item for all formats
    /// except <see cref="DataFormat.File"/>.
    /// </para>
    /// <para>Items returned by this property must stay valid until the <see cref="IAsyncDataTransfer"/> is disposed.</para>
    /// </remarks>
    IReadOnlyList<IAsyncDataTransferItem> Items { get; }
}

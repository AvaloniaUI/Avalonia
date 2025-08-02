using System;
using System.Collections.Generic;
using Avalonia.Input.Platform;

namespace Avalonia.Input;

/// <summary>
/// Represents an object providing a list of <see cref="IDataTransferItem"/> usable by the clipboard
/// or during a drag and drop operation.
/// </summary>
/// <seealso cref="DataTransfer"/>
/// <remarks>
/// <list type="bullet">
/// <item>
/// When an implementation of this interface is put into the clipboard using <see cref="IClipboard.SetDataAsync"/>,
/// it must NOT be disposed by the caller. The system will dispose of it automatically when it becomes unused.
/// </item>
/// <item>
/// When an implementation of this interface is used as a drag source using <see cref="DragDrop.DoDragDropAsync"/>,
/// it must NOT be disposed by the caller. The system will dispose of it automatically when the drag operation completes.
/// </item>
/// <item>
/// When an implementation of this interface is returned from the clipboard via <see cref="IClipboard.TryGetDataAsync"/>,
/// it MUST be disposed the caller.
/// </item>
/// </list>
/// </remarks>
public interface IDataTransfer : IDisposable
{
    /// <summary>
    /// Gets the formats supported by a <see cref="IDataTransfer"/>.
    /// </summary>
    IReadOnlyList<DataFormat> Formats { get; }

    /// <summary>
    /// Gets the list of <see cref="IDataTransferItem"/> contained in this object.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Some platforms (such as Windows and X11) may only support a single data item for all formats
    /// except <see cref="DataFormat.File"/>.
    /// </para>
    /// <para>Items returned by this property must stay valid until the <see cref="IDataTransfer"/> is disposed.</para>
    /// </remarks>
    IReadOnlyList<IDataTransferItem> Items { get; }
}

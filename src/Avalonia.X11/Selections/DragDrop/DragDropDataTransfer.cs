using System;
using Avalonia.Input;
using Avalonia.Input.Platform;

namespace Avalonia.X11.Selections.DragDrop;

/// <summary>
/// Implementation of <see cref="IDataTransfer"/> for data being dragged into an Avalonia window via Xdnd.
/// </summary>
internal sealed class DragDropDataTransfer(
    DragDropDataReader reader,
    DataFormat[] dataFormats,
    IntPtr sourceWindow,
    IntPtr targetWindow,
    IInputRoot inputRoot)
    : PlatformDataTransfer
{
    public IntPtr SourceWindow { get; } = sourceWindow;

    public IntPtr TargetWindow { get; } = targetWindow;

    public IInputRoot InputRoot { get; } = inputRoot;

    public Point? LastPosition { get; set; }

    public IntPtr LastTimestamp { get; set; }

    public DragDropEffects ResultEffects { get; set; } = DragDropEffects.None;

    public bool Dropped { get; set; }

    protected override DataFormat[] ProvideFormats()
        => dataFormats;

    protected override PlatformDataTransferItem[] ProvideItems()
        => reader.CreateItems();

    public override void Dispose()
        => reader.Dispose();
}

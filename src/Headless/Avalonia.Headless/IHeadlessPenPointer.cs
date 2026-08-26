using System;
using Avalonia.Metadata;

namespace Avalonia.Headless;

/// <summary>
/// Represents a pen/stylus device simulated on a headless window/toplevel.
/// Use <see cref="HeadlessWindowExtensions.PenDown"/>, <see cref="HeadlessWindowExtensions.PenMove"/>
/// and <see cref="HeadlessWindowExtensions.PenUp"/> to drive the pen.
/// </summary>
/// <remarks>
/// Each pen pointer represents a separate pen device.
/// Disposing the pen pointer makes the pen leave the toplevel, cancelling the contact if the tip is still pressed.
/// </remarks>
[NotClientImplementable]
public interface IHeadlessPenPointer : IDisposable
{
}

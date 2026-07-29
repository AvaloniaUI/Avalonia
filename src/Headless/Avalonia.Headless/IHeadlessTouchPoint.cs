using System;

namespace Avalonia.Headless;

/// <summary>
/// Represents an active touch contact simulated on a headless window/toplevel.
/// Use <see cref="HeadlessWindowExtensions.TouchMove"/> and <see cref="HeadlessWindowExtensions.TouchEnd"/> to drive the contact.
/// </summary>
/// <remarks>
/// Disposing the touch point cancels the contact if it hasn't been released with <see cref="HeadlessWindowExtensions.TouchEnd"/> yet.
/// </remarks>
public interface IHeadlessTouchPoint : IDisposable
{
}

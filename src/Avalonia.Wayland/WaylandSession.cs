using System;

// ReSharper disable once CheckNamespace
namespace Avalonia;

/// <summary>
/// Names windows in the compositor session enabled by <see cref="WaylandPlatformOptions.UseSessionManagement"/>.
/// </summary>
public static class WaylandSession
{
    [ThreadStatic] private static string? t_name;

    /// <summary>
    /// Names the next window constructed on the current thread before the returned scope is disposed.
    /// The compositor restores the state it kept for that name, and keeps the window's state under it from then on.
    /// </summary>
    /// <remarks>
    /// The name is sent before the window's surface is first committed, which happens while the window is
    /// constructed, so the scope must wrap the construction itself. Names must be unique among the
    /// application's open windows.
    /// </remarks>
    /// <param name="name">Identifies the window across launches.</param>
    public static IDisposable Name(string name)
    {
        var previous = t_name;
        t_name = name ?? throw new ArgumentNullException(nameof(name));
        return new Scope(previous);
    }

    internal static string? Take()
    {
        var name = t_name;
        t_name = null;
        return name;
    }

    private sealed class Scope(string? previous) : IDisposable
    {
        public void Dispose() => t_name = previous;
    }
}

#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace Avalonia.Headless.UnitTests;

internal static class AssertHelper
{
    public static void True(bool condition, string? message = null)
    {
#if NUNIT
        Assert.That(condition, Is.True, message);
#elif XUNIT
        Assert.True(condition, message);
#endif
    }

    public static void False(bool condition, string? message = null)
    {
#if NUNIT
        Assert.That(condition, Is.False, message);
#elif XUNIT
        Assert.False(condition, message);
#endif
    }

    public static void Null(object? value)
    {
#if NUNIT
        Assert.That(value, Is.Null);
#elif XUNIT
        Assert.Null(value);
#endif
    }

    public static void NotNull([NotNull] object? value)
    {
#if NUNIT
        Assert.That(value, Is.Not.Null);
#elif XUNIT
        Assert.NotNull(value);
#endif
        // NUnit doesn't suppress CS8777 warning on its own
#pragma warning disable CS8777 // Parameter must have a non-null value when exiting.
    }
#pragma warning restore CS8777 // Parameter must have a non-null value when exiting.

    public static void Equal<T>(T expected, T actual)
    {
#if NUNIT
        Assert.That(expected, Is.EqualTo(actual));
#elif XUNIT
        Assert.Equal(expected, actual);
#endif
    }

    public static void Same(object? expected, object? actual)
    {
#if NUNIT
        Assert.That(expected, Is.SameAs(actual));
#elif XUNIT
        Assert.Same(expected, actual);
#endif
    }

    public static void NotSame(object? expected, object? actual)
    {
#if NUNIT
        Assert.That(expected, Is.Not.SameAs(actual));
#elif XUNIT
        Assert.NotSame(expected, actual);
#endif
    }

}

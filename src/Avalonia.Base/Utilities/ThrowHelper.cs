using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Avalonia.Utilities;

/// <summary>
/// Helper method to help inlining methods that do a throw check.
/// Equivalent of .NET6+ ArgumentNullException.ThrowIfNull() for netstandard2.0+
/// </summary>
internal class ThrowHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNull([NotNull] object? argument, string paramName)
    {
        if (argument is null)
        {
            ThrowArgumentNullException(paramName);
        }
    }

    [DoesNotReturn]
    private static void ThrowArgumentNullException(string paramName) => throw new ArgumentNullException(paramName);
}

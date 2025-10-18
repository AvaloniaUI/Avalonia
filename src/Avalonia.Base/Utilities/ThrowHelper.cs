using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Avalonia.Utilities;

/// <summary>
/// Helper methods to help inlining methods that do a throw check.
/// </summary>
internal static class ThrowHelper
{
    /// <summary>
    /// Equivalent of .NET6+ ArgumentNullException.ThrowIfNull().
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNull([NotNull] object? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        if (argument is null)
        {
            ThrowArgumentNullException(paramName);
        }

        [DoesNotReturn]
        static void ThrowArgumentNullException(string? paramName) => throw new ArgumentNullException(paramName);
    }

    /// <summary>
    /// Equivalent of .NET8+ ArgumentException.ThrowIfNullOrEmpty().
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNullOrEmpty([NotNull] string? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        if (string.IsNullOrEmpty(argument))
        {
            ThrowNullOrEmptyException(argument, paramName);
        }

        [DoesNotReturn]
        static void ThrowNullOrEmptyException(string? argument, string? paramName)
        {
            ThrowIfNull(argument, paramName);
            throw new ArgumentException("Empty string", paramName);
        }
    }

}

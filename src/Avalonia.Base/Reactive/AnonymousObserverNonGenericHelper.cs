using System;

namespace Avalonia.Reactive;

/// <summary>
/// Contains fields for <see cref="AnonymousObserver{T}"/> that aren't using generic arguments.
/// Separated to avoid unnecessary generic instantiations.
/// </summary>
internal static class AnonymousObserverNonGenericHelper
{
    public static readonly Action<Exception> ThrowsOnError = ex => throw ex;
    public static readonly Action NoOpCompleted = () => { };
}

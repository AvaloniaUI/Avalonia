using System;
using Avalonia.Reactive;

namespace Avalonia.PropertyStore;

/// <summary>
/// Contains fields for <see cref="BindingEntryBase{TValue,TSource}"/> that aren't using generic arguments.
/// Separated to avoid unnecessary generic instantiations.
/// </summary>
internal static class BindingEntryBaseNonGenericHelper
{
    public static readonly IDisposable Creating = Disposable.Empty;
    public static readonly IDisposable CreatingQuiet = Disposable.Create(() => { });
}

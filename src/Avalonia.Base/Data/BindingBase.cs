using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Data.Core;
using Avalonia.Diagnostics;

namespace Avalonia.Data;

/// <summary>
/// Base class for the various types of binding supported by Avalonia.
/// </summary>
public abstract class BindingBase : IBinding2
{
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = TrimmingMessages.TypeConversionSupressWarningMessage)]
    [Obsolete(ObsoletionMessages.MayBeRemovedInAvalonia12)]
    public abstract InstancedBinding? Initiate(
            AvaloniaObject target,
            AvaloniaProperty? targetProperty,
            object? anchor = null,
            bool enableDataValidation = false);

    internal abstract BindingExpressionBase Instance(
        AvaloniaObject target,
        AvaloniaProperty? targetProperty,
        object? anchor);

    BindingExpressionBase IBinding2.Instance(AvaloniaObject target, AvaloniaProperty? targetProperty, object? anchor)
    {
        return Instance(target, targetProperty, anchor);
    }
}

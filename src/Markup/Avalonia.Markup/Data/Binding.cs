using System;
using System.Diagnostics.CodeAnalysis;

namespace Avalonia.Data;

/// <summary>
/// Provides limited compatibility with the 11.x Binding class. Use <see cref="ReflectionBinding"/>
/// for new code.
/// </summary>
[RequiresUnreferencedCode(TrimmingMessages.ReflectionBindingRequiresUnreferencedCodeMessage)]
public class Binding : ReflectionBinding
{
    [Obsolete("Use ReflectionBinding.")]
    public Binding() { }

    [Obsolete("Use ReflectionBinding.")]
    public Binding(string path) : base(path) { }

    [Obsolete("Use ReflectionBinding.")]
    public Binding(string path, BindingMode mode) : base(path, mode) { }
}

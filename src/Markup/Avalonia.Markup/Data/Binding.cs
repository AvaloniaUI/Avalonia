using System;
using System.Diagnostics.CodeAnalysis;

namespace Avalonia.Data;

/// <summary>
/// Provides limited compatibility with the 11.x Binding class. Use <see cref="ReflectionBinding"/>
/// for new code.
/// </summary>
[RequiresUnreferencedCode(TrimmingMessages.ReflectionBindingRequiresUnreferencedCodeMessage)]
#if NET8_0_OR_GREATER
    [RequiresDynamicCode(TrimmingMessages.ReflectionBindingRequiresDynamicCodeMessage)]
#endif
public class Binding : ReflectionBinding
{
    public Binding() { }

    public Binding(string path) : base(path) { }

    public Binding(string path, BindingMode mode) : base(path, mode) { }
}

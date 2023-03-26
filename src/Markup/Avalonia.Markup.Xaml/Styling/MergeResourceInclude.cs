using System;
using System.Diagnostics.CodeAnalysis;

namespace Avalonia.Markup.Xaml.Styling;

/// <summary>
/// Loads a resource dictionary from a specified URL.
/// </summary>
/// <remarks>
/// If used from the XAML code, it is merged into the parent dictionary in the compile time. 
/// When used in runtime, this type behaves like <see cref="ResourceInclude"/>.  
/// </remarks>
[RequiresUnreferencedCode(TrimmingMessages.StyleResourceIncludeRequiresUnreferenceCodeMessage)]
public class MergeResourceInclude : ResourceInclude
{
    public MergeResourceInclude(Uri? baseUri) : base(baseUri)
    {
    }

    public MergeResourceInclude(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}

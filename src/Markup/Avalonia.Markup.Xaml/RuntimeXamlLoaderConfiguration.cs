using System;
using System.Reflection;

namespace Avalonia.Markup.Xaml;

public class RuntimeXamlLoaderConfiguration
{
    /// <summary>
    /// The URI of the XAML being loaded.
    /// </summary>
    public Uri? BaseUri { get; set; }

    /// <summary>
    /// Default assembly for clr-namespace:.
    /// </summary>
    public Assembly LocalAssembly { get; set; }
            
    /// <summary>
    /// The optional instance into which the XAML should be loaded.
    /// </summary>
    public object? RootInstance { get; set; }
            
    /// <summary>
    /// Defines is CompiledBinding should be used by default.
    /// Default is 'false'.
    /// </summary>
    public bool UseCompiledBindingsByDefault { get; set; } = false;

    /// <summary>
    /// Indicates whether the XAML is being loaded in design mode.
    /// Default is 'false'.
    /// </summary>
    public bool DesignMode { get; set; } = false;
}

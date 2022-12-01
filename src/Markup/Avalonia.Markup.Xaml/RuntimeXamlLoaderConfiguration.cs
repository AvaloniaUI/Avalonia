using System.Reflection;

namespace Avalonia.Markup.Xaml;

#nullable enable

public class RuntimeXamlLoaderConfiguration
{
    /// <summary>
    /// Default assembly for clr-namespace:.
    /// </summary>
    public Assembly? LocalAssembly { get; set; }

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

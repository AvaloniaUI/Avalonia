using System;
using System.Reflection;

namespace Avalonia.Markup.Xaml;

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

    /// <summary>
    /// XAML diagnostics handler.
    /// </summary>
    /// <returns>
    /// Defines if any diagnostic severity should be overriden.
    /// Note, severity cannot be set lower than minimal for specific diagnostic.
    /// </returns>
    public XamlDiagnosticFunc? DiagnosticHandler { get; set; }

    /// <summary>
    /// Delegate for <see cref="RuntimeXamlLoaderConfiguration.DiagnosticHandler"/> property.
    /// </summary>
    public delegate RuntimeXamlDiagnosticSeverity XamlDiagnosticFunc(RuntimeXamlDiagnostic diagnostic);
}

public enum RuntimeXamlDiagnosticSeverity
{
    /// <summary>
    /// Something that is an issue, as determined by some authority,
    /// but is not surfaced through normal means.
    /// There may be different mechanisms that act on these issues.
    /// </summary>
    Info = 1,

    /// <summary>
    /// Diagnostic is reported as a warning.
    /// </summary>
    Warning,

    /// <summary>
    /// Diagnostic is reported as an error.
    /// Compilation process is continued until the end of the parsing and transforming stage, throwing an aggregated exception of all errors.
    /// </summary>
    Error,
    
    /// <summary>
    /// Diagnostic is reported as an fatal error.
    /// Compilation process is stopped right after this error.
    /// </summary>
    Fatal
}

public record RuntimeXamlDiagnostic(
    string Id,
    RuntimeXamlDiagnosticSeverity Severity,
    string Title,
    int? LineNumber,
    int? LinePosition)
{
    public string? Document { get; set; }
}

using System.Collections.Generic;

namespace Avalonia.OpenGL;

/// <summary>
/// Options for <see cref="OpenGlCompositionInterop.TryCreateCompatibleGlContextAsync"/>.
/// </summary>
public class CompositionGlContextOptions
{
    /// <summary>
    /// The list of desired OpenGL(ES) versions in order of preference.
    /// </summary>
    public IReadOnlyList<GlVersion>? GlProfiles { get; set; }
}

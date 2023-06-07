using System.Collections.Generic;

namespace Avalonia.OpenGL;

public interface IPlatformGraphicsOpenGlContextFactory
{
    IGlContext CreateContext(IEnumerable<GlVersion>? versions);
}

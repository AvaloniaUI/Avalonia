using Avalonia.Metadata;

namespace Avalonia.Platform;

/// <summary>
/// Builds a geometry using drawing commands.
/// </summary>
[Unstable]
public interface IStreamGeometryBuilder : IGeometryContext
{
    /// <summary>
    /// Creates the geometry described by the commands issued so far.
    /// </summary>
    /// <returns>The resulting geometry, which is immutable.</returns>
    /// <remarks>
    /// This method can only be called once: the context can't be used afterwards, except to be disposed.
    /// </remarks>
    IGeometryImpl ToGeometry();
}

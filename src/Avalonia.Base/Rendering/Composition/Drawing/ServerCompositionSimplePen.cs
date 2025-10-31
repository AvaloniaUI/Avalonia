using Avalonia.Media;

namespace Avalonia.Rendering.Composition.Server;

internal partial class ServerCompositionSimplePen : IPen
{
    IDashStyle? IPen.DashStyle => DashStyle;

    /// <inheritdoc/>
    public override void Dispose()
    {
        // Dispose of the brush resource, this will remove the pen from the brush observers.
        // This was causing the pen to be retained in memory by long lived brush resources (e.g. those defined in
        // the theme or app resources), hence was causing memory leaks; see Issue #16451
        SetValue(ref _brush, null!);
        base.Dispose();
    }
}

using Avalonia.Media;

namespace Avalonia.Rendering.Composition.Server;

internal partial class ServerCompositionSimplePen : IPen
{
    IDashStyle? IPen.DashStyle => DashStyle;

    /// <inheritdoc/>
    public override void Dispose()
    {
        // Remove the pen from the brush observers.
        // Without this, the pen was being retained in memory by long lived brush resources (e.g. those defined in
        // the theme or app resources), hence was causing memory leaks; see Issue #16451
        RemoveObserversFromProperty(ref _brush);
        _brush = null;
        base.Dispose();
    }
}

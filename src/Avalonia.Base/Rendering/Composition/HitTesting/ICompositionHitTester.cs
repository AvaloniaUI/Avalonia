using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.HitTesting;

internal interface ICompositionHitTester<T>
{
    static abstract T Transform(T input, in Matrix matrix);

    static abstract bool HitTest(CompositionVisual visual, T input);

    static abstract bool TransformedSubTreeBoundsMatch(LtrbRect bounds, T input);

    static abstract bool AabbNodeBoundsMatch(LtrbRect bounds, T input);

    static abstract bool ClippedBoundsMatch(CompositionVisual visual, T input);

    static abstract bool ClipMatches(IGeometryImpl clip, T input);
}

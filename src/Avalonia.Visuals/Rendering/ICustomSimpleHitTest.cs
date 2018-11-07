namespace Avalonia.Rendering
{
    /// <summary>
    /// An interface to allow non-templated controls to customize their hit-testing
    /// when using a renderer with a simple hit-testing algorithm without a scene graph,
    /// such as <see cref="ImmediateRenderer" />
    /// </summary>
    public interface ICustomSimpleHitTest
    {
        bool HitTest(Point point);
    }
}

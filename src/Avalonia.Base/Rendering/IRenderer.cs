using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Metadata;
using Avalonia.Rendering.Composition;

namespace Avalonia.Rendering
{
    /// <summary>
    /// Defines the interface for a renderer.
    /// </summary>
    [PrivateApi]
    public interface IRenderer : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether the renderer should draw specific diagnostics.
        /// </summary>
        RendererDiagnostics Diagnostics { get; }

        /// <summary>
        /// Raised when a portion of the scene has been invalidated.
        /// </summary>
        /// <remarks>
        /// Indicates that the underlying low-level scene information has been updated. Used to
        /// signal that an update to the current pointer-over state may be required.
        /// </remarks>
        event EventHandler<SceneInvalidatedEventArgs>? SceneInvalidated;

        /// <summary>
        /// Mark a visual as dirty and needing re-rendering.
        /// </summary>
        /// <param name="visual">The visual.</param>
        void AddDirty(Visual visual);
        
        /// <summary>
        /// Informs the renderer that the z-ordering of a visual's children has changed.
        /// </summary>
        /// <param name="visual">The visual.</param>
        void RecalculateChildren(Visual visual);

        /// <summary>
        /// Called when a resize notification is received by the control being rendered.
        /// </summary>
        /// <param name="size">The new size of the window.</param>
        void Resized(Size size);

        /// <summary>
        /// Called when a paint notification is received by the control being rendered.
        /// </summary>
        /// <param name="rect">The dirty rectangle.</param>
        void Paint(Rect rect);

        /// <summary>
        /// Starts the renderer.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the renderer.
        /// </summary>
        void Stop();

        /// <summary>
        /// Attempts to query for a feature from the platform render interface
        /// </summary>
        public ValueTask<object?> TryGetRenderInterfaceFeature(Type featureType);
    }
    
    internal interface IRendererWithCompositor : IRenderer
    {
        /// <summary>
        /// The associated <see cref="Avalonia.Rendering.Composition.Compositor"/> object
        /// </summary>
        Compositor Compositor { get; }
    }

    [PrivateApi]
    public interface IHitTester
    {
        /// <summary>
        /// Hit tests a location to find the visuals at the specified point.
        /// </summary>
        /// <param name="p">The point, in client coordinates.</param>
        /// <param name="root">The root of the subtree to search.</param>
        /// <param name="filter">
        /// A filter predicate. If the predicate returns false then the visual and all its
        /// children will be excluded from the results.
        /// </param>
        /// <returns>The visuals at the specified point, topmost first.</returns>
        IEnumerable<Visual> HitTest(Point p, Visual root, Func<Visual, bool> filter);

        /// <summary>
        /// Hit tests a location to find first visual at the specified point.
        /// </summary>
        /// <param name="p">The point, in client coordinates.</param>
        /// <param name="root">The root of the subtree to search.</param>
        /// <param name="filter">
        /// A filter predicate. If the predicate returns false then the visual and all its
        /// children will be excluded from the results.
        /// </param>
        /// <returns>The visual at the specified point, topmost first.</returns>
        Visual? HitTestFirst(Point p, Visual root, Func<Visual, bool> filter);
    }
}

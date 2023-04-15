using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Collections.Pooled;
using Avalonia.Media;
using Avalonia.VisualTree;

// Special license applies <see href="https://raw.githubusercontent.com/AvaloniaUI/Avalonia/master/src/Avalonia.Base/Rendering/Composition/License.md">License.md</see>

namespace Avalonia.Rendering.Composition;

/// <summary>
/// A renderer that utilizes <see cref="Avalonia.Rendering.Composition.Compositor"/> to render the visual tree 
/// </summary>
public class CompositingRenderer : IRendererWithCompositor
{
    private readonly IRenderRoot _root;
    private readonly Compositor _compositor;
    private readonly CompositionDrawingContext _recorder = new();
    private readonly DrawingContext _recordingContext;
    private readonly HashSet<Visual> _dirty = new();
    private readonly HashSet<Visual> _recalculateChildren = new();
    private readonly Action _update;

    private bool _queuedUpdate;
    private bool _updating;
    private bool _isDisposed;

    internal CompositionTarget CompositionTarget { get; }
    
    /// <summary>
    /// Asks the renderer to only draw frames on the render thread. Makes Paint to wait until frame is rendered.
    /// </summary>
    public bool RenderOnlyOnRenderThread { get; set; } = true;

    /// <inheritdoc/>
    public RendererDiagnostics Diagnostics { get; }

    /// <inheritdoc />
    public Compositor Compositor => _compositor;

    /// <summary>
    /// Initializes a new instance of <see cref="CompositingRenderer"/>
    /// </summary>
    /// <param name="root">The render root using this renderer.</param>
    /// <param name="compositor">The associated compositors.</param>
    /// <param name="surfaces">
    /// A function returning the list of native platform's surfaces that can be consumed by rendering subsystems.
    /// </param>
    public CompositingRenderer(IRenderRoot root, Compositor compositor, Func<IEnumerable<object>> surfaces)
    {
        _root = root;
        _compositor = compositor;
        _recordingContext = _recorder;
        CompositionTarget = compositor.CreateCompositionTarget(surfaces);
        CompositionTarget.Root = ((Visual)root).AttachToCompositor(compositor);
        _update = Update;
        Diagnostics = new RendererDiagnostics();
        Diagnostics.PropertyChanged += OnDiagnosticsPropertyChanged;
    }

    private void OnDiagnosticsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(RendererDiagnostics.DebugOverlays):
                CompositionTarget.DebugOverlays = Diagnostics.DebugOverlays;
                break;
            case nameof(RendererDiagnostics.LastLayoutPassTiming):
                CompositionTarget.LastLayoutPassTiming = Diagnostics.LastLayoutPassTiming;
                break;
        }
    }

    /// <inheritdoc/>
    public event EventHandler<SceneInvalidatedEventArgs>? SceneInvalidated;

    private void QueueUpdate()
    {
        if(_queuedUpdate)
            return;
        _queuedUpdate = true;
        _compositor.RequestCompositionUpdate(_update);
    }
    
    /// <inheritdoc/>
    public void AddDirty(Visual visual)
    {
        if (_isDisposed)
            return;
        if (_updating)
            throw new InvalidOperationException("Visual was invalidated during the render pass");
        _dirty.Add(visual);
        QueueUpdate();
    }

    /// <inheritdoc/>
    public IEnumerable<Visual> HitTest(Point p, Visual? root, Func<Visual, bool>? filter)
    {
        CompositionVisual? rootVisual = null;
        if (root != null)
        {
            if (root.CompositionVisual == null)
                yield break;
            rootVisual = root.CompositionVisual;
        }
        
        Func<CompositionVisual, bool>? f = null;
        if (filter != null)
            f = v =>
            {
                if (v is CompositionDrawListVisual dlv)
                    return filter(dlv.Visual);
                return true;
            };

        var res = CompositionTarget.TryHitTest(p, rootVisual, f);
        if(res == null)
            yield break;
        foreach(var v in res)
        {
            if (v is CompositionDrawListVisual dv)
            {
                if (filter == null || filter(dv.Visual))
                    yield return dv.Visual;
            }
        }
    }

    /// <inheritdoc/>
    public Visual? HitTestFirst(Point p, Visual root, Func<Visual, bool>? filter)
    {
        // TODO: Optimize
        return HitTest(p, root, filter).FirstOrDefault();
    }

    /// <inheritdoc/>
    public void RecalculateChildren(Visual visual)
    {
        if (_isDisposed)
            return;
        if (_updating)
            throw new InvalidOperationException("Visual was invalidated during the render pass");
        _recalculateChildren.Add(visual);
        QueueUpdate();
    }

    private static void SyncChildren(Visual v)
    {
        //TODO: Optimize by moving that logic to Visual itself
        if(v.CompositionVisual == null)
            return;
        var compositionChildren = v.CompositionVisual.Children;
        var visualChildren = (AvaloniaList<Visual>)v.GetVisualChildren();
        
        PooledList<(Visual visual, int index)>? sortedChildren = null;
        if (v.HasNonUniformZIndexChildren && visualChildren.Count > 1)
        {
            sortedChildren = new (visualChildren.Count);
            for (var c = 0; c < visualChildren.Count; c++) 
                sortedChildren.Add((visualChildren[c], c));
            
            // Regular Array.Sort is unstable, we need to provide indices as well to avoid reshuffling elements.
            sortedChildren.Sort(static (lhs, rhs) =>
            {
                var result = lhs.visual.ZIndex.CompareTo(rhs.visual.ZIndex);
                return result == 0 ? lhs.index.CompareTo(rhs.index) : result;
            });
        }
        
        var childVisual = v.ChildCompositionVisual;
        
        // Check if the current visual somehow got migrated to another compositor
        if (childVisual != null && childVisual.Compositor != v.CompositionVisual.Compositor)
            childVisual = null;
        
        var expectedCount = visualChildren.Count;
        if (childVisual != null)
            expectedCount++;
        
        if (compositionChildren.Count == expectedCount)
        {
            bool mismatch = false;
            if (sortedChildren != null)
                for (var c = 0; c < visualChildren.Count; c++)
                {
                    if (!ReferenceEquals(compositionChildren[c], sortedChildren[c].visual.CompositionVisual))
                    {
                        mismatch = true;
                        break;
                    }
                }
            else
                for (var c = 0; c < visualChildren.Count; c++)
                    if (!ReferenceEquals(compositionChildren[c], visualChildren[c].CompositionVisual))
                    {
                        mismatch = true;
                        break;
                    }

            if (childVisual != null &&
                !ReferenceEquals(compositionChildren[compositionChildren.Count - 1], childVisual))
                mismatch = true;

            if (!mismatch)
            {
                sortedChildren?.Dispose();
                return;
            }
        }
        
        compositionChildren.Clear();
        if (sortedChildren != null)
        {
            foreach (var ch in sortedChildren)
            {
                var compositionChild = ch.visual.CompositionVisual;
                if (compositionChild != null)
                    compositionChildren.Add(compositionChild);
            }
            sortedChildren.Dispose();
        }
        else
            foreach (var ch in visualChildren)
            {
                var compositionChild = ch.CompositionVisual;
                if (compositionChild != null)
                    compositionChildren.Add(compositionChild);
            }

        if (childVisual != null)
            compositionChildren.Add(childVisual);
    }

    private void UpdateCore()
    {
        _queuedUpdate = false;
        foreach (var visual in _dirty)
        {
            var comp = visual.CompositionVisual;
            if(comp == null)
                continue;
            
            // TODO: Optimize all of that by moving to the Visual itself, so we won't have to recalculate every time
            comp.Offset = new Vector3((float)visual.Bounds.Left, (float)visual.Bounds.Top, 0);
            comp.Size = new Vector2((float)visual.Bounds.Width, (float)visual.Bounds.Height);
            comp.Visible = visual.IsVisible;
            comp.Opacity = (float)visual.Opacity;
            comp.ClipToBounds = visual.ClipToBounds;
            comp.Clip = visual.Clip?.PlatformImpl;


            if (!Equals(comp.OpacityMask, visual.OpacityMask))
                comp.OpacityMask = visual.OpacityMask?.ToImmutable();

            if (!comp.Effect.EffectEquals(visual.Effect))
                comp.Effect = visual.Effect?.ToImmutable();

            var renderTransform = Matrix.Identity;

            if (visual.HasMirrorTransform) 
                renderTransform = new Matrix(-1.0, 0.0, 0.0, 1.0, visual.Bounds.Width, 0);

            if (visual.RenderTransform != null)
            {
                var origin = visual.RenderTransformOrigin.ToPixels(new Size(visual.Bounds.Width, visual.Bounds.Height));
                var offset = Matrix.CreateTranslation(origin);
                renderTransform *= (-offset) * visual.RenderTransform.Value * (offset);
            }



            comp.TransformMatrix = MatrixUtils.ToMatrix4x4(renderTransform);

            _recorder.BeginUpdate(comp.DrawList);
            visual.Render(_recordingContext);
            comp.DrawList = _recorder.EndUpdate();

            SyncChildren(visual);
        }
        foreach(var v in _recalculateChildren)
            if (!_dirty.Contains(v))
                SyncChildren(v);
        _dirty.Clear();
        _recalculateChildren.Clear();
        CompositionTarget.Size = _root.ClientSize;
        CompositionTarget.Scaling = _root.RenderScaling;
        TriggerSceneInvalidatedOnBatchCompletion(_compositor.RequestCommitAsync());
    }

    private async void TriggerSceneInvalidatedOnBatchCompletion(Task batchCompletion)
    {
        await batchCompletion;
        SceneInvalidated?.Invoke(this, new SceneInvalidatedEventArgs(_root, new Rect(_root.ClientSize)));
    }
    
    private void Update()
    {
        if(_updating)
            return;
        _updating = true;
        try
        {
            UpdateCore();
        }
        finally
        {
            _updating = false;
        }
    }

    /// <inheritdoc />
    public void Resized(Size size)
    {
    }

    /// <inheritdoc />
    public void Paint(Rect rect)
    {
        if (_isDisposed)
            return;

        QueueUpdate();
        CompositionTarget.RequestRedraw();
        if(RenderOnlyOnRenderThread && Compositor.Loop.RunsInBackground)
            Compositor.Commit().Wait();
        else
            CompositionTarget.ImmediateUIThreadRender();
    }

    /// <inheritdoc />
    public void Start()
    {
        if (_isDisposed)
            return;

        CompositionTarget.IsEnabled = true;
    }

    /// <inheritdoc />
    public void Stop()
        => CompositionTarget.IsEnabled = false;

    /// <inheritdoc />
    public ValueTask<object?> TryGetRenderInterfaceFeature(Type featureType)
        => Compositor.TryGetRenderInterfaceFeature(featureType);

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _dirty.Clear();
        _recalculateChildren.Clear();
        SceneInvalidated = null;

        Stop();
        CompositionTarget.Dispose();
        
        // Wait for the composition batch to be applied and rendered to guarantee that
        // render target is not used anymore and can be safely disposed
        if (Compositor.Loop.RunsInBackground)
            _compositor.Commit().Wait();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Avalonia.Collections;
using Avalonia.Collections.Pooled;
using Avalonia.Media;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.Composition;

/// <summary>
/// A renderer that utilizes <see cref="Avalonia.Rendering.Composition.Compositor"/> to render the visual tree 
/// </summary>
public class CompositingRenderer : IRendererWithCompositor
{
    private readonly IRenderRoot _root;
    private readonly Compositor _compositor;
    CompositionDrawingContext _recorder = new();
    DrawingContext _recordingContext;
    private HashSet<Visual> _dirty = new();
    private HashSet<Visual> _recalculateChildren = new();
    private readonly CompositionTarget _target;
    private bool _queuedUpdate;
    private Action _update;
    private Action _invalidateScene;

    /// <summary>
    /// Asks the renderer to only draw frames on the render thread. Makes Paint to wait until frame is rendered.
    /// </summary>
    public bool RenderOnlyOnRenderThread { get; set; } = true;

    public CompositingRenderer(IRenderRoot root,
        Compositor compositor)
    {
        _root = root;
        _compositor = compositor;
        _recordingContext = new DrawingContext(_recorder);
        _target = compositor.CreateCompositionTarget(root.CreateRenderTarget);
        _target.Root = ((Visual)root!.VisualRoot!).AttachToCompositor(compositor);
        _update = Update;
        _invalidateScene = InvalidateScene;
    }

    /// <inheritdoc/>
    public bool DrawFps
    {
        get => _target.DrawFps;
        set => _target.DrawFps = value;
    }
    
    /// <inheritdoc/>
    public bool DrawDirtyRects
    {
        get => _target.DrawDirtyRects;
        set => _target.DrawDirtyRects = value;
    }

    /// <inheritdoc/>
    public event EventHandler<SceneInvalidatedEventArgs>? SceneInvalidated;

    void QueueUpdate()
    {
        if(_queuedUpdate)
            return;
        _queuedUpdate = true;
        Dispatcher.UIThread.Post(_update, DispatcherPriority.Composition);
    }
    
    /// <inheritdoc/>
    public void AddDirty(IVisual visual)
    {
        _dirty.Add((Visual)visual);
        QueueUpdate();
    }

    /// <inheritdoc/>
    public IEnumerable<IVisual> HitTest(Point p, IVisual root, Func<IVisual, bool>? filter)
    {
        var res = _target.TryHitTest(p, filter);
        if(res == null)
            yield break;
        for (var index = res.Count - 1; index >= 0; index--)
        {
            var v = res[index];
            if (v is CompositionDrawListVisual dv)
            {
                if (filter == null || filter(dv.Visual))
                    yield return dv.Visual;
            }
        }
    }

    /// <inheritdoc/>
    public IVisual? HitTestFirst(Point p, IVisual root, Func<IVisual, bool>? filter)
    {
        // TODO: Optimize
        return HitTest(p, root, filter).FirstOrDefault();
    }

    /// <inheritdoc/>
    public void RecalculateChildren(IVisual visual)
    {
        _recalculateChildren.Add((Visual)visual);
        QueueUpdate();
    }

    private void SyncChildren(Visual v)
    {
        //TODO: Optimize by moving that logic to Visual itself
        if(v.CompositionVisual == null)
            return;
        var compositionChildren = v.CompositionVisual.Children;
        var visualChildren = (AvaloniaList<IVisual>)v.GetVisualChildren();
        
        PooledList<(IVisual visual, int index)>? sortedChildren = null;
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

        if (compositionChildren.Count == visualChildren.Count)
        {
            bool mismatch = false;
            if (v.HasNonUniformZIndexChildren)
            {
                
                
            }

            if (sortedChildren != null)
                for (var c = 0; c < visualChildren.Count; c++)
                {
                    if (!ReferenceEquals(compositionChildren[c], ((Visual)sortedChildren[c].visual).CompositionVisual))
                    {
                        mismatch = true;
                        break;
                    }
                }
            else
                for (var c = 0; c < visualChildren.Count; c++)
                    if (!ReferenceEquals(compositionChildren[c], ((Visual)visualChildren[c]).CompositionVisual))
                    {
                        mismatch = true;
                        break;
                    }


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
                var compositionChild = ((Visual)ch.visual).CompositionVisual;
                if (compositionChild != null)
                    compositionChildren.Add(compositionChild);
            }
            sortedChildren.Dispose();
        }
        else
            foreach (var ch in v.GetVisualChildren())
            {
                var compositionChild = ((Visual)ch).CompositionVisual;
                if (compositionChild != null)
                    compositionChildren.Add(compositionChild);
            }
    }

    private void InvalidateScene() =>
        SceneInvalidated?.Invoke(this, new SceneInvalidatedEventArgs(_root, new Rect(_root.ClientSize)));

    private void Update()
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
            comp.OpacityMask = visual.OpacityMask;
            
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
        _target.Size = _root.ClientSize;
        _target.Scaling = _root.RenderScaling;
        Compositor.InvokeOnNextCommit(_invalidateScene);
    }
    
    public void Resized(Size size)
    {
    }

    public void Paint(Rect rect)
    {
        Update();
        _target.RequestRedraw();
        if(RenderOnlyOnRenderThread && Compositor.Loop.RunsInBackground)
            Compositor.RequestCommitAsync().Wait();
        else
            _target.ImmediateUIThreadRender();
    }

    public void Start() => _target.IsEnabled = true;

    public void Stop()
    {
        _target.IsEnabled = false;
    }
    
    public void Dispose()
    {
        Stop();
        _target.Dispose();
        
        // Wait for the composition batch to be applied and rendered to guarantee that
        // render target is not used anymore and can be safely disposed
        if (Compositor.Loop.RunsInBackground)
            _compositor.RequestCommitAsync().Wait();
    }

    /// <summary>
    /// The associated <see cref="Avalonia.Rendering.Composition.Compositor"/> object
    /// </summary>
    public Compositor Compositor => _compositor;
}

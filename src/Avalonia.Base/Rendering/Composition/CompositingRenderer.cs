using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Avalonia.Collections;
using Avalonia.Media;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.Composition;

public class CompositingRenderer : RendererBase, IRendererWithCompositor
{
    private readonly IRenderRoot _root;
    private readonly Compositor _compositor;
    private readonly IDeferredRendererLock? _rendererLock;
    CompositionDrawingContext _recorder = new();
    DrawingContext _recordingContext;
    private HashSet<Visual> _dirty = new();
    private HashSet<Visual> _recalculateChildren = new();
    private readonly CompositionTarget _target;
    private bool _queuedUpdate;
    private Action _update;

    public CompositingRenderer(IRenderRoot root,
        Compositor compositor, 
        IDeferredRendererLock? rendererLock = null)
    {
        _root = root;
        _compositor = compositor;
        _recordingContext = new DrawingContext(_recorder);
        _rendererLock = rendererLock ?? new ManagedDeferredRendererLock();
        _target = compositor.CreateCompositionTarget(root.CreateRenderTarget);
        _target.Root = ((Visual)root!.VisualRoot!).AttachToCompositor(compositor);
        _update = Update;
    }

    public bool DrawFps
    {
        get => _target.DrawFps;
        set => _target.DrawFps = value;
    }

    public bool DrawDirtyRects
    {
        get => _target.DrawDirtyRects;
        set => _target.DrawDirtyRects = value;
    }

    public event EventHandler<SceneInvalidatedEventArgs>? SceneInvalidated;

    void QueueUpdate()
    {
        if(_queuedUpdate)
            return;
        _queuedUpdate = true;
        Dispatcher.UIThread.Post(_update, DispatcherPriority.Composition);
    }
    public void AddDirty(IVisual visual)
    {
        _dirty.Add((Visual)visual);
        QueueUpdate();
    }

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

    public IVisual? HitTestFirst(Point p, IVisual root, Func<IVisual, bool>? filter)
    {
        // TODO: Optimize
        return HitTest(p, root, filter).FirstOrDefault();
    }

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
        if (compositionChildren.Count == visualChildren.Count)
        {
            bool mismatch = false;
            for(var c=0; c<visualChildren.Count; c++)
                if(!object.ReferenceEquals(compositionChildren[c], ((Visual)visualChildren[c]).CompositionVisual))
                {
                    mismatch = true;
                    break;
                }
            
            if(!mismatch)
                return;
        }
        compositionChildren.Clear();
        foreach (var ch in v.GetVisualChildren())
        {
            var compositionChild = ((Visual)ch).CompositionVisual;
            if (compositionChild != null)
                compositionChildren.Add(compositionChild);
        }
    }
    
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
            
            var renderTransform = Matrix.Identity;

            if (visual.RenderTransform != null)
            {
                var origin = visual.RenderTransformOrigin.ToPixels(new Size(visual.Bounds.Width, visual.Bounds.Height));
                var offset = Matrix.CreateTranslation(origin);
                renderTransform = (-offset) * visual.RenderTransform.Value * (offset);
            }

            

            if (visual.HasMirrorTransform)
            {
                var mirrorMatrix = new Matrix(-1.0, 0.0, 0.0, 1.0, visual.Bounds.Width, 0);
                renderTransform *= mirrorMatrix;
            }

            comp.TransformMatrix = MatrixUtils.ToMatrix4x4(renderTransform);

            _recorder.BeginUpdate(comp.DrawList ?? new CompositionDrawList());
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
    }
    
    public void Resized(Size size)
    {
    }

    public void Paint(Rect rect)
    {
        // We render only on the render thread for now
        Update();
        _target.RequestRedraw();
        Compositor.RequestCommitAsync().Wait();
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
        _compositor.RequestCommitAsync().Wait();
    }


    public Compositor Compositor => _compositor;
}

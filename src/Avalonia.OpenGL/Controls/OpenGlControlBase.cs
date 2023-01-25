using System;
using System.Numerics;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Logging;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.VisualTree;
using Avalonia.Platform;
namespace Avalonia.OpenGL.Controls
{
    public abstract class OpenGlControlBase : Control
    {
        private CompositionSurfaceVisual _visual;
        private Action _update;
        private bool _updateQueued;
        private Task<bool> _initialization;
        private OpenGlControlBaseResources? _resources;
        private Compositor? _compositor;
        protected GlVersion GlVersion => _resources?.Context.Version ?? default;
        
        public OpenGlControlBase()
        {
            _update = Update;
        }

        void DoCleanup()
        {
            if (_initialization is { Status: TaskStatus.RanToCompletion } && _resources != null)
            {
                try
                {
                    using (_resources.Context.EnsureCurrent())
                    {
                        OnOpenGlDeinit(_resources.Context.GlInterface);
                    }
                }
                catch(Exception e)
                {
                    Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log("OpenGlControlBase",
                        "Unable to free user OpenGL resources: {exception}", e);
                }
            }

            ElementComposition.SetElementChildVisual(this, null);
            _visual = null;
            
            _resources?.DisposeAsync();
            _resources = null;
            _initialization = null;
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            DoCleanup();
            base.OnDetachedFromVisualTree(e);
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _compositor = (this.GetVisualRoot()?.Renderer as IRendererWithCompositor)?.Compositor;
            RequestNextFrameRendering();
        }

        private bool EnsureInitializedCore(
            ICompositionGpuInterop interop,
            IOpenGlTextureSharingRenderInterfaceContextFeature contextSharingFeature)
        {
            var surface = _compositor.CreateDrawingSurface();

            IGlContext ctx = null;
            var contextFactory = AvaloniaLocator.Current.GetService<IPlatformGraphicsOpenGlContextFactory>();
            try
            {
                if (contextSharingFeature?.CanCreateSharedContext == true)
                    _resources = OpenGlControlBaseResources.TryCreate(surface, interop, contextSharingFeature);

                if(_resources == null)
                {
                    ctx = contextFactory.CreateContext(null);
                    if (ctx.TryGetFeature<IGlContextExternalObjectsFeature>(out var externalObjects))
                        _resources = OpenGlControlBaseResources.TryCreate(ctx, surface, interop, externalObjects);
                    else
                        ctx.Dispose();
                }
                
                if(_resources == null)
                {
                    Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log("OpenGlControlBase",
                        "Unable to initialize OpenGL: current platform does not support multithreaded context sharing and shared memory");
                    return false;
                }
            }
            catch (Exception e)
            {
                Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log("OpenGlControlBase",
                    "Unable to initialize OpenGL: {exception}", e);
                ctx?.Dispose();
                return false;
            }
            
            _visual = _compositor.CreateSurfaceVisual();
            _visual.Size = new Vector2((float)Bounds.Width, (float)Bounds.Height);
            _visual.Surface = _resources.Surface;
            ElementComposition.SetElementChildVisual(this, _visual);
            using (_resources.Context.MakeCurrent())
                OnOpenGlInit(_resources.Context.GlInterface);
            return true;

        }
        
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            if (_visual != null && change.Property == BoundsProperty)
            {
                _visual.Size = new Vector2((float)Bounds.Width, (float)Bounds.Height);
                RequestNextFrameRendering();
            }

            base.OnPropertyChanged(change);
        }

        void ContextLost()
        {
            _initialization = null;
            _resources?.DisposeAsync();
            OnOpenGlLost();
        }

        private bool EnsureInitialized()
        {
            if (_initialization != null)
            {
                // Check if we've previously failed to initialize OpenGL on this platform
                if (_initialization is { IsCompleted: true, Result: false } ||
                    _initialization?.IsFaulted == true)
                    return false;

                // Check if we are still waiting for init to complete
                if (_initialization is { IsCompleted: false })
                    return false;

                if (_resources!.Context.IsLost)
                    ContextLost();
                else 
                    return true;
            }

            _initialization = InitializeAsync();

            async void ContinueOnInitialization()
            {
                try
                {
                    await _initialization;
                    RequestNextFrameRendering();
                }
                catch
                {
                    //
                }
            }
            ContinueOnInitialization();
            return false;

        }

        
        private void Update()
        {
            _updateQueued = false;
            if (VisualRoot == null)
                return;
            if(!EnsureInitialized())
                return;
            using (_resources.BeginDraw(GetPixelSize()))
                OnOpenGlRender(_resources.Context.GlInterface, _resources.Fbo);
        }

        private async Task<bool> InitializeAsync()
        {
            if (_compositor == null)
            {
                Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log("OpenGlControlBase",
                    "Unable to obtain Compositor instance");
                return false;
            }
            
            var gpuInteropTask = _compositor.TryGetCompositionGpuInterop();

            var contextSharingFeature =
                (IOpenGlTextureSharingRenderInterfaceContextFeature)
                await _compositor.TryGetRenderInterfaceFeature(
                    typeof(IOpenGlTextureSharingRenderInterfaceContextFeature));
            var interop = await gpuInteropTask;

            if (interop == null)
            {
                Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log("OpenGlControlBase",
                    "Compositor backend doesn't support GPU interop");
                return false;
            }

            if (!EnsureInitializedCore(interop, contextSharingFeature))
            {
                DoCleanup();
                return false;
            }

            using (_resources!.Context.MakeCurrent())
                OnOpenGlInit(_resources.Context.GlInterface);
            
            return true;
        }

        [Obsolete("Use RequestNextFrameRendering()")]
        // ReSharper disable once MemberCanBeProtected.Global
        public new void InvalidateVisual() => RequestNextFrameRendering(); 
        
        public void RequestNextFrameRendering()
        {
            if ((_initialization == null || _initialization is { Status: TaskStatus.RanToCompletion }) &&
                !_updateQueued)
            {
                _updateQueued = true;
                _compositor?.RequestCompositionUpdate(_update);
            }
        }

        private PixelSize GetPixelSize()
        {
            var scaling = VisualRoot!.RenderScaling;
            return new PixelSize(Math.Max(1, (int)(Bounds.Width * scaling)),
                Math.Max(1, (int)(Bounds.Height * scaling)));
        }
        
        protected virtual void OnOpenGlInit(GlInterface gl)
        {
            
        }

        protected virtual void OnOpenGlDeinit(GlInterface gl)
        {
            
        }
        
        protected virtual void OnOpenGlLost()
        {
            
        }
        
        protected abstract void OnOpenGlRender(GlInterface gl, int fb);
    }
}

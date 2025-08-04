using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Logging;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.VisualTree;
using Avalonia.Platform;
using System.ComponentModel;

namespace Avalonia.OpenGL.Controls
{
    /// <summary>
    /// Base class for controls that render using OpenGL.
    /// Provides infrastructure for OpenGL context management, surface creation, and rendering lifecycle.
    /// </summary>
    /// <remarks>
    /// <para>The control automatically manages OpenGL context creation, surface setup, and cleanup.</para>
    /// <para>
    /// <b>Important:</b> Any interaction with <see cref="GlInterface"/> should only happen within the 
    /// <see cref="OnOpenGlInit"/>, <see cref="OnOpenGlDeinit"/>, or <see cref="OnOpenGlRender"/> method overrides.
    /// </para>
    /// <para>
    /// Avalonia ensures proper OpenGL context synchronization and makes the context current only during these method calls.
    /// Accessing OpenGL functions outside of these methods may result in undefined behavior, crashes, or rendering corruption.
    /// </para>
    /// </remarks>
    public abstract class OpenGlControlBase : Control
    {
        private CompositionSurfaceVisual? _visual;
        private readonly Action _update;
        private bool _updateQueued;
        private Task<bool>? _initialization;
        private OpenGlControlBaseResources? _resources;
        private Compositor? _compositor;

        [MemberNotNullWhen(true, nameof(_resources))]
        private bool IsInitializedSuccessfully => _initialization is { Status: TaskStatus.RanToCompletion, Result: true };

        /// <summary>
        /// Gets the OpenGL version information for the current context.
        /// </summary>
        protected GlVersion GlVersion => _resources?.Context.Version ?? default;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="OpenGlControlBase"/> class.
        /// </summary>
        public OpenGlControlBase()
        {
            _update = Update;
        }

        private void DoCleanup()
        {
            if (IsInitializedSuccessfully)
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

            _updateQueued = false;
            _visual = null;
            _resources?.DisposeAsync();
            _resources = null;
            _initialization = null;
        }

        /// <inheritdoc/>
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            DoCleanup();
            base.OnDetachedFromVisualTree(e);
        }

        /// <inheritdoc/>
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _compositor = (this.GetVisualRoot()?.Renderer as IRendererWithCompositor)?.Compositor;
            RequestNextFrameRendering();
        }

        [MemberNotNullWhen(true, nameof(_resources))]
        private bool EnsureInitializedCore(
            ICompositionGpuInterop interop,
            IOpenGlTextureSharingRenderInterfaceContextFeature? contextSharingFeature)
        {
            var surface = _compositor!.CreateDrawingSurface();

            IGlContext? ctx = null;
            try
            {
                if (contextSharingFeature?.CanCreateSharedContext == true)
                    _resources = OpenGlControlBaseResources.TryCreate(surface, interop, contextSharingFeature);

                if(_resources == null)
                {
                    var contextFactory = AvaloniaLocator.Current.GetRequiredService<IPlatformGraphicsOpenGlContextFactory>();
                    ctx = contextFactory.CreateContext(null);
                    if (ctx.TryGetFeature<IGlContextExternalObjectsFeature>(out var externalObjects))
                        _resources = OpenGlControlBaseResources.TryCreate(ctx, surface, interop, externalObjects);
                }
                
                if(_resources == null)
                {
                    Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log("OpenGlControlBase",
                        "Unable to initialize OpenGL: current platform does not support multithreaded context sharing and shared memory");
                    ctx?.Dispose();
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
            _visual.Size = new Vector(Bounds.Width, Bounds.Height);
            _visual.Surface = _resources.Surface;
            ElementComposition.SetElementChildVisual(this, _visual);
            return true;

        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            if (_visual != null && change.Property == BoundsProperty)
            {
                _visual.Size = new Vector(Bounds.Width, Bounds.Height);
                RequestNextFrameRendering();
            }

            base.OnPropertyChanged(change);
        }

        private void ContextLost()
        {
            _initialization = null;
            _resources?.DisposeAsync();
            OnOpenGlLost();
        }

        [MemberNotNullWhen(true, nameof(_resources))]
        private bool EnsureInitialized()
        {
            if (_initialization != null)
            {
                // Check if we've previously failed to initialize OpenGL on this platform
                // or if we are still waiting for init to complete
                if (!IsInitializedSuccessfully)
                {
                    return false;
                }

                if (_resources.Context.IsLost)
                    ContextLost();
                else 
                    return true;
            }

            _initialization = InitializeAsync();

            async void ContinueOnInitialization()
            {
                try
                {
                    if (await _initialization)
                    {
                        RequestNextFrameRendering();
                    }
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
            if (VisualRoot is not { } visualRoot)
                return;
            if(!EnsureInitialized())
                return;
            using (_resources.BeginDraw(GetPixelSize(visualRoot)))
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
                (IOpenGlTextureSharingRenderInterfaceContextFeature?)
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

            using (_resources.Context.MakeCurrent())
                OnOpenGlInit(_resources.Context.GlInterface);
            
            return true;
        }

        /// <inheritdoc cref="Visual.InvalidateVisual"/>
        [Obsolete("Use RequestNextFrameRendering()"), EditorBrowsable(EditorBrowsableState.Never)]
        // ReSharper disable once MemberCanBeProtected.Global
        public new void InvalidateVisual() => RequestNextFrameRendering();

        /// <summary>
        /// Requests that the control be rendered on the next frame.
        /// </summary>
        public void RequestNextFrameRendering()
        {
            if ((_initialization == null || IsInitializedSuccessfully) &&
                !_updateQueued && _compositor != null)
            {
                _updateQueued = true;
                _compositor.RequestCompositionUpdate(_update);
            }
        }

        private PixelSize GetPixelSize(IRenderRoot visualRoot)
        {
            var scaling = visualRoot.RenderScaling;
            return new PixelSize(Math.Max(1, (int)(Bounds.Width * scaling)),
                Math.Max(1, (int)(Bounds.Height * scaling)));
        }

        /// <summary>
        /// Called when the OpenGL context is first created.
        /// </summary>
        /// <param name="gl">The interface for making OpenGL calls. Use <see cref="GlInterface.GetProcAddress"/> to access additional APIs not covered by <see cref="GlInterface"/>.</param>
        protected virtual void OnOpenGlInit(GlInterface gl)
        {
            
        }

        /// <summary>
        /// Called when the OpenGL context is being destroyed.
        /// </summary>
        /// <param name="gl">The OpenGL interface for making OpenGL calls. Use <see cref="GlInterface.GetProcAddress"/> to access additional APIs not covered by <see cref="GlInterface"/>.</param>
        protected virtual void OnOpenGlDeinit(GlInterface gl)
        {
            
        }

        /// <summary>
        /// Called when the OpenGL context is lost and cannot be recovered.
        /// </summary>
        protected virtual void OnOpenGlLost()
        {
            
        }

        /// <summary>
        /// Called to render the OpenGL content for the current frame.
        /// </summary>
        /// <param name="gl">The OpenGL interface for making OpenGL calls. Use <see cref="GlInterface.GetProcAddress"/> to access additional APIs not covered by <see cref="GlInterface"/>.</param>
        /// <param name="fb">The framebuffer ID to render into.</param>
        protected abstract void OnOpenGlRender(GlInterface gl, int fb);
    }
}

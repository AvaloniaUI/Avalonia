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
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using Avalonia.OpenGL.Composition;
using Avalonia.Threading;

namespace Avalonia.OpenGL.Controls
{
    public abstract class OpenGlControlBase : Control
    {
        private readonly Action _update;
        private bool _updateQueued;
        private Task<bool>? _initialization;
        private OpenGlControlBaseResources? _resources;
        private Compositor? _compositor;
        protected GlVersion GlVersion => _resources?.Context.Version ?? default;
        
        public OpenGlControlBase()
        {
            _update = Update;
        }

        private bool ExecUserCode(Action cb)
        {
            try
            {
                cb();
                return true;
            }
            catch (Exception e)
            {
                var info = ExceptionDispatchInfo.Capture(e);
                Dispatcher.UIThread.Post(() => info.Throw());
                return false;
            }
        }
        
        private bool ExecUserCode<T>(Action<T> cb, T arg)
        {
            try
            {
                cb(arg);
                return true;
            }
            catch (Exception e)
            {
                var info = ExceptionDispatchInfo.Capture(e);
                Dispatcher.UIThread.Post(() => info.Throw());
                return false;
            }
        }
        
        private void DoCleanup()
        {
            if (_initialization is { Status: TaskStatus.RanToCompletion } && _resources != null)
            {
                try
                {
                    using (_resources.Context.EnsureCurrent())
                    {
                        ExecUserCode(OnOpenGlDeinit, _resources.Context.GlInterface);
                    }
                }
                catch(Exception e)
                {
                    Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log("OpenGlControlBase",
                        "Unable to free user OpenGL resources: {exception}", e);
                }
            }

            _updateQueued = false;
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
        
        private void ContextLost()
        {
            _initialization = null;
            _resources?.DisposeAsync();
            _resources = null;
            ExecUserCode(OnOpenGlLost);
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


                if (_resources != null)
                {
                    if (_resources.Context.IsLost)
                        ContextLost();
                    else
                        return true;
                }
            }
            
            _initialization = InitializeAsync();

            if (_initialization.Status == TaskStatus.RanToCompletion)
                return true;

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
            if (VisualRoot is not { } visualRoot)
                return;
            if(!EnsureInitialized())
                return;
            using (_resources!.BeginDraw(FramebufferPixelSize))
                ExecUserCode(OpenGlRender);
        }

        private async Task<bool> InitializeAsync()
        {
            _resources = null;
            if (_compositor == null)
            {
                Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log("OpenGlControlBase",
                    "Unable to obtain Compositor instance");
                return false;
            }

            _resources = await OpenGlControlBaseResources.TryCreateAsync(_compositor, this, FramebufferPixelSize);
            if (_resources == null)
                return false;


            var success = false;
            try
            {
                using (_resources.Context.EnsureCurrent())
                    return success = ExecUserCode(OnOpenGlInit, _resources.Context.GlInterface);
            }
            catch(Exception e)
            {
                Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log("OpenGlControlBase",
                    "EnsureCurrent failed: {Exception}", e);
                
                return false;
            }
            finally
            {
                if(!success)
                    await _resources.DisposeAsync();
            }

        }

        protected PixelSize FramebufferPixelSize
        {
            get
            {
                if (VisualRoot == null)
                    return new(1, 1);
                var size = PixelSize.FromSize(Bounds.Size, VisualRoot.RenderScaling);
                return new PixelSize(Math.Max(1, size.Width), Math.Max(1, size.Height));
            }
        }

        [Obsolete("Use RequestNextFrameRendering()"), EditorBrowsable(EditorBrowsableState.Never)]
        // ReSharper disable once MemberCanBeProtected.Global
        public new void InvalidateVisual() => RequestNextFrameRendering();

        public void RequestNextFrameRendering()
        {
            if ((_initialization == null || _initialization is { Status: TaskStatus.RanToCompletion }) &&
                !_updateQueued && _compositor != null)
            {
                _updateQueued = true;
                _compositor.RequestCompositionUpdate(_update);
            }
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

        private void OpenGlRender() => OnOpenGlRender(_resources!.Context.GlInterface, _resources.Fbo);
        
        protected abstract void OnOpenGlRender(GlInterface gl, int fb);
    }
}

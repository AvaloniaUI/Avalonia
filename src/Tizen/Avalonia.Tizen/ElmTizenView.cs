using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Controls.Embedding;
using Avalonia.Input;
using Avalonia.OpenGL.Egl;
using Avalonia.Tizen.Platform;
using Avalonia.Tizen.Platform.Interop;
using ElmSharp;
using Tizen;

namespace Avalonia.Tizen;

public class ElmAvaloniaView : Widget, ITizenView, EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo
{
    private readonly Evas.ImagePixelsSetCallback redrawCallback;

    private IntPtr animator;
    private RenderingMode renderingMode = RenderingMode.WhenDirty;

    protected IntPtr evasImage;

    private readonly Evas.Config glConfig;

    private IntPtr glConfigPtr;
    private IntPtr glEvas;
    private IntPtr glContext;
    private IntPtr glSurface;

    private ElmSharp.Rect _surfaceSize;

    //private GRContext context;
    //private GRGlFramebufferInfo glInfo;
    //private GRBackendRenderTarget renderTarget;
    //private SKSurface surface;
    //private SKCanvas canvas;
    //private SKSizeI surfaceSize;

    private TopLevelImpl _topLevelImpl;
    private EmbeddableControlRoot _topLevel;
    private EglGlPlatformSurface _eglSurface;
    private bool _isInitalised;

    public IInputRoot InputRoot { get; set; }

    public IntPtr Handle => glSurface;

    PixelSize EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo.Size => new PixelSize((int)_surfaceSize.Width, (int)_surfaceSize.Height);

    public Size Size => new Size(_surfaceSize.Width, _surfaceSize.Height);

    public ElmAvaloniaView(EvasObject parent) : base(parent)
    {
        redrawCallback = (d, o) => OnDrawFrame();
        Resized += (sender, e) => OnResized();

        glConfig = new Evas.Config
        {
            color_format = Evas.ColorFormat.RGBA_8888,
            depth_bits = Evas.DepthBits.BIT_24,
            stencil_bits = Evas.StencilBits.BIT_8,
            options_bits = Evas.OptionsBits.NONE,
            multisample_bits = Evas.MultisampleBits.HIGH,
            gles_version = default(int)
        };
    }

    internal void Initialise()
    {
        _eglSurface = new EglGlPlatformSurface(this);
        _topLevelImpl = new TopLevelImpl(this);
        _topLevelImpl.Surfaces = new[] { _eglSurface };

        _topLevel = new EmbeddableControlRoot(_topLevelImpl);

        Invalidate();
    }

    public RenderingMode RenderingMode
    {
        get { return renderingMode; }
        set
        {
            if (renderingMode != value)
            {
                renderingMode = value;

                if (renderingMode == RenderingMode.Continuously)
                    CreateAnimator();
                else
                    DestroyAnimator();
            }
        }
    }

    public void Invalidate()
    {
        if (RenderingMode == RenderingMode.WhenDirty)
            Evas.evas_object_image_pixels_dirty_set(evasImage, true);
    }

    protected sealed override IntPtr CreateHandle(EvasObject parent)
    {
        var handle = Platform.Interop.Elementary.elm_layout_add(parent);
        Platform.Interop.Elementary.elm_layout_theme_set(handle, "layout", "background", "default");

        evasImage = Evas.evas_object_image_filled_add(Evas.evas_object_evas_get(handle));
        Evas.evas_object_image_colorspace_set(evasImage, Evas.Colorspace.ARGB8888);
        Evas.evas_object_image_smooth_scale_set(evasImage, true);
        Evas.evas_object_image_alpha_set(evasImage, true);

        Platform.Interop.Elementary.elm_object_part_content_set(handle, "elm.swallow.content", evasImage);

        CreateNativeResources(parent);

        return handle;
    }

    protected sealed override void OnUnrealize()
    {
        DestroyAnimator();
        DestroyDrawingSurface();
        DestroyNativeResources();

        base.OnUnrealize();
    }

    protected void OnResized()
    {
        var geometry = Geometry;

        // control is not yet fully initialized
        if (geometry.Width <= 0 || geometry.Height <= 0)
            return;

        if (UpdateSurfaceSize(geometry))
        {
            // disconnect the callback
            Evas.evas_object_image_native_surface_set(evasImage, IntPtr.Zero);

            // recreate the drawing surface to match the new size
            DestroyDrawingSurface();

            Evas.evas_object_image_size_set(evasImage, geometry.Width, geometry.Height);

            CreateDrawingSurface();

            // set the image callback; will be invoked when image is marked as dirty
            Evas.evas_object_image_pixels_get_callback_set(evasImage, redrawCallback, IntPtr.Zero);

            TizenThreadingInterface.MainloopContext.Post(_ =>
            {
                if (!_isInitalised)
                {
                    //EglPlatformGraphics.TryInitialize();

                    _isInitalised = true;
                    _topLevel.Prepare();
                    _topLevel.StartRendering();
                }
                else
                {
                    _topLevelImpl?.Resized?.Invoke(_topLevelImpl.ClientSize, WindowResizeReason.Layout);
                }

                // repaint
                Invalidate();
            }, null);
        }
    }

    private void CreateAnimator()
    {
        if (animator == IntPtr.Zero)
        {
            animator = EcoreAnimator.AddAnimator(() =>
            {
                Evas.evas_object_image_pixels_dirty_set(evasImage, true);
                return true;
            });
        }
    }

    private void DestroyAnimator()
    {
        if (animator != IntPtr.Zero)
        {
            EcoreAnimator.RemoveAnimator(animator);
            animator = IntPtr.Zero;
        }
    }

    protected void CreateNativeResources(EvasObject parent)
    {
        if (glEvas == IntPtr.Zero)
        {
            // initialize the OpenGL (the EFL way)
            glEvas = Evas.evas_gl_new(Evas.evas_object_evas_get(parent));

            // copy the configuration to the native side
            glConfigPtr = Marshal.AllocHGlobal(Marshal.SizeOf(glConfig));
            Marshal.StructureToPtr(glConfig, glConfigPtr, false);

            // try initialize the context with Open GL ES 3.x first
            glContext = Evas.evas_gl_context_version_create(glEvas, IntPtr.Zero, Evas.GLContextVersion.EVAS_GL_GLES_3_X);

            // if we could not get 3.x, try 2.x
            if (glContext == IntPtr.Zero)
            {
                Log.Debug("SKGLSurfaceView", "OpenGL ES 3.x was not available, trying 2.x.");
                glContext = Evas.evas_gl_context_version_create(glEvas, IntPtr.Zero, Evas.GLContextVersion.EVAS_GL_GLES_2_X);
            }

            // if that is not available, the default
            if (glContext == IntPtr.Zero)
            {
                Log.Debug("SKGLSurfaceView", "OpenGL ES 2.x was not available, trying the default.");
                glContext = Evas.evas_gl_context_create(glEvas, IntPtr.Zero);
            }
        }
    }

    protected void DestroyNativeResources()
    {
        if (glEvas != IntPtr.Zero)
        {
            _topLevel.StopRendering();

            // destroy the context
            Evas.evas_gl_context_destroy(glEvas, glContext);
            glContext = IntPtr.Zero;

            // release the unmanaged memory
            Marshal.FreeHGlobal(glConfigPtr);
            glConfigPtr = IntPtr.Zero;

            // destroy the EFL wrapper
            Evas.evas_gl_free(glEvas);
            glEvas = IntPtr.Zero;
        }
    }

    protected void OnDrawFrame()
    {
        if (glSurface != IntPtr.Zero)
        {
            TizenThreadingInterface.MainloopContext.Post(_ =>
                _topLevelImpl.Paint?.Invoke(new Rect(_topLevelImpl.ClientSize)), null);
        }
    }

    protected bool UpdateSurfaceSize(ElmSharp.Rect geometry)
    {
        var changed =
            geometry.Width != _surfaceSize.Width ||
            geometry.Height != _surfaceSize.Height;

        if (changed)
        {
            // size has changed, update geometry
            _surfaceSize.Width = geometry.Width;
            _surfaceSize.Height = geometry.Height;
        }

        return changed;
    }

    protected void CreateDrawingSurface()
    {
        if (glSurface == IntPtr.Zero)
        {
            // create the surface
            glSurface = Evas.evas_gl_surface_create(glEvas, glConfigPtr, _surfaceSize.Width, _surfaceSize.Height);

            // copy the native surface to the image
            Evas.evas_gl_native_surface_get(glEvas, glSurface, out var nativeSurface);
            Evas.evas_object_image_native_surface_set(evasImage, ref nativeSurface);

            // switch to the current OpenGL context
            Evas.evas_gl_make_current(glEvas, glSurface, glContext);
        }
    }

    protected void DestroyDrawingSurface()
    {
        if (glSurface != IntPtr.Zero)
        {
            // disconnect the surface from the image
            Evas.evas_object_image_native_surface_set(evasImage, IntPtr.Zero);

            // destroy the surface
            Evas.evas_gl_surface_destroy(glEvas, glSurface);
            glSurface = IntPtr.Zero;
        }
    }

    public Control Content
    {
        get => (Control)_topLevel.Content;
        set => _topLevel.Content = value;
    }

    public Size ClientSize => new Size(Size.Width, Size.Height);

    public double Scaling => ScalingInfo.ScalingFactor;
}

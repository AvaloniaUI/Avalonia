using System;
using System.Collections.Generic;
using System.Linq;
using static Avalonia.OpenGL.Egl.EglConsts;
namespace Avalonia.OpenGL.Egl;

internal static class EglDisplayUtils
{
    public static IntPtr CreateDisplay(EglDisplayCreationOptions options)
    {
        var egl = options.Egl ?? new EglInterface();
        var display = IntPtr.Zero;
        if (options.PlatformType == null)
        {
            if (display == IntPtr.Zero)
                display = egl.GetDisplay(IntPtr.Zero);
        }
        else if (egl.IsGetPlatformDisplayAvailable)
        {
            display = egl.GetPlatformDisplay(options.PlatformType.Value, options.PlatformDisplay,
                options.PlatformDisplayAttrs);
        }
        else if (egl.IsGetPlatformDisplayExtAvailable)
        {
            display = egl.GetPlatformDisplayExt(options.PlatformType.Value, options.PlatformDisplay,
                options.PlatformDisplayAttrs);
        }

        if (display == IntPtr.Zero)
            throw OpenGlException.GetFormattedException("eglGetDisplay", egl);
        return display;
    }

    public static EglConfigInfo InitializeAndGetConfig(EglInterface egl, IntPtr display, IEnumerable<GlVersion>? versions)
    {
        if (!egl.Initialize(display, out _, out _))
            throw OpenGlException.GetFormattedException("eglInitialize", egl);

        // TODO: AvaloniaLocator.Current.GetService<AngleOptions>()?.GlProfiles
        versions ??= new[]
        {
            new GlVersion(GlProfileType.OpenGLES, 3, 0),
            new GlVersion(GlProfileType.OpenGLES, 2, 0)
        };

        var cfgs = versions
            .Where(x => x.Type == GlProfileType.OpenGLES)
            .Select(x =>
            {
                var typeBit = EGL_OPENGL_ES3_BIT;

                switch (x.Major)
                {
                    case 2:
                        typeBit = EGL_OPENGL_ES2_BIT;
                        break;

                    case 1:
                        typeBit = EGL_OPENGL_ES_BIT;
                        break;
                }

                return new
                {
                    Attributes = new[]
                    {
                        EGL_CONTEXT_MAJOR_VERSION, x.Major,
                        EGL_CONTEXT_MINOR_VERSION, x.Minor,
                        EGL_NONE
                    },
                    Api = EGL_OPENGL_ES_API,
                    RenderableTypeBit = typeBit,
                    Version = x
                };
            });

        foreach (var cfg in cfgs)
        {
            if (!egl.BindApi(cfg.Api))
                continue;
            foreach (var surfaceType in new[] { EGL_PBUFFER_BIT | EGL_WINDOW_BIT, EGL_WINDOW_BIT })
            foreach (var stencilSize in new[] { 8, 1, 0 })
            foreach (var depthSize in new[] { 8, 1, 0 })
            {
                var attribs = new[]
                {
                    EGL_SURFACE_TYPE, surfaceType,
                    EGL_RENDERABLE_TYPE, cfg.RenderableTypeBit,
                    EGL_RED_SIZE, 8,
                    EGL_GREEN_SIZE, 8,
                    EGL_BLUE_SIZE, 8,
                    EGL_ALPHA_SIZE, 8,
                    EGL_STENCIL_SIZE, stencilSize,
                    EGL_DEPTH_SIZE, depthSize,
                    EGL_NONE
                };
                if (!egl.ChooseConfig(display, attribs, out var config, 1, out int numConfigs))
                    continue;
                if (numConfigs == 0)
                    continue;


                egl.GetConfigAttrib(display, config, EGL_SAMPLES, out var sampleCount);
                egl.GetConfigAttrib(display, config, EGL_STENCIL_SIZE, out var returnedStencilSize);
                return new EglConfigInfo(config, cfg.Version, surfaceType, cfg.Attributes, sampleCount,
                    returnedStencilSize);
            }
        }

        throw new OpenGlException("No suitable EGL config was found");
    }

    
}

internal class EglConfigInfo
{
    public IntPtr Config { get; }
    public GlVersion Version { get; }
    public int SurfaceType { get; }
    public int[] Attributes { get; }
    public int SampleCount { get; }
    public int StencilSize { get; }

    public EglConfigInfo(IntPtr config, GlVersion version, int surfaceType, int[] attributes, int sampleCount,
        int stencilSize)
    {
        Config = config;
        Version = version;
        SurfaceType = surfaceType;
        Attributes = attributes;
        SampleCount = sampleCount;
        StencilSize = stencilSize;
    }
}

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
        else
        {
            if (!egl.IsGetPlatformDisplayExtAvailable)
                throw new OpenGlException("eglGetPlatformDisplayEXT is not supported by libegl");

            display = egl.GetPlatformDisplayExt(options.PlatformType.Value, options.PlatformDisplay,
                options.PlatformDisplayAttrs);
        }

        if (display == IntPtr.Zero)
            throw OpenGlException.GetFormattedException("eglGetDisplay", egl);
        return display;
    }

    // Enumerates every config matching the attribute list and lets the probe callback pick one (or simply
    // takes the first one if no probe is supplied).
    private static IntPtr? ChooseConfigWithProbe(EglInterface egl, IntPtr display, int[] attribs,
        EglConfigProbeCallback? probe)
    {
        if (!egl.ChooseConfigs(display, attribs, null, 0, out var numConfigs) || numConfigs == 0)
            return null;

        var configs = new IntPtr[numConfigs];
        if (!egl.ChooseConfigs(display, attribs, configs, configs.Length, out numConfigs) || numConfigs == 0)
            return null;
        if (numConfigs != configs.Length)
            Array.Resize(ref configs, numConfigs);

        if (probe == null)
            return configs[0];

        return probe(egl, display, configs);
    }

    public static EglConfigInfo InitializeAndGetConfig(EglInterface egl, IntPtr display,
        IEnumerable<GlVersion>? versions, EglConfigProbeCallback? probeConfig = null)
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
            .Select(x =>
            {
                if (x.Type == GlProfileType.OpenGLES)
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
                }
                else
                {
                    var attrs = (x.Major > 3 || (x.Major == 3 && x.Minor >= 2))
                        ? new[]
                        {
                            EGL_CONTEXT_MAJOR_VERSION, x.Major,
                            EGL_CONTEXT_MINOR_VERSION, x.Minor,
                            EGL_CONTEXT_OPENGL_PROFILE_MASK,
                            x.IsCompatibilityProfile
                                ? EGL_CONTEXT_OPENGL_COMPATIBILITY_PROFILE_BIT
                                : EGL_CONTEXT_OPENGL_CORE_PROFILE_BIT,
                            EGL_NONE
                        }
                        : new[]
                        {
                            EGL_CONTEXT_MAJOR_VERSION, x.Major,
                            EGL_CONTEXT_MINOR_VERSION, x.Minor,
                            EGL_NONE
                        };

                    return new
                    {
                        Attributes = attrs,
                        Api = EGL_OPENGL_API,
                        RenderableTypeBit = EGL_OPENGL_BIT,
                        Version = x
                    };
                }
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
                if (ChooseConfigWithProbe(egl, display, attribs, probeConfig) is not { } config)
                    continue;

                egl.GetConfigAttrib(display, config, EGL_SAMPLES, out var sampleCount);
                egl.GetConfigAttrib(display, config, EGL_STENCIL_SIZE, out var returnedStencilSize);
                return new EglConfigInfo(config, cfg.Version, surfaceType, cfg.Attributes, sampleCount,
                    returnedStencilSize, cfg.Api);
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
    public int Api { get; }

    public EglConfigInfo(IntPtr config, GlVersion version, int surfaceType, int[] attributes, int sampleCount,
        int stencilSize, int api)
    {
        Config = config;
        Version = version;
        SurfaceType = surfaceType;
        Attributes = attributes;
        SampleCount = sampleCount;
        StencilSize = stencilSize;
        Api = api;
    }
}

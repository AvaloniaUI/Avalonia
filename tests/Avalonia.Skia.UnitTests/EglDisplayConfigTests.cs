using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Egl;
using Xunit;
using static Avalonia.OpenGL.Egl.EglConsts;

namespace Avalonia.Skia.UnitTests;

public unsafe class EglDisplayConfigTests
{
    private static readonly IntPtr s_display = 1;
    private static readonly IntPtr s_config = 2;
    private static readonly IntPtr s_context = 3;
    private static readonly IntPtr s_extensions = Marshal.StringToHGlobalAnsi("EGL_KHR_surfaceless_context");

    // Fake driver state. Tests in a class run sequentially, so static state is safe here.
    private static Func<int, int, bool> s_canCreateContext = (_, _) => true;
    private static int s_api = EGL_OPENGL_ES_API;
    private static int s_error = EGL_SUCCESS;

    private static readonly GlVersion[] s_waylandDefaultProfiles =
    {
        new(GlProfileType.OpenGL, 4, 0),
        new(GlProfileType.OpenGL, 3, 2),
        new(GlProfileType.OpenGL, 3, 0),
        new(GlProfileType.OpenGLES, 3, 2),
        new(GlProfileType.OpenGLES, 3, 0),
        new(GlProfileType.OpenGLES, 2, 0)
    };

    public EglDisplayConfigTests()
    {
        s_canCreateContext = (_, _) => true;
        s_api = EGL_OPENGL_ES_API;
        s_error = EGL_SUCCESS;
    }

    [Fact]
    public void Profiles_Without_A_Creatable_Context_Are_Skipped()
    {
        // Mali-400 (lima): configs report EGL_OPENGL_BIT, but only desktop GL 2.1 and GLES 2.0 contexts exist.
        s_canCreateContext = (_, major) => major <= 2;

        var config = EglDisplayUtils.InitializeAndGetConfig(CreateEgl(), s_display, s_waylandDefaultProfiles);

        Assert.Equal(GlProfileType.OpenGLES, config.Version.Type);
        Assert.Equal(2, config.Version.Major);
        Assert.Equal(0, config.Version.Minor);
    }

    [Fact]
    public void Config_Selection_Fails_When_No_Profile_Can_Create_A_Context()
    {
        s_canCreateContext = (_, _) => false;

        Assert.Throws<OpenGlException>(() =>
            EglDisplayUtils.InitializeAndGetConfig(CreateEgl(), s_display, s_waylandDefaultProfiles));
    }

    [Fact]
    public void CreateContext_Reports_The_Error_Of_eglCreateContext()
    {
        var display = new EglDisplay(s_display, new EglDisplayOptions
        {
            Egl = CreateEgl(),
            SupportsMultipleContexts = true,
            GlVersions = new[] { new GlVersion(GlProfileType.OpenGLES, 2, 0) }
        });
        s_canCreateContext = (_, _) => false;

        var ex = Assert.Throws<OpenGlException>(() => display.CreateContext(null));

        Assert.Equal(EGL_BAD_MATCH, ex.ErrorCode);
    }

    private static EglInterface CreateEgl() => new(name => name switch
    {
        "eglGetError" => (IntPtr)(delegate* unmanaged[Stdcall]<int>)&GetError,
        "eglInitialize" => (IntPtr)(delegate* unmanaged[Stdcall]<IntPtr, int*, int*, int>)&Initialize,
        "eglBindAPI" => (IntPtr)(delegate* unmanaged[Stdcall]<int, int>)&BindApi,
        "eglQueryAPI" => (IntPtr)(delegate* unmanaged[Stdcall]<int>)&QueryApi,
        "eglChooseConfig" => (IntPtr)(delegate* unmanaged[Stdcall]<IntPtr, int*, IntPtr*, int, int*, int>)&ChooseConfig,
        "eglGetConfigAttrib" => (IntPtr)(delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int, int*, int>)&GetConfigAttrib,
        "eglCreateContext" => (IntPtr)(delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr, int*, IntPtr>)&CreateContext,
        "eglDestroyContext" => (IntPtr)(delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>)&DestroyContext,
        "eglQueryString" => (IntPtr)(delegate* unmanaged[Stdcall]<IntPtr, int, IntPtr>)&QueryString,
        // Never called by these tests, but required entry points must resolve.
        _ => (IntPtr)(delegate* unmanaged[Stdcall]<IntPtr>)&Unexpected
    });

    // Like a real driver, every successful call resets the error to EGL_SUCCESS.
    private static int Succeed()
    {
        s_error = EGL_SUCCESS;
        return 1;
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static int GetError()
    {
        var error = s_error;
        s_error = EGL_SUCCESS;
        return error;
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static int Initialize(IntPtr display, int* major, int* minor)
    {
        *major = 1;
        *minor = 5;
        return Succeed();
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static int BindApi(int api)
    {
        s_api = api;
        return Succeed();
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static int QueryApi()
    {
        Succeed();
        return s_api;
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static int ChooseConfig(IntPtr display, int* attribs, IntPtr* configs, int configSize, int* numConfigs)
    {
        if (configs != null && configSize > 0)
            configs[0] = s_config;
        *numConfigs = 1;
        return Succeed();
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static int GetConfigAttrib(IntPtr display, IntPtr config, int attribute, int* value)
    {
        *value = attribute == EGL_STENCIL_SIZE ? 8 : 0;
        return Succeed();
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static IntPtr CreateContext(IntPtr display, IntPtr config, IntPtr share, int* attribs)
    {
        var major = 1;
        for (var i = 0; attribs[i] != EGL_NONE; i += 2)
            if (attribs[i] == EGL_CONTEXT_MAJOR_VERSION)
                major = attribs[i + 1];

        if (!s_canCreateContext(s_api, major))
        {
            s_error = EGL_BAD_MATCH;
            return IntPtr.Zero;
        }

        Succeed();
        return s_context;
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static int DestroyContext(IntPtr display, IntPtr context) => Succeed();

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static IntPtr QueryString(IntPtr display, int name)
    {
        Succeed();
        return name == EGL_EXTENSIONS ? s_extensions : IntPtr.Zero;
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static IntPtr Unexpected() => IntPtr.Zero;
}

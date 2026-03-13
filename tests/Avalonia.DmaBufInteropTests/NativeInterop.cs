using System;
using System.Runtime.InteropServices;

namespace Avalonia.DmaBufInteropTests;

/// <summary>
/// P/Invoke bindings for GBM, DRM, and Linux kernel interfaces used for test buffer allocation.
/// </summary>
internal static unsafe partial class NativeInterop
{
    // GBM
    private const string LibGbm = "libgbm.so.1";

    [LibraryImport(LibGbm, EntryPoint = "gbm_create_device")]
    public static partial IntPtr GbmCreateDevice(int fd);

    [LibraryImport(LibGbm, EntryPoint = "gbm_device_destroy")]
    public static partial void GbmDeviceDestroy(IntPtr gbm);

    [LibraryImport(LibGbm, EntryPoint = "gbm_bo_create")]
    public static partial IntPtr GbmBoCreate(IntPtr gbm, uint width, uint height, uint format, uint flags);

    [LibraryImport(LibGbm, EntryPoint = "gbm_bo_create_with_modifiers")]
    public static partial IntPtr GbmBoCreateWithModifiers(IntPtr gbm, uint width, uint height, uint format,
        ulong* modifiers, uint count);

    [LibraryImport(LibGbm, EntryPoint = "gbm_bo_destroy")]
    public static partial void GbmBoDestroy(IntPtr bo);

    [LibraryImport(LibGbm, EntryPoint = "gbm_bo_get_fd")]
    public static partial int GbmBoGetFd(IntPtr bo);

    [LibraryImport(LibGbm, EntryPoint = "gbm_bo_get_stride")]
    public static partial uint GbmBoGetStride(IntPtr bo);

    [LibraryImport(LibGbm, EntryPoint = "gbm_bo_get_modifier")]
    public static partial ulong GbmBoGetModifier(IntPtr bo);

    [LibraryImport(LibGbm, EntryPoint = "gbm_bo_get_width")]
    public static partial uint GbmBoGetWidth(IntPtr bo);

    [LibraryImport(LibGbm, EntryPoint = "gbm_bo_get_height")]
    public static partial uint GbmBoGetHeight(IntPtr bo);

    [LibraryImport(LibGbm, EntryPoint = "gbm_bo_get_format")]
    public static partial uint GbmBoGetFormat(IntPtr bo);

    [LibraryImport(LibGbm, EntryPoint = "gbm_bo_map")]
    public static partial IntPtr GbmBoMap(IntPtr bo, uint x, uint y, uint width, uint height,
        uint flags, uint* stride, IntPtr* mapData);

    [LibraryImport(LibGbm, EntryPoint = "gbm_bo_unmap")]
    public static partial void GbmBoUnmap(IntPtr bo, IntPtr mapData);

    // GBM flags
    public const uint GBM_BO_USE_RENDERING = 1 << 2;
    public const uint GBM_BO_USE_LINEAR = 1 << 4;

    // GBM_BO_TRANSFER flags for gbm_bo_map
    public const uint GBM_BO_TRANSFER_WRITE = 2;
    public const uint GBM_BO_TRANSFER_READ_WRITE = 3;

    // DRM format codes
    public const uint GBM_FORMAT_ARGB8888 = 0x34325241; // DRM_FORMAT_ARGB8888
    public const uint GBM_FORMAT_XRGB8888 = 0x34325258;
    public const uint GBM_FORMAT_ABGR8888 = 0x34324241;

    // libc
    private const string LibC = "libc";

    [LibraryImport(LibC, EntryPoint = "open", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int Open(string path, int flags);

    [LibraryImport(LibC, EntryPoint = "close")]
    public static partial int Close(int fd);

    [LibraryImport(LibC, EntryPoint = "mmap")]
    public static partial IntPtr Mmap(IntPtr addr, nuint length, int prot, int flags, int fd, long offset);

    [LibraryImport(LibC, EntryPoint = "munmap")]
    public static partial int Munmap(IntPtr addr, nuint length);

    [LibraryImport(LibC, EntryPoint = "memfd_create", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int MemfdCreate(string name, uint flags);

    [LibraryImport(LibC, EntryPoint = "ftruncate")]
    public static partial int Ftruncate(int fd, long length);

    [LibraryImport(LibC, EntryPoint = "ioctl")]
    public static partial int Ioctl(int fd, ulong request, void* arg);

    public const int O_RDWR = 0x02;
    public const int PROT_READ = 0x1;
    public const int PROT_WRITE = 0x2;
    public const int MAP_SHARED = 0x01;
    public const uint MFD_ALLOW_SEALING = 0x0002;

    // udmabuf
    public const ulong UDMABUF_CREATE = 0x40187542; // _IOW('u', 0x42, struct udmabuf_create)

    [StructLayout(LayoutKind.Sequential)]
    public struct UdmabufCreate
    {
        public int Memfd;
        public uint Flags;
        public ulong Offset;
        public ulong Size;
    }

    // EGL
    private const string LibEgl = "libEGL.so.1";

    [LibraryImport(LibEgl, EntryPoint = "eglGetProcAddress", StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr EglGetProcAddress(string procname);

    [LibraryImport(LibEgl, EntryPoint = "eglGetDisplay")]
    public static partial IntPtr EglGetDisplay(IntPtr nativeDisplay);

    [LibraryImport(LibEgl, EntryPoint = "eglInitialize")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool EglInitialize(IntPtr display, out int major, out int minor);

    [LibraryImport(LibEgl, EntryPoint = "eglTerminate")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool EglTerminate(IntPtr display);

    [LibraryImport(LibEgl, EntryPoint = "eglQueryString")]
    public static partial IntPtr EglQueryStringNative(IntPtr display, int name);

    public static string? EglQueryString(IntPtr display, int name)
    {
        var ptr = EglQueryStringNative(display, name);
        return ptr == IntPtr.Zero ? null : Marshal.PtrToStringAnsi(ptr);
    }

    [LibraryImport(LibEgl, EntryPoint = "eglBindAPI")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool EglBindApi(int api);

    [LibraryImport(LibEgl, EntryPoint = "eglChooseConfig")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool EglChooseConfig(IntPtr display, int[] attribs, IntPtr* configs, int configSize,
        out int numConfig);

    [LibraryImport(LibEgl, EntryPoint = "eglCreateContext")]
    public static partial IntPtr EglCreateContext(IntPtr display, IntPtr config, IntPtr shareContext, int[] attribs);

    [LibraryImport(LibEgl, EntryPoint = "eglDestroyContext")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool EglDestroyContext(IntPtr display, IntPtr context);

    [LibraryImport(LibEgl, EntryPoint = "eglMakeCurrent")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool EglMakeCurrent(IntPtr display, IntPtr draw, IntPtr read, IntPtr context);

    [LibraryImport(LibEgl, EntryPoint = "eglCreatePbufferSurface")]
    public static partial IntPtr EglCreatePbufferSurface(IntPtr display, IntPtr config, int[] attribs);

    [LibraryImport(LibEgl, EntryPoint = "eglDestroySurface")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool EglDestroySurface(IntPtr display, IntPtr surface);

    [LibraryImport(LibEgl, EntryPoint = "eglGetError")]
    public static partial int EglGetError();

    // EGL_KHR_platform_gbm
    [LibraryImport(LibEgl, EntryPoint = "eglGetPlatformDisplayEXT")]
    public static partial IntPtr EglGetPlatformDisplayExt(int platform, IntPtr nativeDisplay, int[]? attribs);

    // EGL constants
    public const int EGL_OPENGL_ES_API = 0x30A0;
    public const int EGL_OPENGL_ES2_BIT = 0x0004;
    public const int EGL_OPENGL_ES3_BIT = 0x0040;
    public const int EGL_RENDERABLE_TYPE = 0x3040;
    public const int EGL_SURFACE_TYPE = 0x3033;
    public const int EGL_PBUFFER_BIT = 0x0001;
    public const int EGL_RED_SIZE = 0x3024;
    public const int EGL_GREEN_SIZE = 0x3023;
    public const int EGL_BLUE_SIZE = 0x3022;
    public const int EGL_ALPHA_SIZE = 0x3021;
    public const int EGL_NONE = 0x3038;
    public const int EGL_CONTEXT_MAJOR_VERSION = 0x3098;
    public const int EGL_CONTEXT_MINOR_VERSION = 0x30FB;
    public const int EGL_WIDTH = 0x3057;
    public const int EGL_HEIGHT = 0x3056;
    public const int EGL_EXTENSIONS = 0x3055;
    public const int EGL_NO_IMAGE_KHR = 0;
    public const int EGL_LINUX_DMA_BUF_EXT = 0x3270;
    public const int EGL_LINUX_DRM_FOURCC_EXT = 0x3271;
    public const int EGL_DMA_BUF_PLANE0_FD_EXT = 0x3272;
    public const int EGL_DMA_BUF_PLANE0_OFFSET_EXT = 0x3273;
    public const int EGL_DMA_BUF_PLANE0_PITCH_EXT = 0x3274;
    public const int EGL_DMA_BUF_PLANE0_MODIFIER_LO_EXT = 0x3443;
    public const int EGL_DMA_BUF_PLANE0_MODIFIER_HI_EXT = 0x3444;
    public const int EGL_PLATFORM_GBM_KHR = 0x31D7;
    public const int EGL_SYNC_NATIVE_FENCE_ANDROID = 0x3144;
    public const int EGL_SYNC_NATIVE_FENCE_FD_ANDROID = 0x3145;
    public const int EGL_NO_NATIVE_FENCE_FD_ANDROID = -1;
    public const int EGL_SYNC_FLUSH_COMMANDS_BIT_KHR = 0x0001;
    public const long EGL_FOREVER_KHR = unchecked((long)0xFFFFFFFFFFFFFFFF);
    public const int EGL_CONDITION_SATISFIED_KHR = 0x30F6;

    // GL
    private const string LibGl = "libGLESv2.so.2";

    [LibraryImport(LibGl, EntryPoint = "glGetError")]
    public static partial int GlGetError();

    [LibraryImport(LibGl, EntryPoint = "glGenTextures")]
    public static partial void GlGenTextures(int n, int* textures);

    [LibraryImport(LibGl, EntryPoint = "glDeleteTextures")]
    public static partial void GlDeleteTextures(int n, int* textures);

    [LibraryImport(LibGl, EntryPoint = "glBindTexture")]
    public static partial void GlBindTexture(int target, int texture);

    [LibraryImport(LibGl, EntryPoint = "glGenFramebuffers")]
    public static partial void GlGenFramebuffers(int n, int* framebuffers);

    [LibraryImport(LibGl, EntryPoint = "glDeleteFramebuffers")]
    public static partial void GlDeleteFramebuffers(int n, int* framebuffers);

    [LibraryImport(LibGl, EntryPoint = "glBindFramebuffer")]
    public static partial void GlBindFramebuffer(int target, int framebuffer);

    [LibraryImport(LibGl, EntryPoint = "glFramebufferTexture2D")]
    public static partial void GlFramebufferTexture2D(int target, int attachment, int texTarget, int texture,
        int level);

    [LibraryImport(LibGl, EntryPoint = "glCheckFramebufferStatus")]
    public static partial int GlCheckFramebufferStatus(int target);

    [LibraryImport(LibGl, EntryPoint = "glReadPixels")]
    public static partial void GlReadPixels(int x, int y, int width, int height, int format, int type, void* pixels);

    [LibraryImport(LibGl, EntryPoint = "glFlush")]
    public static partial void GlFlush();

    [LibraryImport(LibGl, EntryPoint = "glFinish")]
    public static partial void GlFinish();

    public const int GL_TEXTURE_2D = 0x0DE1;
    public const int GL_FRAMEBUFFER = 0x8D40;
    public const int GL_COLOR_ATTACHMENT0 = 0x8CE0;
    public const int GL_FRAMEBUFFER_COMPLETE = 0x8CD5;
    public const int GL_RGBA = 0x1908;
    public const int GL_BGRA = 0x80E1;
    public const int GL_UNSIGNED_BYTE = 0x1401;

    // Function pointer types for EGL extensions loaded via eglGetProcAddress
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr EglCreateImageKHRDelegate(IntPtr dpy, IntPtr ctx, int target, IntPtr buffer,
        int[] attribs);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public delegate bool EglDestroyImageKHRDelegate(IntPtr dpy, IntPtr image);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public delegate bool EglQueryDmaBufFormatsEXTDelegate(IntPtr dpy, int maxFormats,
        [Out] int[]? formats, out int numFormats);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public delegate bool EglQueryDmaBufModifiersEXTDelegate(IntPtr dpy, int format, int maxModifiers,
        [Out] long[]? modifiers, [Out] int[]? externalOnly, out int numModifiers);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void GlEGLImageTargetTexture2DOESDelegate(int target, IntPtr image);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr EglCreateSyncKHRDelegate(IntPtr dpy, int type, int[] attribs);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public delegate bool EglDestroySyncKHRDelegate(IntPtr dpy, IntPtr sync);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int EglClientWaitSyncKHRDelegate(IntPtr dpy, IntPtr sync, int flags, long timeout);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int EglWaitSyncKHRDelegate(IntPtr dpy, IntPtr sync, int flags);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int EglDupNativeFenceFDANDROIDDelegate(IntPtr dpy, IntPtr sync);

    public static T? LoadEglExtension<T>(string name) where T : Delegate
    {
        var ptr = EglGetProcAddress(name);
        return ptr == IntPtr.Zero ? null : Marshal.GetDelegateForFunctionPointer<T>(ptr);
    }
}

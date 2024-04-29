using System;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Egl;
using Avalonia.Platform;
using Avalonia.Win32.DirectX;
using MicroCom.Runtime;
using static Avalonia.OpenGL.Egl.EglConsts;
using static Avalonia.OpenGL.GlConsts;

namespace Avalonia.Win32.OpenGl.Angle;

internal class AngleExternalMemoryD3D11Texture2D : IGlExternalImageTexture
{
    private readonly EglContext _context;
    private ID3D11Texture2D? _texture2D;
    private EglSurface? _eglSurface;
    private IDXGIKeyedMutex? _mutex;

    private IDXGIKeyedMutex Mutex
        => _mutex ?? throw new ObjectDisposedException(nameof(AngleExternalMemoryD3D11Texture2D));

    public unsafe AngleExternalMemoryD3D11Texture2D(EglContext context, ID3D11Texture2D texture2D, PlatformGraphicsExternalImageProperties props)
    {
        _context = context;
        _texture2D = texture2D.CloneReference();
        _mutex = _texture2D.QueryInterface<IDXGIKeyedMutex>();
        Properties = props;

        InternalFormat = GL_RGBA8;

        var attrs = stackalloc[]
        {
            EGL_WIDTH, props.Width, EGL_HEIGHT, props.Height, EGL_TEXTURE_FORMAT, EGL_TEXTURE_RGBA,
            EGL_TEXTURE_TARGET, EGL_TEXTURE_2D, EGL_TEXTURE_INTERNAL_FORMAT_ANGLE, GL_RGBA, EGL_NONE, EGL_NONE,
            EGL_NONE
        };
        _eglSurface = _context.Display.CreatePBufferFromClientBuffer(EGL_D3D_TEXTURE_ANGLE, texture2D.GetNativeIntPtr(), attrs);
        
        var gl = _context.GlInterface;
        int temp = 0;
        gl.GenTextures(1, &temp);
        TextureId = temp;
        gl.BindTexture(GL_TEXTURE_2D, TextureId);

        if (_context.Display.EglInterface.BindTexImage(_context.Display.Handle, _eglSurface.DangerousGetHandle(),
                EGL_BACK_BUFFER) == 0)
            
            throw OpenGlException.GetFormattedException("eglBindTexImage", _context.Display.EglInterface);
    }

    public void Dispose()
    {
        
        if (!_context.IsLost && TextureId != 0)
            using (_context.EnsureCurrent())
                _context.GlInterface.DeleteTexture(TextureId);
        TextureId = 0;
        _eglSurface?.Dispose();
        _eglSurface = null;
        _texture2D?.Dispose();
        _texture2D = null;
        _mutex?.Dispose();
        _mutex = null;
    }

    public void AcquireKeyedMutex(uint key) => Mutex.AcquireSync(key, int.MaxValue);
    public void ReleaseKeyedMutex(uint key) => Mutex.ReleaseSync(key);

    public int TextureId { get; private set; }
    public int InternalFormat { get; }
    public PlatformGraphicsExternalImageProperties Properties { get; }
}

internal class AngleExternalMemoryD3D11ExportedTexture2D : AngleExternalMemoryD3D11Texture2D, IGlExportableExternalImageTexture
{
    private static IPlatformHandle GetHandle(ID3D11Texture2D texture2D)
    {
        using var resource = texture2D.QueryInterface<IDXGIResource>();
        return new PlatformHandle(resource.SharedHandle,
            KnownPlatformGraphicsExternalImageHandleTypes.D3D11TextureGlobalSharedHandle);
    }

    public AngleExternalMemoryD3D11ExportedTexture2D(EglContext context, ID3D11Texture2D texture2D,
        D3D11_TEXTURE2D_DESC desc,
        PlatformGraphicsExternalImageFormat format)
        : this(context, texture2D, GetHandle(texture2D),
            new PlatformGraphicsExternalImageProperties
            {
                Width = (int)desc.Width, Height = (int)desc.Height, Format = format
            })
    {

    }

    private AngleExternalMemoryD3D11ExportedTexture2D(EglContext context, ID3D11Texture2D texture2D,
        IPlatformHandle handle, PlatformGraphicsExternalImageProperties properties) 
        : base(context, texture2D, properties)
    {
        Handle = handle;
    }

    public IPlatformHandle Handle { get; }
    public IPlatformHandle GetHandle() => Handle;

}

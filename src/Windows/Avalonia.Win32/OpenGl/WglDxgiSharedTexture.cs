using System;
using System.Reactive.Disposables;
using Avalonia.MicroCom;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Win32.DirectX;

namespace Avalonia.Win32.OpenGl
{
    class WglDxgiSharedTexture : IGlOSSharedTexture, IPlatformHandle
    {
        private readonly WglContext _context;
        private ID3D11Texture2D _texture;
        private IDXGIKeyedMutex _mutex;
        private IntPtr _textureHandle;
        private bool _glDisposed;
        
        public IntPtr Handle { get; private set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public string HandleDescriptor => "DXGITexture2DSharedHandle";

        public unsafe WglDxgiSharedTexture(WglContext context, int width, int height)
        {
            _context = context;

            var desc = new D3D11_TEXTURE2D_DESC
            {
                Format = DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM,
                Width = (uint)width,
                Height = (uint)height,
                ArraySize = 1,
                MipLevels = 1,
                SampleDesc = new DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
                Usage = D3D11_USAGE.D3D11_USAGE_DEFAULT,
                CPUAccessFlags = 0,
                MiscFlags = D3D11_RESOURCE_MISC_FLAG.D3D11_RESOURCE_MISC_SHARED_KEYEDMUTEX,
                BindFlags = D3D11_BIND_FLAG.D3D11_BIND_RENDER_TARGET | D3D11_BIND_FLAG.D3D11_BIND_SHADER_RESOURCE
            };

            _texture = _context.D3DInterop.Device.CreateTexture2D(&desc, null);

            // Get the handle and the keyed mutex
            using var resource = _texture.QueryInterface<IDXGIResource>();
            Handle = resource.SharedHandle;
            
            // Extract info and register the texture with GL
            Register(width, height);
        }

        public WglDxgiSharedTexture(WglContext context, IntPtr dxgiHandle, int width, int height)
        {
            _context = context;
            Handle = dxgiHandle;
            _texture = _context.D3DInterop.Device.OpenSharedResource<ID3D11Texture2D>(dxgiHandle);
            Register(width, height);
        }

        void Register(int width, int height)
        {
            Width = width;
            Height = height;
            
            _mutex = _texture.QueryInterfaceOrNull<IDXGIKeyedMutex>();
            // Register the texture with the current WGL context
            using (_context.EnsureCurrent())
            {
                _context.WglInterface.DXSetResourceShareHandleNV(_texture.GetNativeIntPtr(), Handle);
                _context.WglInterface.DXSetResourceShareHandleNV(_texture.GetNativeIntPtr(), Handle);

                var textures = new int[1];
                _context.GlInterface.GenTextures(1, textures);
                
                _textureHandle = _context.WglInterface.DXRegisterObjectNV(_context.D3DInterop.GlDevice, _texture.GetNativeIntPtr(),
                    textures[0],
                    GlConsts.GL_TEXTURE_2D, WglConsts.WGL_ACCESS_READ_WRITE_NV);
                TextureId = textures[0];
            }
        }
        
        public void Dispose()
        {
            if (!_glDisposed)
            {
                using (_context.EnsureCurrent())
                {
                    _context.GlInterface.DeleteTextures(1, new[] { TextureId });
                    _context.WglInterface.DXUnregisterObjectNV(_context.D3DInterop.GlDevice, _textureHandle);
                }
                _glDisposed = true;
            }

            _mutex?.Dispose();
            _mutex = null;
            _texture?.Dispose();
            _texture = null;
            Handle = IntPtr.Zero;
        }

        public IDisposable LockDx()
        {
            if (_mutex == null)
                return Disposable.Empty;
            _mutex.AcquireSync((ulong)0, 2000);
            return Disposable.Create(() => _mutex.ReleaseSync(0));
        }

        public IDisposable LockGl()
        {
            using (_context.EnsureCurrent())
            {
                if (_context.WglInterface.DXLockObjectsNV(_context.D3DInterop.GlDevice, 1, new[]{_textureHandle}) == 0)
                    throw new OpenGlException("wglDXLockObjectsNV failed");
            }

            return Disposable.Create(() =>
            {
                using (_context.EnsureCurrent())
                {
                    _context.GlInterface.Flush();
                    _context.WglInterface.DXUnlockObjectsNV(_context.D3DInterop.GlDevice, 1, new[] { _textureHandle });
                }
            });
        }

        public int TextureId { get; set; }
        public int Fbo => 0;

        public IDisposable Lock()
        {
            var dxLock = LockDx();
            try
            {
                var glLock = LockGl();
                return Disposable.Create(() =>
                {
                    glLock.Dispose();
                    dxLock.Dispose();
                    
                });
            }
            catch
            {
                dxLock.Dispose();
                throw;
            }
        }
    }
}

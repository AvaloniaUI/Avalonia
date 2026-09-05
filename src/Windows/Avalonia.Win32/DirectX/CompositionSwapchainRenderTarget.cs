using System;
using Avalonia.OpenGL.Egl;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using MicroCom.Runtime;

namespace Avalonia.Win32.DirectX;

/// <summary>
/// A composed window that can display a DXGI swapchain as its content.
/// </summary>
internal interface ISwapchainVisualHost
{
    EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo WindowInfo { get; }
    IDisposable BeginTransaction();

    /// <summary>
    /// Attaches the swapchain as the content of the window's visual.
    /// Called with the transaction held, only when the swapchain is (re)created.
    /// </summary>
    void SetSwapchainContent(IUnknown swapchain);

    void ResizeIfNeeded(PixelSize size);
    void ApplyEffects(CompositionTransparencyLevel transparencyLevel, PlatformThemeVariant themeVariant);
}

/// <summary>
/// Renders into a DXGI flip-model swapchain created for composition and attached
/// as visual content via <see cref="ISwapchainVisualHost"/>. Unlike the composition
/// drawing surface targets, this allows DWM to use optimized presentation modes
/// (independent flip) for fullscreen windows.
/// </summary>
internal unsafe class CompositionSwapchainRenderTarget : IDirect3D11TextureRenderTarget, IDirect3D11TextureRenderTarget2
{
    private static readonly Guid IID_ID3D11Texture2D = Guid.Parse("6f15aaf2-d208-4e89-9ab4-489535d34f9c");

    private readonly IPlatformGraphicsContext _context;
    private readonly ISwapchainVisualHost _host;
    private readonly IUnknown _d3dDevice;
    private readonly IDXGIDevice _dxgiDevice;
    private readonly IDXGIFactory2 _dxgiFactory;
    private IDXGISwapChain1? _swapChain;
    private PixelSize _size;
    private bool _isSwapchainTransparencyCapable;
    private bool _lost;

    public CompositionSwapchainRenderTarget(IPlatformGraphicsContext context, IntPtr d3dDevice,
        ISwapchainVisualHost host)
    {
        _context = context;
        _host = host;

        try
        {
            _d3dDevice = MicroComRuntime.CreateProxyFor<IUnknown>(d3dDevice, false).CloneReference();
            _dxgiDevice = _d3dDevice.QueryInterface<IDXGIDevice>();
            using var adapter = _dxgiDevice.Adapter;
            var factoryGuid = MicroComRuntime.GetGuidFor(typeof(IDXGIFactory2));
            _dxgiFactory = MicroComRuntime.CreateProxyFor<IDXGIFactory2>(adapter.GetParent(&factoryGuid), true);
        }
        catch
        {
            _dxgiFactory?.Dispose();
            _dxgiDevice?.Dispose();
            _d3dDevice?.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        _swapChain?.Dispose();
        _dxgiFactory.Dispose();
        _dxgiDevice.Dispose();
        _d3dDevice.Dispose();
    }

    public PlatformRenderTargetState State =>
        _context.IsLost || _lost ? PlatformRenderTargetState.Corrupted : PlatformRenderTargetState.Ready;

    private void CreateSwapchain(PixelSize size, bool isTransparency)
    {
        var desc = new DXGI_SWAP_CHAIN_DESC1
        {
            Width = (uint)size.Width,
            Height = (uint)size.Height,
            Format = DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,
            SampleDesc = { Count = 1, Quality = 0 },
            BufferUsage = DxgiRenderTarget.DXGI_USAGE_RENDER_TARGET_OUTPUT,
            BufferCount = 2,
            Scaling = DXGI_SCALING.DXGI_SCALING_STRETCH,
            // FLIP_DISCARD is not available on Windows 8.x
            SwapEffect = Win32Platform.WindowsVersion >= PlatformConstants.Windows10 ?
                DXGI_SWAP_EFFECT.DXGI_SWAP_EFFECT_FLIP_DISCARD :
                DXGI_SWAP_EFFECT.DXGI_SWAP_EFFECT_FLIP_SEQUENTIAL,
            // Premultiplied alpha increases the DWM composition cost and prevents optimized
            // presentation modes, so it's only used when the window is actually transparent,
            // see https://github.com/AvaloniaUI/Avalonia/issues/20643
            AlphaMode = isTransparency ?
                DXGI_ALPHA_MODE.DXGI_ALPHA_MODE_PREMULTIPLIED :
                DXGI_ALPHA_MODE.DXGI_ALPHA_MODE_IGNORE
        };

        var swapChain = _dxgiFactory.CreateSwapChainForComposition(_dxgiDevice, &desc, null);
        try
        {
            _host.SetSwapchainContent(swapChain);
        }
        catch
        {
            swapChain.Dispose();
            throw;
        }

        _swapChain = swapChain;
        _isSwapchainTransparencyCapable = isTransparency;
        _size = size;
    }

    IDirect3D11TextureRenderTargetRenderSession IDirect3D11TextureRenderTarget.BeginDraw()
    {
        var fallbackSceneInfo = new IRenderTarget.RenderTargetSceneInfo(_host.WindowInfo.Size,
            _host.WindowInfo.Scaling, CompositionTransparencyLevel.None);
        return BeginDraw(fallbackSceneInfo);
    }

    public IDirect3D11TextureRenderTargetRenderSession BeginDraw(IRenderTarget.RenderTargetSceneInfo sceneInfo)
    {
        if (State.IsCorrupted)
            throw new RenderTargetCorruptedException();
        var transaction = _host.BeginTransaction();
        try
        {
            var isTransparency = sceneInfo.TransparencyLevel != CompositionTransparencyLevel.None;
            // Unlike hwnd ones, composition swapchains require explicit non-zero dimensions
            var size = new PixelSize(Math.Max(1, sceneInfo.Size.Width), Math.Max(1, sceneInfo.Size.Height));

            IUnknown texture;
            try
            {
                if (_swapChain is null || _isSwapchainTransparencyCapable != isTransparency)
                {
                    // The alpha mode can't be changed by ResizeBuffers, so the swapchain
                    // is recreated when the transparency level changes
                    _swapChain?.Dispose();
                    _swapChain = null;
                    CreateSwapchain(size, isTransparency);
                }
                else if (_size != size)
                {
                    // All backbuffer references are released at this point: the previous
                    // session disposes its texture proxy before Present, and the EGL surface
                    // wrapping it is created and destroyed within a frame
                    _swapChain.ResizeBuffers(2, (uint)size.Width, (uint)size.Height,
                        DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM, 0);
                    _size = size;
                }

                _host.ResizeIfNeeded(size);
                _host.ApplyEffects(sceneInfo.TransparencyLevel,
                    (sceneInfo.TopLevelSpecificSceneInfo as Win32TopLevelSceneInfo)?.ThemeVariant ??
                    PlatformThemeVariant.Light);

                var iid = IID_ID3D11Texture2D;
                texture = MicroComRuntime.CreateProxyFor<IUnknown>(_swapChain!.GetBuffer(0, &iid), true);
            }
            catch (Exception e)
            {
                _lost = true;
                throw new RenderTargetCorruptedException(e);
            }

            using (texture)
            {
                var session = new Session(_swapChain, texture, transaction, size, sceneInfo.Scaling);
                transaction = null;
                return session;
            }
        }
        finally
        {
            transaction?.Dispose();
        }
    }

    private class Session : IDirect3D11TextureRenderTargetRenderSession
    {
        private readonly IDXGISwapChain1 _swapChain;
        private readonly IUnknown _texture;
        private readonly IDisposable _transaction;
        private readonly PixelSize _size;
        private readonly double _scaling;

        public Session(IDXGISwapChain1 swapChain, IUnknown texture, IDisposable transaction,
            PixelSize size, double scaling)
        {
            _swapChain = swapChain.CloneReference();
            _texture = texture.CloneReference();
            _transaction = transaction;
            _size = size;
            _scaling = scaling;
        }

        public void Dispose()
        {
            try
            {
                // The backbuffer reference must be released before the next ResizeBuffers;
                // Present goes before the transaction is committed, so a freshly attached
                // swapchain always has a frame by the time it becomes visible
                _texture.Dispose();
                _swapChain.Present(0, 0);
                _swapChain.Dispose();
            }
            finally
            {
                _transaction.Dispose();
            }
        }

        public IntPtr D3D11Texture2D => _texture.GetNativeIntPtr();
        public PixelSize Size => _size;
        public PixelPoint Offset => default;
        public double Scaling => _scaling;
    }
}

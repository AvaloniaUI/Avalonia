using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using static Silk.NET.Core.Native.SilkMarshal;

namespace GpuInterop.D3DDemo;

class D3D11Swapchain : SwapchainBase<D3D11SwapchainImage>
{
    private readonly ComPtr<ID3D11Device> _device;

    public D3D11Swapchain(ComPtr<ID3D11Device> device, ICompositionGpuInterop interop, CompositionDrawingSurface target)
        : base(interop, target)
    {
        _device = device;
    }

    protected override D3D11SwapchainImage CreateImage(PixelSize size) => new(_device, size, Interop, Target);

    public IDisposable BeginDraw(PixelSize size, out ComPtr<ID3D11RenderTargetView> view)
    {
        var rv = BeginDrawCore(size, out var image);
        view = image.RenderTargetView;
        return rv;
    }
}

public class D3D11SwapchainImage : ISwapchainImage
{
    public PixelSize Size { get; }
    private readonly ICompositionGpuInterop _interop;
    private readonly CompositionDrawingSurface _target;
    private ComPtr<ID3D11Texture2D> _texture;
    private ComPtr<IDXGIKeyedMutex> _mutex;
    private ComPtr<ID3D11RenderTargetView> _renderTargetView;
    private readonly IntPtr _handle;
    private PlatformGraphicsExternalImageProperties _properties;
    private ICompositionImportedGpuImage? _imported;

    public Task? LastPresent { get; private set; }
    public ComPtr<ID3D11RenderTargetView> RenderTargetView => _renderTargetView;

    public unsafe D3D11SwapchainImage(
        ComPtr<ID3D11Device> device,
        PixelSize size,
        ICompositionGpuInterop interop,
        CompositionDrawingSurface target)
    {
        Size = size;
        _interop = interop;
        _target = target;

        ComPtr<ID3D11Texture2D> texture = default;
        var textureDesc = new Texture2DDesc
        {
            Format = Format.FormatR8G8B8A8Unorm,
            Width = (uint)size.Width,
            Height = (uint)size.Height,
            ArraySize = 1,
            MipLevels = 1,
            SampleDesc = new SampleDesc(1, 0),
            Usage = Usage.Default,
            BindFlags = (uint)(BindFlag.RenderTarget | BindFlag.ShaderResource),
            CPUAccessFlags = 0,
            MiscFlags = (uint)ResourceMiscFlag.SharedKeyedmutex
        };
        ThrowHResult(device.CreateTexture2D(&textureDesc, (SubresourceData*)null, texture.GetAddressOf()));
        _texture = texture;

        _mutex = _texture.QueryInterface<IDXGIKeyedMutex>();
        using (var res = _texture.QueryInterface<IDXGIResource>())
        {
            void* handle = null;
            ThrowHResult(res.GetSharedHandle(ref handle));
            _handle = (IntPtr)handle;
        }

        _properties = new PlatformGraphicsExternalImageProperties
        {
            Width = size.Width, Height = size.Height, Format = PlatformGraphicsExternalImageFormat.B8G8R8A8UNorm
        };

        ThrowHResult(device.CreateRenderTargetView(_texture, null, ref _renderTargetView));
    }

    public void BeginDraw()
    {
        _mutex.AcquireSync(0, int.MaxValue);
    }

    public void Present()
    {
        _mutex.ReleaseSync(1);
        _imported ??= _interop.ImportImage(
            new PlatformHandle(_handle, KnownPlatformGraphicsExternalImageHandleTypes.D3D11TextureGlobalSharedHandle),
            _properties);
        LastPresent = _target.UpdateWithKeyedMutexAsync(_imported, 1, 0);
    }


    public async ValueTask DisposeAsync()
    {
        if (LastPresent != null)
            try
            {
                await LastPresent;
            }
            catch
            {
                // Ignore
            }

        _renderTargetView.Dispose();
        _mutex.Dispose();
        _texture.Dispose();
    }
}

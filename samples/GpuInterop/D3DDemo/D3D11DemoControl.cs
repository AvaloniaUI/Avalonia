using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using static Silk.NET.Core.Native.SilkMarshal;

namespace GpuInterop.D3DDemo;

public class D3D11DemoControl : DrawingSurfaceDemoBase
{
    private ComPtr<ID3D11Device> _device;
    private D3D11Swapchain? _swapchain;
    private ComPtr<ID3D11DeviceContext> _context;
    private Matrix4x4 _view;
    private PixelSize _lastSize;
    private ComPtr<ID3D11Texture2D> _depthBuffer;
    private ComPtr<ID3D11DepthStencilView> _depthView;
    private Matrix4x4 _proj;
    private ComPtr<ID3D11Buffer> _constantBuffer;
    private readonly Stopwatch _st = Stopwatch.StartNew();

    protected override unsafe (bool success, string info) InitializeGraphicsResources(Compositor compositor,
        CompositionDrawingSurface surface, ICompositionGpuInterop interop)
    {
        if (interop.SupportedImageHandleTypes.Contains(KnownPlatformGraphicsExternalImageHandleTypes
                .D3D11TextureGlobalSharedHandle) != true)
            return (false, "DXGI shared handle import is not supported by the current graphics backend");

        using var dxgi = new DXGI(DXGI.CreateDefaultContext(["DXGI.dll"]));
        using var d3d11 = new D3D11(D3D11.CreateDefaultContext(["d3d11.dll"]));
        using var factory = dxgi.CreateDXGIFactory1<IDXGIFactory1>();

        using ComPtr<IDXGIAdapter> adapter = default;
        ThrowHResult(factory.EnumAdapters(0, adapter.GetAddressOf()));

        const int featureLevelCount = 8;
        var featureLevels = stackalloc D3DFeatureLevel[featureLevelCount]
        {
            D3DFeatureLevel.Level121,
            D3DFeatureLevel.Level120,
            D3DFeatureLevel.Level111,
            D3DFeatureLevel.Level110,
            D3DFeatureLevel.Level100,
            D3DFeatureLevel.Level93,
            D3DFeatureLevel.Level92,
            D3DFeatureLevel.Level91
        };

        ComPtr<ID3D11Device> device = default;
        ComPtr<ID3D11DeviceContext> context = default;
        D3DFeatureLevel actualFeatureLevel;
        ThrowHResult(d3d11.CreateDevice(
            adapter,
            D3DDriverType.Unknown,
            IntPtr.Zero,
            0u,
            featureLevels,
            featureLevelCount,
            D3D11.SdkVersion,
            device.GetAddressOf(),
            &actualFeatureLevel,
            context.GetAddressOf()));

        _device = device;
        _swapchain = new D3D11Swapchain(device, interop, surface);
        _context = context;
        _constantBuffer = D3DContent.CreateMesh(_device);
        _view = Matrix4x4.CreateLookAtLeftHanded(new Vector3(0, 0, -5), new Vector3(0, 0, 0), Vector3.UnitY);

        AdapterDesc adapterDesc;
        ThrowHResult(adapter.GetDesc(&adapterDesc));
        var description = PtrToString((IntPtr)adapterDesc.Description, NativeStringEncoding.LPWStr);

        return (true, $"D3D11 ({actualFeatureLevel}) {description}");
    }

    protected override void FreeGraphicsResources()
    {
        if (_swapchain is not null)
        {
            _swapchain.DisposeAsync().GetAwaiter().GetResult();
            _swapchain = null;
        }

        _depthView.Dispose();
        _depthBuffer.Dispose();
        _constantBuffer.Dispose();
        _context.Dispose();
        _device.Dispose();
    }

    protected override bool SupportsDisco => true;

    protected override unsafe void RenderFrame(PixelSize pixelSize)
    {
        if (pixelSize == default)
            return;
        if (pixelSize != _lastSize)
        {
            _lastSize = pixelSize;
            Resize(pixelSize);
        }
        using (_swapchain!.BeginDraw(pixelSize, out var renderView))
        {
            var renderViewHandle = renderView.Handle;
            _context.OMSetRenderTargets(1, &renderViewHandle, _depthView);
            var viewProj = _view * _proj;

            var now = _st.Elapsed.TotalSeconds * 5;
            var scaleX = (float)(1f + Disco * (Math.Sin(now) + 1) / 6);
            var scaleY = (float)(1f + Disco * (Math.Cos(now) + 1) / 8);
            var colorOff =(float) (Math.Sin(now) + 1) / 2 * Disco;
            
            // Clear views
            _context.ClearDepthStencilView(_depthView, (uint)ClearFlag.Depth, 1.0f, 0);
            var color = new Vector4(1f - colorOff, colorOff, 0.5f + colorOff * 0.5f, 1.0f);
            _context.ClearRenderTargetView(renderView, (float*)&color);

            // Update WorldViewProj Matrix
            var ypr = Matrix4x4.CreateFromYawPitchRoll(Yaw, Pitch, Roll);
            var worldViewProj = ypr * Matrix4x4.CreateScale(new Vector3(scaleX, scaleY, 1)) * viewProj;
            worldViewProj = Matrix4x4.Transpose(worldViewProj);

            _context.UpdateSubresource(_constantBuffer, 0, null, &worldViewProj, 0, 0);

            // Draw the cube
            _context.Draw(36, 0);
            
            _context.Flush();
        }
    }

    private unsafe void Resize(PixelSize size)
    {
        _depthBuffer.Dispose();

        if (_device.Handle == null)
            return;

        ComPtr<ID3D11Texture2D> depthBuffer = default;
        var textureDesc = new Texture2DDesc
        {
            Format = Format.FormatD32FloatS8X24Uint,
            ArraySize = 1,
            MipLevels = 1,
            Width = (uint)size.Width,
            Height = (uint)size.Height,
            SampleDesc = new SampleDesc(1, 0),
            Usage = Usage.Default,
            BindFlags = (uint)BindFlag.DepthStencil,
            CPUAccessFlags = (uint)CpuAccessFlag.None,
            MiscFlags = (uint)ResourceMiscFlag.None
        };
        ThrowHResult(_device.CreateTexture2D(
            &textureDesc,
            (SubresourceData*)null,
            depthBuffer.GetAddressOf()));

        _depthBuffer = depthBuffer;

        _depthView.Dispose();
        ThrowHResult(_device.CreateDepthStencilView(_depthBuffer, null, ref _depthView));

        // Setup targets and viewport for rendering
        var viewport = new Viewport(0, 0, size.Width, size.Height, 0.0f, 1.0f);
        _context.RSSetViewports(1, &viewport);
        
        // Setup new projection matrix with correct aspect ratio
        _proj = Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded((float)Math.PI / 4.0f, size.Width / (float) size.Height, 0.1f, 100.0f);
    }
}

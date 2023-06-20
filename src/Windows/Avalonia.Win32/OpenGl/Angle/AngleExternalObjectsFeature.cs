using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Egl;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Win32.DirectX;
using MicroCom.Runtime;

namespace Avalonia.Win32.OpenGl.Angle;

internal class AngleExternalObjectsFeature : IGlContextExternalObjectsFeature, IDisposable
{
    private readonly EglContext _context;
    private readonly ID3D11Device _device;
    private readonly ID3D11Device1 _device1;

    public AngleExternalObjectsFeature(EglContext context)
    {
        _context = context;
        var angle = (AngleWin32EglDisplay)context.Display;
        _device = MicroComRuntime.CreateProxyFor<ID3D11Device>(angle.GetDirect3DDevice(), false).CloneReference();
        _device1 = _device.QueryInterface<ID3D11Device1>();
        using var dxgiDevice = _device.QueryInterface<IDXGIDevice>();
        using var adapter = dxgiDevice.Adapter;
        DeviceLuid = BitConverter.GetBytes(adapter.Desc.AdapterLuid);
    }

    public IReadOnlyList<string> SupportedImportableExternalImageTypes { get; } = new[]
    {
        KnownPlatformGraphicsExternalImageHandleTypes.D3D11TextureGlobalSharedHandle,
        KnownPlatformGraphicsExternalImageHandleTypes.D3D11TextureNtHandle,
    };

    public IReadOnlyList<string> SupportedExportableExternalImageTypes => SupportedImportableExternalImageTypes;
    public IReadOnlyList<string> SupportedImportableExternalSemaphoreTypes => Array.Empty<string>();
    public IReadOnlyList<string> SupportedExportableExternalSemaphoreTypes => Array.Empty<string>();

    public IReadOnlyList<PlatformGraphicsExternalImageFormat> GetSupportedFormatsForExternalMemoryType(string type) =>
        new[]
        {
            PlatformGraphicsExternalImageFormat.R8G8B8A8UNorm
        };

    public unsafe IGlExportableExternalImageTexture CreateImage(string type, PixelSize size, 
        PlatformGraphicsExternalImageFormat format)
    {
        if (format != PlatformGraphicsExternalImageFormat.R8G8B8A8UNorm)
            throw new NotSupportedException("Unsupported external memory format");
        using (_context.EnsureCurrent())
        {
            var fmt = DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM;

            var desc = new D3D11_TEXTURE2D_DESC
            {
                Format = fmt,
                Width = (uint)size.Width,
                Height = (uint)size.Height,
                ArraySize = 1,
                MipLevels = 1,
                SampleDesc = new DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
                Usage = D3D11_USAGE.D3D11_USAGE_DEFAULT,
                CPUAccessFlags = 0,
                MiscFlags = D3D11_RESOURCE_MISC_FLAG.D3D11_RESOURCE_MISC_SHARED_KEYEDMUTEX,
                BindFlags = D3D11_BIND_FLAG.D3D11_BIND_RENDER_TARGET | D3D11_BIND_FLAG.D3D11_BIND_SHADER_RESOURCE,
            };
            using var texture = _device.CreateTexture2D(&desc, IntPtr.Zero);
            return new AngleExternalMemoryD3D11ExportedTexture2D(_context, texture, desc, format);
        }
    }

    public IGlExportableExternalImageTexture CreateSemaphore(string type) => throw new NotSupportedException();

    public unsafe IGlExternalImageTexture ImportImage(IPlatformHandle handle, PlatformGraphicsExternalImageProperties properties)
    {
        if (!SupportedImportableExternalImageTypes.Contains(handle.HandleDescriptor))
            throw new NotSupportedException("Unsupported external memory type");
        
        using (_context.EnsureCurrent())
        {
            var guid = MicroComRuntime.GetGuidFor(typeof(ID3D11Texture2D));
            using var opened =
                handle.HandleDescriptor ==
                KnownPlatformGraphicsExternalImageHandleTypes.D3D11TextureGlobalSharedHandle ?
                    _device.OpenSharedResource(handle.Handle, &guid) :
                    _device1.OpenSharedResource1(handle.Handle, &guid);
            using var texture = opened.QueryInterface<ID3D11Texture2D>();
            return new AngleExternalMemoryD3D11Texture2D(_context, texture, properties);
        }
    }

    public IGlExternalSemaphore ImportSemaphore(IPlatformHandle handle) => throw new NotSupportedException();
    public CompositionGpuImportedImageSynchronizationCapabilities GetSynchronizationCapabilities(string imageHandleType)
    {
        if (imageHandleType == KnownPlatformGraphicsExternalImageHandleTypes.D3D11TextureGlobalSharedHandle
            || imageHandleType == KnownPlatformGraphicsExternalImageHandleTypes.D3D11TextureNtHandle)
            return CompositionGpuImportedImageSynchronizationCapabilities.KeyedMutex;
        return default;
    }

    public byte[]? DeviceLuid { get; }
    public byte[]? DeviceUuid => null;

    public void Dispose()
    {
        _device.Dispose();
    }
}

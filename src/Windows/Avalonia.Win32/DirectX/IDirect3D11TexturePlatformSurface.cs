using System;
using Avalonia.OpenGL;
using Avalonia.Platform;

namespace Avalonia.Win32.DirectX;

public interface IDirect3D11TexturePlatformSurface
{
    public IDirect3D11TextureRenderTarget CreateRenderTarget(IPlatformGraphicsContext graphicsContext, IntPtr d3dDevice);
}



public interface IDirect3D11TextureRenderTarget : IDisposable
{
    bool IsCorrupted { get; }
    IDirect3D11TextureRenderTargetRenderSession BeginDraw();
}

public interface IDirect3D11TextureRenderTargetRenderSession : IDisposable
{
    public IntPtr D3D11Texture2D { get; }
    public PixelSize Size { get; }
    public PixelPoint Offset { get; }
    public double Scaling { get; }
}

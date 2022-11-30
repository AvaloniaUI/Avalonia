namespace Avalonia.Win32.DxgiSwapchain
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal unsafe partial struct DXGI_MODE_DESC
    {
        public ushort Width;
        public ushort Height;
        public DXGI_RATIONAL RefreshRate;
        public DXGI_FORMAT Format;
        public DXGI_MODE_SCANLINE_ORDER ScanlineOrdering;
        public DXGI_MODE_SCALING Scaling;
    }
}

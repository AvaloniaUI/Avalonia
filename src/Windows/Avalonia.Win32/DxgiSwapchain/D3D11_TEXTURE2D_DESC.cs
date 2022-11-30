namespace Avalonia.Win32.DxgiSwapchain
{
    internal partial struct D3D11_TEXTURE2D_DESC
    {
        public uint Width;

        public uint Height;

        public uint MipLevels;

        public uint ArraySize;

        public DXGI_FORMAT Format;

        public DXGI_SAMPLE_DESC SampleDesc;

        public D3D11_USAGE Usage;

        public uint BindFlags;

        public uint CPUAccessFlags;

        public uint MiscFlags;
    }
}

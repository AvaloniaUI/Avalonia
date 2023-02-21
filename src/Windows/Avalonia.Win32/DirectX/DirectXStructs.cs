using System;
using static Avalonia.Win32.Interop.UnmanagedMethods;
// ReSharper disable InconsistentNaming
#pragma warning disable CS0649

namespace Avalonia.Win32.DirectX
{
    internal unsafe struct HANDLE
    {
        public readonly void* Value;

        public HANDLE(void* value)
        {
            Value = value;
        }

        public static HANDLE INVALID_VALUE => new HANDLE((void*)(-1));

        public static HANDLE NULL => new HANDLE(null);

        public static bool operator ==(HANDLE left, HANDLE right) => left.Value == right.Value;

        public static bool operator !=(HANDLE left, HANDLE right) => left.Value != right.Value;

        public override bool Equals(object? obj) => (obj is HANDLE other) && Equals(other);

        public bool Equals(HANDLE other) => ((nuint)(Value)).Equals((nuint)(other.Value));

        public override int GetHashCode() => ((nuint)(Value)).GetHashCode();

        public override string ToString() => ((IntPtr)Value).ToString();
    }

    internal unsafe struct MONITORINFOEXW
    {
        internal MONITORINFO Base;

        internal fixed ushort szDevice[32];
    }
    
    internal unsafe struct DEVMODEW
    {
        public fixed ushort dmDeviceName[32];
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public short dmOrientation;
        public short dmPaperSize;
        public short dmPaperLength;
        public short dmPaperWidth;
        public short dmScale;
        public short dmCopies;
        public short dmDefaultSource;
        public short dmPrintQuality;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        public fixed ushort dmFormName[32];
        public short dmUnusedPadding;
        public short dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
    }

    internal unsafe struct DXGI_ADAPTER_DESC
    {
        public fixed ushort Description[128];

        public uint VendorId;

        public uint DeviceId;

        public uint SubSysId;

        public uint Revision;

        public nuint DedicatedVideoMemory;

        public nuint DedicatedSystemMemory;

        public nuint SharedSystemMemory;

        public ulong AdapterLuid;
    }

    internal unsafe struct DXGI_ADAPTER_DESC1
    {
        public fixed ushort Description[128];

        public uint VendorId;

        public uint DeviceId;

        public uint SubSysId;

        public uint Revision;

        public nuint DedicatedVideoMemory;

        public nuint DedicatedSystemMemory;

        public nuint SharedSystemMemory;

        public ulong AdapterLuid;

        public uint Flags;
    }

    internal struct DXGI_FRAME_STATISTICS
    {
        public uint PresentCount;

        public uint PresentRefreshCount;

        public uint SyncRefreshCount;

        public ulong SyncQPCTime;

        public ulong SyncGPUTime;
    }

    internal unsafe struct DXGI_GAMMA_CONTROL_CAPABILITIES
    {
        public int ScaleAndOffsetSupported;

        public float MaxConvertedValue;

        public float MinConvertedValue;

        public uint NumGammaControlPoints;

        public fixed float ControlPointPositions[1025];
    }

    internal unsafe struct DXGI_MAPPED_RECT
    {
        public int Pitch;
        public byte* pBits;
    }

    internal struct DXGI_MODE_DESC
    {
        public ushort Width;
        public ushort Height;
        public DXGI_RATIONAL RefreshRate;
        public DXGI_FORMAT Format;
        public DXGI_MODE_SCANLINE_ORDER ScanlineOrdering;
        public DXGI_MODE_SCALING Scaling;
    }

    internal unsafe struct DXGI_OUTPUT_DESC
    {
        internal fixed ushort DeviceName[32];

        internal RECT DesktopCoordinates;

        internal int AttachedToDesktop; // BOOL maps to int. If we use the CLR 'bool' type here, the struct becomes non-blittable. See #9599

        internal DXGI_MODE_ROTATION Rotation;

        internal HANDLE Monitor;
    }

    internal unsafe struct DXGI_PRESENT_PARAMETERS
    {
        public uint DirtyRectsCount;

        public RECT* pDirtyRects;

        public RECT* pScrollRect;

        public POINT* pScrollOffset;
    }

    internal struct DXGI_RATIONAL
    {
        public ushort Numerator;
        public ushort Denominator;
    }

    internal struct DXGI_RGB
    {
        public float Red;

        public float Green;

        public float Blue;
    }

    internal struct DXGI_RGBA
    {
        public float r;

        public float g;

        public float b;

        public float a;
    }

    internal struct DXGI_SAMPLE_DESC
    {
        public uint Count;
        public uint Quality;
    }

    internal struct DXGI_SURFACE_DESC
    {
        public uint Width;

        public uint Height;

        public DXGI_FORMAT Format;

        public DXGI_SAMPLE_DESC SampleDesc;
    }

    internal struct DXGI_SWAP_CHAIN_DESC
    {
        public DXGI_MODE_DESC BufferDesc;
        public DXGI_SAMPLE_DESC SampleDesc;
        public uint BufferUsage;
        public ushort BufferCount;
        public IntPtr OutputWindow;
        public int Windowed;
        public DXGI_SWAP_EFFECT SwapEffect;
        public ushort Flags;
    }

    internal struct DXGI_SWAP_CHAIN_DESC1
    {
        public uint Width;
        public uint Height;
        public DXGI_FORMAT Format;
        public int Stereo; // BOOL maps to int. If we use the CLR 'bool' type here, the struct becomes non-blittable. See #9599
        public DXGI_SAMPLE_DESC SampleDesc;
        public uint BufferUsage;
        public uint BufferCount;
        public DXGI_SCALING Scaling;
        public DXGI_SWAP_EFFECT SwapEffect;
        public DXGI_ALPHA_MODE AlphaMode;
        public uint Flags;
    }

    internal struct DXGI_SWAP_CHAIN_FULLSCREEN_DESC
    {
        public DXGI_RATIONAL RefreshRate;

        public DXGI_MODE_SCANLINE_ORDER ScanlineOrdering;

        public DXGI_MODE_SCALING Scaling;

        public int Windowed;
    }

    internal struct D3D11_TEXTURE2D_DESC
    {
        public uint Width;

        public uint Height;

        public uint MipLevels;

        public uint ArraySize;

        public DXGI_FORMAT Format;

        public DXGI_SAMPLE_DESC SampleDesc;

        public D3D11_USAGE Usage;

        public D3D11_BIND_FLAG BindFlags;

        public uint CPUAccessFlags;

        public D3D11_RESOURCE_MISC_FLAG MiscFlags;
    }
}

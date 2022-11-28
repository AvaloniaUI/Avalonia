using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Win32.DxgiSwapchain
{
    internal unsafe struct IDXGIFactory
    {
        // some of this code looks ugly. It can look less ugly when we all leave NetFX in the past. 
        internal void** lpVtbl;

        internal HRESULT QueryInterface(Guid* riid, void** ppvObject)
        {
#if NET6_0_OR_GREATER
            return ((delegate* unmanaged<IDXGIFactory*, Guid*, void**, int>)(lpVtbl[0]))((IDXGIFactory*)Unsafe.AsPointer(ref this), riid, ppvObject);
#else
            return ((delegate* unmanaged[Stdcall]<IDXGIFactory*, Guid*, void**, int>)(lpVtbl[0]))((IDXGIFactory*)Unsafe.AsPointer(ref this), riid, ppvObject);
#endif
        }

        // several entries ommitted for breviety ... 

        internal uint Release()
        {
#if NET6_0_OR_GREATER
            return ((delegate* unmanaged<IDXGIFactory*, uint>)(lpVtbl[2]))((IDXGIFactory*)Unsafe.AsPointer(ref this));
#else
            return ((delegate* unmanaged[Stdcall]<IDXGIFactory*, uint>)(lpVtbl[2]))((IDXGIFactory*)Unsafe.AsPointer(ref this));
#endif
        }

        internal HRESULT EnumAdapters(uint Adapter, IDXGIAdapter** ppAdapter)
        {
#if NET6_0_OR_GREATER
            return ((delegate* unmanaged<IDXGIFactory*, uint, IDXGIAdapter**, int>)(lpVtbl[7]))((IDXGIFactory*)Unsafe.AsPointer(ref this), Adapter, ppAdapter);
#else
            return ((delegate* unmanaged[Stdcall]<IDXGIFactory*, uint, IDXGIAdapter**, int>)(lpVtbl[7]))((IDXGIFactory*)Unsafe.AsPointer(ref this), Adapter, ppAdapter);
#endif
        }
    }
}

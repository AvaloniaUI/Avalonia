using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Win32.DxgiSwapchain
{
    internal unsafe struct IDXGIAdapter
    {
        internal void** lpVtbl;

        internal HRESULT QueryInterface(Guid* riid, void** ppvObject)
        {
            return ((delegate* unmanaged[Stdcall]<IDXGIAdapter*, Guid*, void**, int>)(lpVtbl[0]))((IDXGIAdapter*)Unsafe.AsPointer(ref this), riid, ppvObject);
        }

        internal uint Release()
        {
            return ((delegate* unmanaged[Stdcall]<IDXGIAdapter*, uint>)(lpVtbl[2]))((IDXGIAdapter*)Unsafe.AsPointer(ref this));
        }

        public HRESULT GetParent(Guid* riid, void** ppParent)
        {
            return ((delegate* unmanaged[Stdcall]<IDXGIAdapter*, Guid*, void**, int>)(lpVtbl[6]))((IDXGIAdapter*)Unsafe.AsPointer(ref this), riid, ppParent);
        }

        internal HRESULT EnumOutputs(uint Output, IDXGIOutput** ppOutput)
        {
            return ((delegate* unmanaged[Stdcall]<IDXGIAdapter*, uint, IDXGIOutput**, int>)(lpVtbl[7]))((IDXGIAdapter*)Unsafe.AsPointer(ref this), Output, ppOutput);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Win32.DxgiSwapchain
{
    internal unsafe struct IDXGIOutput
    {
        internal void** lpVtbl;

        internal HRESULT QueryInterface(Guid* riid, void** ppvObject)
        {
            return ((delegate* unmanaged[Stdcall]<IDXGIOutput*, Guid*, void**, int>)(lpVtbl[0]))((IDXGIOutput*)Unsafe.AsPointer(ref this), riid, ppvObject);
        }

        internal uint Release()
        {
            return ((delegate* unmanaged[Stdcall]<IDXGIOutput*, uint>)(lpVtbl[2]))((IDXGIOutput*)Unsafe.AsPointer(ref this));
        }

        internal HRESULT GetDesc(DXGI_OUTPUT_DESC* pDesc)
        {
            return ((delegate* unmanaged[Stdcall]<IDXGIOutput*, DXGI_OUTPUT_DESC*, int>)(lpVtbl[7]))((IDXGIOutput*)Unsafe.AsPointer(ref this), pDesc);
        }

        internal HRESULT WaitForVBlank()
        {
            return ((delegate* unmanaged[Stdcall]<IDXGIOutput*, int>)(lpVtbl[10]))((IDXGIOutput*)Unsafe.AsPointer(ref this));
        }
    }
}

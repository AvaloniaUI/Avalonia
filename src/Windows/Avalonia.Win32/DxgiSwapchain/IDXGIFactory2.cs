using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using MicroCom.Runtime;

namespace Avalonia.Win32.DxgiSwapchain
{
    internal unsafe struct IDXGIFactory2
    {
        public static Guid Guid = Guid.Parse("50C83A1C-E072-4C48-87B0-3630FA36A6D0");

        void** lpVtbl;

        public HRESULT QueryInterface(Guid* riid, void** ppvObject)
        {
            return ((delegate* unmanaged[Stdcall]<IDXGIFactory2*, Guid*, void**, int>)(lpVtbl[0]))((IDXGIFactory2*)Unsafe.AsPointer(ref this), riid, ppvObject);
        }

        public uint Release()
        {
            return ((delegate* unmanaged[Stdcall]<IDXGIFactory2*, uint>)(lpVtbl[2]))((IDXGIFactory2*)Unsafe.AsPointer(ref this));
        }

        public HRESULT GetParent(Guid* riid, void** ppParent)
        {
            return ((delegate* unmanaged[Stdcall]<IDXGIFactory2*, Guid*, void**, int>)(lpVtbl[6]))((IDXGIFactory2*)Unsafe.AsPointer(ref this), riid, ppParent);
        }

        public HRESULT EnumAdapters(uint Adapter, IDXGIAdapter** ppAdapter)
        {
            return ((delegate* unmanaged[Stdcall]<IDXGIFactory2*, uint, IDXGIAdapter**, int>)(lpVtbl[7]))((IDXGIFactory2*)Unsafe.AsPointer(ref this), Adapter, ppAdapter);
        }

        // dev-note: dropped DXGI_SWAP_CHAIN_FULLSCREEN_DESC because we're not interested in that 
        public HRESULT CreateSwapChainForHwnd(void* pDevice, IntPtr hWnd, DXGI_SWAP_CHAIN_DESC1* pDesc, void* pFullscreenDesc, IDXGIOutput* pRestrictToOutput, IDXGISwapChain1** ppSwapChain)
        {
            return ((delegate* unmanaged[Stdcall]<IDXGIFactory2*, void*, IntPtr, DXGI_SWAP_CHAIN_DESC1*, void*, IDXGIOutput*, IDXGISwapChain1**, int>)(lpVtbl[15]))((IDXGIFactory2*)Unsafe.AsPointer(ref this), pDevice, hWnd, pDesc, pFullscreenDesc, pRestrictToOutput, ppSwapChain);
        }
    }
}

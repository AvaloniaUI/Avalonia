using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Win32.DxgiSwapchain
{
    internal unsafe struct IDXGIDevice
    {
        public static Guid Guid = Guid.Parse("54EC77FA-1377-44E6-8C32-88FD5F44C84C");

        void** lpVtbl;

        public HRESULT QueryInterface(Guid* riid, void** ppvObject)
        {
            return ((delegate* unmanaged[Stdcall]<IDXGIDevice*, Guid*, void**, int>)(lpVtbl[0]))((IDXGIDevice*)Unsafe.AsPointer(ref this), riid, ppvObject);
        }

        public uint Release()
        {
            return ((delegate* unmanaged[Stdcall]<IDXGIDevice*, uint>)(lpVtbl[2]))((IDXGIDevice*)Unsafe.AsPointer(ref this));
        }

        public HRESULT GetAdapter(IDXGIAdapter** pAdapter)
        {
            return ((delegate* unmanaged[Stdcall]<IDXGIDevice*, IDXGIAdapter**, int>)(lpVtbl[7]))((IDXGIDevice*)Unsafe.AsPointer(ref this), pAdapter);
        }


    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Win32.DxgiSwapchain
{
    internal unsafe struct ID3D11Device
    {
        public void** lpVtbl;

        public HRESULT QueryInterface(Guid* riid, void** ppvObject)
        {
            return ((delegate* unmanaged[Stdcall]<ID3D11Device*, Guid*, void**, int>)(lpVtbl[0]))((ID3D11Device*)Unsafe.AsPointer(ref this), riid, ppvObject);
        }

        public uint Release()
        {
            return ((delegate* unmanaged[Stdcall]<ID3D11Device*, uint>)(lpVtbl[2]))((ID3D11Device*)Unsafe.AsPointer(ref this));
        }


    }
}

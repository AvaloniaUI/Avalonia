using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Win32.DxgiSwapchain
{
    internal unsafe struct ID3D11Texture2D
    {
        public static Guid Guid = Guid.Parse("6F15AAF2-D208-4E89-9AB4-489535D34F9C");

        void ** lpVtbl;

        public HRESULT QueryInterface(Guid* riid, void** ppvObject)
        {
            return ((delegate* unmanaged[Stdcall]<ID3D11Texture2D*, Guid*, void**, int>)(lpVtbl[0]))((ID3D11Texture2D*)Unsafe.AsPointer(ref this), riid, ppvObject);
        }

        public uint Release()
        {
            return ((delegate* unmanaged[Stdcall]<ID3D11Texture2D*, uint>)(lpVtbl[2]))((ID3D11Texture2D*)Unsafe.AsPointer(ref this));
        }


    }
}

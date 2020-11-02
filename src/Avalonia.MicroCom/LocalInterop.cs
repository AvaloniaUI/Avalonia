using System;

namespace Avalonia.MicroCom
{
    unsafe class LocalInterop
    {
        public static unsafe void CalliStdCallvoid(void* thisObject, void* methodPtr)
        {
            throw null;
        }
        
        public static unsafe int CalliStdCallint(void* thisObject, Guid* guid, IntPtr* ppv,  void* methodPtr)
        {
            throw null;
        }
    }
}

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MicroCom.Runtime;

namespace Avalonia.Win32.WinRT
{
    internal class WinRTInspectable : IInspectable, IMicroComShadowContainer
    {
        public virtual void Dispose()
        {
            
        }

        public unsafe void GetIids(ulong* iidCount, Guid** iids)
        {
            var interfaces = GetType().GetInterfaces().Where(typeof(IUnknown).IsAssignableFrom)
                .Select(MicroComRuntime.GetGuidFor).ToArray();
            var mem = (Guid*)Marshal.AllocCoTaskMem(Unsafe.SizeOf<Guid>() * interfaces.Length);
            for (var c = 0; c < interfaces.Length; c++)
                mem[c] = interfaces[c];
            *iids = mem;
            *iidCount = (ulong) interfaces.Length;
        }

        public IntPtr RuntimeClassName => NativeWinRTMethods.WindowsCreateString(GetType().FullName!);
        public TrustLevel TrustLevel => TrustLevel.BaseTrust;
        public MicroComShadow? Shadow { get; set; }
        public virtual void OnReferencedFromNative()
        {
        }

        public virtual void OnUnreferencedFromNative()
        {
        }
    }
}

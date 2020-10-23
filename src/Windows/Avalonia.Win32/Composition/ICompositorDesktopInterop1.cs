using WinRT;

namespace ABI.Windows.UI.Composition.Desktop
{
    using global::System;
    using global::System.Runtime.InteropServices;

    [Guid("29E691FA-4567-4DCA-B319-D0F207EB6807")]
    internal class ICompositorDesktopInterop : global::Windows.UI.Composition.Desktop.ICompositorDesktopInterop

    {
        [Guid("29E691FA-4567-4DCA-B319-D0F207EB6807")]
        public struct Vftbl
        {
            public delegate int _CreateDesktopWindowTarget(IntPtr thisPtr, IntPtr hwndTarget, byte isTopMost, out IntPtr desktopWindowTarget);

            internal global::WinRT.Interop.IUnknownVftbl IUnknownVftbl;
            public _CreateDesktopWindowTarget CreateDesktopWindowTarget;


            public static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

            static Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IUnknownVftbl = global::WinRT.Interop.IUnknownVftbl.AbiToProjectionVftbl,
                    CreateDesktopWindowTarget = Do_Abi_Create_Desktop_Window_Target
                };
                AbiToProjectionVftablePtr = Marshal.AllocHGlobal(Marshal.SizeOf<Vftbl>());
                Marshal.StructureToPtr(AbiToProjectionVftable, AbiToProjectionVftablePtr, false);
            }

            private static int Do_Abi_Create_Desktop_Window_Target(IntPtr thisPtr, IntPtr hwndTarget, byte isTopMost, out IntPtr desktopWindowTarget)
            {
                try
                {
                    ComWrappersSupport.FindObject<global::Windows.UI.Composition.Desktop.ICompositorDesktopInterop>(thisPtr).CreateDesktopWindowTarget(hwndTarget, isTopMost != 0, out desktopWindowTarget);
                    return 0;
                }
                catch (Exception ex)
                {
                    desktopWindowTarget = IntPtr.Zero;
                    return Marshal.GetHRForException(ex);
                }
            }
        }
        internal static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator ICompositorDesktopInterop(IObjectReference obj) => (obj != null) ? new ICompositorDesktopInterop(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public ICompositorDesktopInterop(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        internal ICompositorDesktopInterop(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public void CreateDesktopWindowTarget(IntPtr hwndTarget, bool isTopmost, out IntPtr test)
        {
            Marshal.ThrowExceptionForHR(_obj.Vftbl.CreateDesktopWindowTarget(ThisPtr, hwndTarget, isTopmost ? (byte)1 : (byte)0, out test));
        }
    }
}


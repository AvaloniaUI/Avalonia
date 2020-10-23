using System;
using System.Runtime.InteropServices;
using WinRT;

namespace Windows.UI.Composition.Interop
{
    [WindowsRuntimeType]
    [Guid("25297D5C-3AD4-4C9C-B5CF-E36A38512330")]    
    public interface ICompositorInterop
    {
        ICompositionSurface CreateCompositionSurfaceForHandle(IntPtr swapChain);

        ICompositionSurface CreateCompositionSurfaceForSwapChain(IntPtr swapChain);

        CompositionGraphicsDevice CreateGraphicsDevice(IntPtr renderingDevice);
    }
}

namespace ABI.Windows.UI.Composition.Interop
{
    using global::System;
    using global::System.Runtime.InteropServices;
    using global::Windows.UI.Composition;

    [Guid("25297D5C-3AD4-4C9C-B5CF-E36A38512330")]
    internal class ICompositorInterop : global::Windows.UI.Composition.Interop.ICompositorInterop

    {
        [Guid("25297D5C-3AD4-4C9C-B5CF-E36A38512330")]
        public struct Vftbl
        {
            public delegate int _CreateCompositionSurfaceForHandle(IntPtr ThisPtr, IntPtr swapChain, out IntPtr result);
            public delegate int _CreateCompositionSurfaceForSwapChain(IntPtr ThisPtr, IntPtr swapChain, out IntPtr result);
            public delegate int _CreateGraphicsDevice(IntPtr ThisPtr, IntPtr renderingDevice, out IntPtr result);

            internal global::WinRT.Interop.IUnknownVftbl IUnknownVftbl;
            public _CreateCompositionSurfaceForHandle CreateCompositionSurfaceForHandle;
            public _CreateCompositionSurfaceForSwapChain CreateCompositionSurfaceForSwapChain;
            public _CreateGraphicsDevice CreateGraphicsDevice;


            public static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

            static Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IUnknownVftbl = global::WinRT.Interop.IUnknownVftbl.AbiToProjectionVftbl,

                    CreateCompositionSurfaceForHandle = Do_Abi_Create_Composition_Surface_For_Handle,
                    CreateCompositionSurfaceForSwapChain = Do_Abi_Create_Composition_Surface_For_SwapChain,
                    CreateGraphicsDevice= Do_Abi_Create_Graphics_Device
                };
                AbiToProjectionVftablePtr = Marshal.AllocHGlobal(Marshal.SizeOf<Vftbl>());
                Marshal.StructureToPtr(AbiToProjectionVftable, AbiToProjectionVftablePtr, false);
            }

            private static int Do_Abi_Create_Composition_Surface_For_Handle(IntPtr thisPtr, IntPtr swapChain, out IntPtr surface)
            {
                try
                {
                    surface = IntPtr.Zero;
                    //surface = ComWrappersSupport.FindObject<global::Windows.UI.Composition.Interop.ICompositorInterop>(thisPtr).CreateCompositionSurfaceForHandle(swapChain);
                    return 0;
                }
                catch (Exception ex)
                {
                    surface = IntPtr.Zero;
                    return Marshal.GetHRForException(ex);
                }
            }

            private static int Do_Abi_Create_Composition_Surface_For_SwapChain(IntPtr thisPtr, IntPtr swapChain, out IntPtr surface)
            {
                try
                {
                    surface = IntPtr.Zero;
                    //surface = ComWrappersSupport.FindObject<global::Windows.UI.Composition.Interop.ICompositorInterop>(thisPtr).CreateCompositionSurfaceForSwapChain(swapChain);
                    return 0;
                }
                catch (Exception ex)
                {
                    surface = IntPtr.Zero;                    
                    return Marshal.GetHRForException(ex);
                }
            }

            private static int Do_Abi_Create_Graphics_Device(IntPtr thisPtr, IntPtr renderingDevice, out IntPtr graphicsDevice)
            {
                try
                {
                    graphicsDevice = ComWrappersSupport.FindObject<global::Windows.UI.Composition.Interop.ICompositorInterop>(thisPtr).CreateGraphicsDevice(renderingDevice).ThisPtr;
                    return 0;
                }
                catch (Exception ex)
                {
                    graphicsDevice = IntPtr.Zero;
                    return Marshal.GetHRForException(ex);
                }
            }
        }
        internal static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator ICompositorInterop(IObjectReference obj) => (obj != null) ? new ICompositorInterop(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public ICompositorInterop(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        internal ICompositorInterop(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public ICompositionSurface CreateCompositionSurfaceForHandle(IntPtr swapChain)
        {            
            Marshal.ThrowExceptionForHR(_obj.Vftbl.CreateCompositionSurfaceForHandle(ThisPtr, swapChain, out var compositionSurface));
            
            return null;
        }

        public ICompositionSurface CreateCompositionSurfaceForSwapChain(IntPtr swapChain)
        {
            Marshal.ThrowExceptionForHR(_obj.Vftbl.CreateCompositionSurfaceForSwapChain(ThisPtr, swapChain, out var compositionSurface));

            return null;            
        }

        public CompositionGraphicsDevice CreateGraphicsDevice(IntPtr renderingDevice)
        {
            Marshal.ThrowExceptionForHR(_obj.Vftbl.CreateGraphicsDevice(ThisPtr, renderingDevice, out var graphicsDevice));

            return CompositionGraphicsDevice.FromAbi(graphicsDevice);
        }
    }
}


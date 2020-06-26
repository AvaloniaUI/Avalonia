using System;
using System.Runtime.InteropServices;
using WinRT;

namespace Windows.UI.Composition.Interop
{
    public struct POINT
    {
        public int X;
        public int Y;
    }

    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;

        public int Width => right - left;
        public int Height => bottom - top;
    }

    [WindowsRuntimeType]
    [Guid("FD04E6E3-FE0C-4C3C-AB19-A07601A576EE")]
    public interface ICompositionDrawingSurfaceInterop
    {
        void BeginDraw(ref RECT updateRect, ref Guid iid, out IntPtr updateObject, ref POINT point);

        void EndDraw();

        void Resize(POINT sizePixels);

        void ResumeDraw();

        void Scroll(RECT scrollRect, RECT clipRect, int offsetX, int offsetY);

        void SuspendDraw();
    }
}

namespace ABI.Windows.UI.Composition.Interop
{
    using global::System;
    using global::System.Runtime.InteropServices;
    using global::Windows.UI.Composition;
    using global::Windows.UI.Composition.Interop;

    [Guid("FD04E6E3-FE0C-4C3C-AB19-A07601A576EE")]
    internal class ICompositionDrawingSurfaceInterop : global::Windows.UI.Composition.Interop.ICompositionDrawingSurfaceInterop

    {
        [Guid("FD04E6E3-FE0C-4C3C-AB19-A07601A576EE")]
        public struct Vftbl
        {
            public delegate int _BeginDraw(IntPtr ThisPtr, ref RECT updateRect, ref Guid iid, out IntPtr updateObject, ref POINT updateOffset);
            public delegate int _EndDraw(IntPtr ThisPtr);
            public delegate int _Resize(IntPtr ThisPtr, POINT sizePixels);
            public delegate int _ResumeDraw(IntPtr ThisPtr);
            public delegate int _Scroll(IntPtr ThisPtr, RECT scrollRect, RECT clipRect, int offsetX, int offsetY);
            public delegate int _SuspendDraw(IntPtr ThisPtr);

            internal global::WinRT.Interop.IUnknownVftbl IUnknownVftbl;
            public _BeginDraw BeginDraw;
            public _EndDraw EndDraw;
            public _Resize Resize;
            public _ResumeDraw ResumeDraw;
            public _Scroll Scroll;
            public _SuspendDraw SuspendDraw;

            public static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

            static Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IUnknownVftbl = global::WinRT.Interop.IUnknownVftbl.AbiToProjectionVftbl,

                    BeginDraw = Do_Abi_BeginDraw,
                    EndDraw = Do_Abi_EndDraw,
                    Resize = Do_Abi_Resize


                };
                AbiToProjectionVftablePtr = Marshal.AllocHGlobal(Marshal.SizeOf<Vftbl>());
                Marshal.StructureToPtr(AbiToProjectionVftable, AbiToProjectionVftablePtr, false);
            }

            private static int Do_Abi_BeginDraw(IntPtr ThisPtr, ref RECT updateRect, ref Guid iid, out IntPtr updateObject, ref POINT updateOffset)
            {
                updateObject = IntPtr.Zero;
                return 0;
            }

            private static int Do_Abi_EndDraw(IntPtr ThisPtr)
            {
                return 0;
            }

            private static int Do_Abi_Resize(IntPtr ThisPtr, POINT sizePixels)
            {
                return 0;
            }
        }
        internal static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator ICompositionDrawingSurfaceInterop(IObjectReference obj) => (obj != null) ? new ICompositionDrawingSurfaceInterop(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();

        public ICompositionDrawingSurfaceInterop(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        internal ICompositionDrawingSurfaceInterop(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public void BeginDraw(ref RECT updateRect, ref Guid iid, out IntPtr updateObject, ref POINT point)
        {
            Marshal.ThrowExceptionForHR(_obj.Vftbl.BeginDraw(ThisPtr, ref updateRect, ref iid, out updateObject, ref point));
        }

        public void EndDraw()
        {
            Marshal.ThrowExceptionForHR(_obj.Vftbl.EndDraw(ThisPtr));
        }

        public void Resize(POINT sizePixels)
        {
            Marshal.ThrowExceptionForHR(_obj.Vftbl.Resize(ThisPtr, sizePixels));
        }

        public void ResumeDraw()
        {
            throw new NotImplementedException();
        }

        public void Scroll(RECT scrollRect, RECT clipRect, int offsetX, int offsetY)
        {
            throw new NotImplementedException();
        }

        public void SuspendDraw()
        {
            throw new NotImplementedException();
        }        
    }
}


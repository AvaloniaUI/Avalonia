using WinRT;

namespace ABI.Windows.Graphics.Effects.Interop
{
    using global::System;
    using global::System.Runtime.InteropServices;

    [Guid("2FC57384-A068-44D7-A331-30982FCF7177")]
    internal class IGraphicsEffectD2D1Interop : global::Windows.Graphics.Effects.Interop.IGraphicsEffectD2D1Interop

    {
        [Guid("2FC57384-A068-44D7-A331-30982FCF7177")]
        public struct Vftbl
        {
            public delegate int _GetEffectId(IntPtr thisPtr, out Guid guid);
            public delegate int _GetNamedPropertyMapping(IntPtr thisPtr, IntPtr name, IntPtr index, IntPtr mapping);
            public delegate int _GetProperty(IntPtr thisPtr, uint index, out IntPtr value);
            public unsafe delegate int _GetPropertyCount(IntPtr thisPtr, uint* count);
            public delegate int _GetSource(IntPtr thisPtr, uint index, out IntPtr source);
            public delegate int _GetSourceCount(IntPtr thisPtr, out uint count);

            internal global::WinRT.Interop.IUnknownVftbl IUnknownVftbl;
            public _GetEffectId GetEffectId;
            public _GetNamedPropertyMapping GetNamedPropertyMapping;
            public _GetPropertyCount GetPropertyCount;
            public _GetProperty GetProperty;
            public _GetSource GetSource;
            public _GetSourceCount GetSourceCount;

            public static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

            unsafe static Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IUnknownVftbl = global::WinRT.Interop.IUnknownVftbl.AbiToProjectionVftbl,
                    GetEffectId = Do_Abi_Get_Effect_Id,
                    GetNamedPropertyMapping = Do_Abi_Get_Property_Mapping,
                    GetPropertyCount = Do_Abi_Get_Property_Count,
                    GetProperty = Do_Abi_Get_Property,
                    GetSource = Do_Abi_Get_Source,
                    GetSourceCount = Do_Abi_Get_Source_Count

                };
                AbiToProjectionVftablePtr = Marshal.AllocHGlobal(Marshal.SizeOf<Vftbl>());
                Marshal.StructureToPtr(AbiToProjectionVftable, AbiToProjectionVftablePtr, false);
            }

            private static int Do_Abi_Get_Effect_Id(IntPtr thisPtr, out Guid guid)
            {
                guid = default;

                try
                {
                    guid = ComWrappersSupport.FindObject<global::Windows.Graphics.Effects.Interop.IGraphicsEffectD2D1Interop>(thisPtr).EffectId;
                }
                catch (Exception ex)
                {
                    return Marshal.GetHRForException(ex);
                }

                return 0;
            }

            private static int Do_Abi_Get_Property_Mapping(IntPtr thisPtr, IntPtr name, IntPtr index, IntPtr mapping)
            {
                try
                {
                    ComWrappersSupport.FindObject<global::Windows.Graphics.Effects.Interop.IGraphicsEffectD2D1Interop>(thisPtr).GetNamedPropertyMapping(MarshalString.FromAbi(name), out var mappingResult);
                }
                catch (Exception ex)
                {
                    return Marshal.GetHRForException(ex);
                }

                return 0;
            }

            private static int Do_Abi_Get_Property(IntPtr thisPtr, uint index, out IntPtr value)
            {
                value = default;

                try
                {
                    value = MarshalInspectable.CreateMarshaler(
                        ComWrappersSupport.FindObject<global::Windows.Graphics.Effects.Interop.IGraphicsEffectD2D1Interop>(thisPtr).GetProperty(index))
                        .As(Guid.Parse("4BD682DD-7554-40E9-9A9B-82654EDE7E62"))
                        .GetRef();

                }
                catch (Exception ex)
                {
                    return Marshal.GetHRForException(ex);
                }

                return 0;
            }

            unsafe private static int Do_Abi_Get_Property_Count(IntPtr thisPtr, uint* count)
            {

                try
                {
                    var res = ComWrappersSupport.FindObject<global::Windows.Graphics.Effects.Interop.IGraphicsEffectD2D1Interop>(thisPtr).PropertyCount;

                    if (count != null)
                    {
                        *count = res;
                    }
                }
                catch (Exception ex)
                {
                    return Marshal.GetHRForException(ex);
                }

                return 0;
            }

            private static int Do_Abi_Get_Source(IntPtr thisPtr, uint index, out IntPtr value)
            {
                value = default;

                try
                {                    
                    var source = ComWrappersSupport.FindObject<global::Windows.Graphics.Effects.Interop.IGraphicsEffectD2D1Interop>(thisPtr).GetSource(index);

                    value = MarshalInterface<global::Windows.Graphics.Effects.IGraphicsEffectSource>.FromManaged(source);
                }
                catch (Exception ex)
                {
                    return Marshal.GetHRForException(ex);
                }

                return 0;
            }

            private static int Do_Abi_Get_Source_Count(IntPtr thisPtr, out uint count)
            {
                count = default;

                try
                {
                    count = ComWrappersSupport.FindObject<global::Windows.Graphics.Effects.Interop.IGraphicsEffectD2D1Interop>(thisPtr).SourceCount;
                }
                catch (Exception ex)
                {
                    return Marshal.GetHRForException(ex);
                }

                return 0;
            }
        }
        internal static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator IGraphicsEffectD2D1Interop(IObjectReference obj) => (obj != null) ? new IGraphicsEffectD2D1Interop(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }
        public IntPtr ThisPtr => _obj.ThisPtr;

        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public IGraphicsEffectD2D1Interop(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        internal IGraphicsEffectD2D1Interop(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public Guid EffectId
        {
            get
            {
                Marshal.ThrowExceptionForHR(_obj.Vftbl.GetEffectId(ThisPtr, out Guid guid));
                return guid;
            }
        }

        public uint PropertyCount
        {
            get
            {
                unsafe
                {
                    uint count = default;
                    Marshal.ThrowExceptionForHR(_obj.Vftbl.GetPropertyCount(ThisPtr, &count));
                    return count;
                }
            }
        }

        public uint SourceCount
        {
            get
            {
                Marshal.ThrowExceptionForHR(_obj.Vftbl.GetSourceCount(ThisPtr, out uint count));
                return count;
            }
        }

        public uint GetNamedPropertyMapping(string name, out global::Windows.Graphics.Effects.Interop.GRAPHICS_EFFECT_PROPERTY_MAPPING mapping)
        {
            throw new NotImplementedException();
        }

        public object GetProperty(uint index)
        {
            // Marshal.ThrowExceptionForHR(_obj.Vftbl.GetProperty(ThisPtr, index, out IntPtr value));
            throw new NotImplementedException();
        }

        public global::Windows.Graphics.Effects.IGraphicsEffectSource GetSource(uint index)
        {
            throw new NotImplementedException();
        }
    }
}


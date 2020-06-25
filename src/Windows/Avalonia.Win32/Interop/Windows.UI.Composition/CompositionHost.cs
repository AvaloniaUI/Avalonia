using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ABI.Windows.System;
using ABI.Windows.UI.Composition.Desktop;
using Avalonia.Controls;
using SharpDX.Direct2D1;
using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;
using Windows.UI.Composition;
using WinRT;

namespace Windows.UI.Composition.Desktop
{
    using global::System;

    [WindowsRuntimeType]
    [Guid("29E691FA-4567-4DCA-B319-D0F207EB6807")]
    public interface ICompositorDesktopInterop
    {
        void CreateDesktopWindowTarget(IntPtr hwndTarget, bool isTopmost, out IntPtr test);
    }
}

namespace Windows.Graphics.Effects.Interop
{
    public enum GRAPHICS_EFFECT_PROPERTY_MAPPING
    {
        GRAPHICS_EFFECT_PROPERTY_MAPPING_UNKNOWN,
        GRAPHICS_EFFECT_PROPERTY_MAPPING_DIRECT,
        GRAPHICS_EFFECT_PROPERTY_MAPPING_VECTORX,
        GRAPHICS_EFFECT_PROPERTY_MAPPING_VECTORY,
        GRAPHICS_EFFECT_PROPERTY_MAPPING_VECTORZ,
        GRAPHICS_EFFECT_PROPERTY_MAPPING_VECTORW,
        GRAPHICS_EFFECT_PROPERTY_MAPPING_RECT_TO_VECTOR4,
        GRAPHICS_EFFECT_PROPERTY_MAPPING_RADIANS_TO_DEGREES,
        GRAPHICS_EFFECT_PROPERTY_MAPPING_COLORMATRIX_ALPHA_MODE,
        GRAPHICS_EFFECT_PROPERTY_MAPPING_COLOR_TO_VECTOR3,
        GRAPHICS_EFFECT_PROPERTY_MAPPING_COLOR_TO_VECTOR4
    };

    [WindowsRuntimeType]
    [Guid("2FC57384-A068-44D7-A331-30982FCF7177")]
    public interface IGraphicsEffectD2D1Interop
    {
        Guid EffectId { get; }

        uint GetNamedPropertyMapping(string name, out GRAPHICS_EFFECT_PROPERTY_MAPPING mapping);

        object GetProperty(uint index);

        uint PropertyCount { get; }

        IGraphicsEffectSource GetSource(uint index);

        uint SourceCount { get; }
    }
}

namespace ABI.Windows.Graphics.Effects.Interop
{
    using global::System;
    using global::System.Runtime.InteropServices;
    using global::Windows.UI.Composition.Desktop;

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

namespace ABI.Windows.UI.Composition.Desktop
{
    using global::System;
    using global::System.Runtime.InteropServices;
    using global::Windows.UI.Composition.Desktop;

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

namespace Avalonia.Win32.Interop.WindowsComposition
{
    enum D2D1_GAUSSIANBLUR_PROP
    {
        D2D1_GAUSSIANBLUR_PROP_STANDARD_DEVIATION,
        D2D1_GAUSSIANBLUR_PROP_OPTIMIZATION,
        D2D1_GAUSSIANBLUR_PROP_BORDER_MODE,
        D2D1_GAUSSIANBLUR_PROP_FORCE_DWORD
    };

    class GaussianBlurEffect : IGraphicsEffect, IGraphicsEffectSource, global::Windows.Graphics.Effects.Interop.IGraphicsEffectD2D1Interop
    {
        public static readonly Guid CLSID_D2D12DAffineTransform = new Guid(0x6AA97485, 0x6354, 0x4CFC, 0x90, 0x8C, 0xE4, 0xA7, 0x4F, 0x62, 0xC9, 0x6C);

        public static readonly Guid CLSID_D2D13DPerspectiveTransform = new Guid(0xC2844D0B, 0x3D86, 0x46E7, 0x85, 0xBA, 0x52, 0x6C, 0x92, 0x40, 0xF3, 0xFB);

        public static readonly Guid CLSID_D2D13DTransform = new Guid(0xE8467B04, 0xEC61, 0x4B8A, 0xB5, 0xDE, 0xD4, 0xD7, 0x3D, 0xEB, 0xEA, 0x5A);

        public static readonly Guid CLSID_D2D1ArithmeticComposite = new Guid(0xFC151437, 0x049A, 0x4784, 0xA2, 0x4A, 0xF1, 0xC4, 0xDA, 0xF2, 0x09, 0x87);

        public static readonly Guid CLSID_D2D1Atlas = new Guid(0x913E2BE4, 0xFDCF, 0x4FE2, 0xA5, 0xF0, 0x24, 0x54, 0xF1, 0x4F, 0xF4, 0x08);

        public static readonly Guid CLSID_D2D1BitmapSource = new Guid(0x5FB6C24D, 0xC6DD, 0x4231, 0x94, 0x4, 0x50, 0xF4, 0xD5, 0xC3, 0x25, 0x2D);

        public static readonly Guid CLSID_D2D1Blend = new Guid(0x81C5B77B, 0x13F8, 0x4CDD, 0xAD, 0x20, 0xC8, 0x90, 0x54, 0x7A, 0xC6, 0x5D);

        public static readonly Guid CLSID_D2D1Border = new Guid(0x2A2D49C0, 0x4ACF, 0x43C7, 0x8C, 0x6A, 0x7C, 0x4A, 0x27, 0x87, 0x4D, 0x27);

        public static readonly Guid CLSID_D2D1Brightness = new Guid(0x8CEA8D1E, 0x77B0, 0x4986, 0xB3, 0xB9, 0x2F, 0x0C, 0x0E, 0xAE, 0x78, 0x87);

        public static readonly Guid CLSID_D2D1ColorManagement = new Guid(0x1A28524C, 0xFDD6, 0x4AA4, 0xAE, 0x8F, 0x83, 0x7E, 0xB8, 0x26, 0x7B, 0x37);

        public static readonly Guid CLSID_D2D1ColorMatrix = new Guid(0x921F03D6, 0x641C, 0x47DF, 0x85, 0x2D, 0xB4, 0xBB, 0x61, 0x53, 0xAE, 0x11);

        public static readonly Guid CLSID_D2D1Composite = new Guid(0x48FC9F51, 0xF6AC, 0x48F1, 0x8B, 0x58, 0x3B, 0x28, 0xAC, 0x46, 0xF7, 0x6D);

        public static readonly Guid CLSID_D2D1ConvolveMatrix = new Guid(0x407F8C08, 0x5533, 0x4331, 0xA3, 0x41, 0x23, 0xCC, 0x38, 0x77, 0x84, 0x3E);

        public static readonly Guid CLSID_D2D1Crop = new Guid(0xE23F7110, 0x0E9A, 0x4324, 0xAF, 0x47, 0x6A, 0x2C, 0x0C, 0x46, 0xF3, 0x5B);

        public static readonly Guid CLSID_D2D1DirectionalBlur = new Guid(0x174319A6, 0x58E9, 0x49B2, 0xBB, 0x63, 0xCA, 0xF2, 0xC8, 0x11, 0xA3, 0xDB);

        public static readonly Guid CLSID_D2D1DiscreteTransfer = new Guid(0x90866FCD, 0x488E, 0x454B, 0xAF, 0x06, 0xE5, 0x04, 0x1B, 0x66, 0xC3, 0x6C);

        public static readonly Guid CLSID_D2D1DisplacementMap = new Guid(0xEDC48364, 0x417, 0x4111, 0x94, 0x50, 0x43, 0x84, 0x5F, 0xA9, 0xF8, 0x90);

        public static readonly Guid CLSID_D2D1DistantDiffuse = new Guid(0x3E7EFD62, 0xA32D, 0x46D4, 0xA8, 0x3C, 0x52, 0x78, 0x88, 0x9A, 0xC9, 0x54);

        public static readonly Guid CLSID_D2D1DistantSpecular = new Guid(0x428C1EE5, 0x77B8, 0x4450, 0x8A, 0xB5, 0x72, 0x21, 0x9C, 0x21, 0xAB, 0xDA);

        public static readonly Guid CLSID_D2D1DpiCompensation = new Guid(0x6C26C5C7, 0x34E0, 0x46FC, 0x9C, 0xFD, 0xE5, 0x82, 0x37, 0x6, 0xE2, 0x28);

        public static readonly Guid CLSID_D2D1Flood = new Guid(0x61C23C20, 0xAE69, 0x4D8E, 0x94, 0xCF, 0x50, 0x07, 0x8D, 0xF6, 0x38, 0xF2);

        public static readonly Guid CLSID_D2D1GammaTransfer = new Guid(0x409444C4, 0xC419, 0x41A0, 0xB0, 0xC1, 0x8C, 0xD0, 0xC0, 0xA1, 0x8E, 0x42);

        public static readonly Guid CLSID_D2D1GaussianBlur = new Guid(0x1FEB6D69, 0x2FE6, 0x4AC9, 0x8C, 0x58, 0x1D, 0x7F, 0x93, 0xE7, 0xA6, 0xA5);

        public static readonly Guid CLSID_D2D1Scale = new Guid(0x9DAF9369, 0x3846, 0x4D0E, 0xA4, 0x4E, 0xC, 0x60, 0x79, 0x34, 0xA5, 0xD7);

        public static readonly Guid CLSID_D2D1Histogram = new Guid(0x881DB7D0, 0xF7EE, 0x4D4D, 0xA6, 0xD2, 0x46, 0x97, 0xAC, 0xC6, 0x6E, 0xE8);

        public static readonly Guid CLSID_D2D1HueRotation = new Guid(0x0F4458EC, 0x4B32, 0x491B, 0x9E, 0x85, 0xBD, 0x73, 0xF4, 0x4D, 0x3E, 0xB6);

        public static readonly Guid CLSID_D2D1LinearTransfer = new Guid(0xAD47C8FD, 0x63EF, 0x4ACC, 0x9B, 0x51, 0x67, 0x97, 0x9C, 0x03, 0x6C, 0x06);

        public static readonly Guid CLSID_D2D1LuminanceToAlpha = new Guid(0x41251AB7, 0x0BEB, 0x46F8, 0x9D, 0xA7, 0x59, 0xE9, 0x3F, 0xCC, 0xE5, 0xDE);

        public static readonly Guid CLSID_D2D1Morphology = new Guid(0xEAE6C40D, 0x626A, 0x4C2D, 0xBF, 0xCB, 0x39, 0x10, 0x01, 0xAB, 0xE2, 0x02);

        public static readonly Guid CLSID_D2D1OpacityMetadata = new Guid(0x6C53006A, 0x4450, 0x4199, 0xAA, 0x5B, 0xAD, 0x16, 0x56, 0xFE, 0xCE, 0x5E);

        public static readonly Guid CLSID_D2D1PointDiffuse = new Guid(0xB9E303C3, 0xC08C, 0x4F91, 0x8B, 0x7B, 0x38, 0x65, 0x6B, 0xC4, 0x8C, 0x20);

        public static readonly Guid CLSID_D2D1PointSpecular = new Guid(0x09C3CA26, 0x3AE2, 0x4F09, 0x9E, 0xBC, 0xED, 0x38, 0x65, 0xD5, 0x3F, 0x22);

        public static readonly Guid CLSID_D2D1Premultiply = new Guid(0x06EAB419, 0xDEED, 0x4018, 0x80, 0xD2, 0x3E, 0x1D, 0x47, 0x1A, 0xDE, 0xB2);

        public static readonly Guid CLSID_D2D1Saturation = new Guid(0x5CB2D9CF, 0x327D, 0x459F, 0xA0, 0xCE, 0x40, 0xC0, 0xB2, 0x08, 0x6B, 0xF7);

        public static readonly Guid CLSID_D2D1Shadow = new Guid(0xC67EA361, 0x1863, 0x4E69, 0x89, 0xDB, 0x69, 0x5D, 0x3E, 0x9A, 0x5B, 0x6B);

        public static readonly Guid CLSID_D2D1SpotDiffuse = new Guid(0x818A1105, 0x7932, 0x44F4, 0xAA, 0x86, 0x08, 0xAE, 0x7B, 0x2F, 0x2C, 0x93);

        public static readonly Guid CLSID_D2D1SpotSpecular = new Guid(0xEDAE421E, 0x7654, 0x4A37, 0x9D, 0xB8, 0x71, 0xAC, 0xC1, 0xBE, 0xB3, 0xC1);

        public static readonly Guid CLSID_D2D1TableTransfer = new Guid(0x5BF818C3, 0x5E43, 0x48CB, 0xB6, 0x31, 0x86, 0x83, 0x96, 0xD6, 0xA1, 0xD4);

        public static readonly Guid CLSID_D2D1Tile = new Guid(0xB0784138, 0x3B76, 0x4BC5, 0xB1, 0x3B, 0x0F, 0xA2, 0xAD, 0x02, 0x65, 0x9F);

        public static readonly Guid CLSID_D2D1Turbulence = new Guid(0xCF2BB6AE, 0x889A, 0x4AD7, 0xBA, 0x29, 0xA2, 0xFD, 0x73, 0x2C, 0x9F, 0xC9);

        public static readonly Guid CLSID_D2D1UnPremultiply = new Guid(0xFB9AC489, 0xAD8D, 0x41ED, 0x99, 0x99, 0xBB, 0x63, 0x47, 0xD1, 0x10, 0xF7);

        enum D2D1_GAUSSIANBLUR_OPTIMIZATION
        {
            D2D1_GAUSSIANBLUR_OPTIMIZATION_SPEED,
            D2D1_GAUSSIANBLUR_OPTIMIZATION_BALANCED,
            D2D1_GAUSSIANBLUR_OPTIMIZATION_QUALITY,
            D2D1_GAUSSIANBLUR_OPTIMIZATION_FORCE_DWORD
        };

        enum D2D1_BORDER_MODE
        {
            D2D1_BORDER_MODE_SOFT,
            D2D1_BORDER_MODE_HARD,
            D2D1_BORDER_MODE_FORCE_DWORD
        };

        public GaussianBlurEffect()
        {

        }

        public string Name { get; set; }

        public Guid EffectId => CLSID_D2D1GaussianBlur;

        public uint PropertyCount => 3;

        public uint SourceCount => 1;

        public uint GetNamedPropertyMapping(string name, out GRAPHICS_EFFECT_PROPERTY_MAPPING mapping)
        {
            throw new NotImplementedException();
        }

        public object GetProperty(uint index)
        {
            switch ((D2D1_GAUSSIANBLUR_PROP)index)
            {
                case D2D1_GAUSSIANBLUR_PROP.D2D1_GAUSSIANBLUR_PROP_STANDARD_DEVIATION:
                    return 30.0f;

                case D2D1_GAUSSIANBLUR_PROP.D2D1_GAUSSIANBLUR_PROP_OPTIMIZATION:
                    return (UInt32)D2D1_GAUSSIANBLUR_OPTIMIZATION.D2D1_GAUSSIANBLUR_OPTIMIZATION_SPEED;

                case D2D1_GAUSSIANBLUR_PROP.D2D1_GAUSSIANBLUR_PROP_BORDER_MODE:
                    return (UInt32)D2D1_BORDER_MODE.D2D1_BORDER_MODE_HARD;
            }

            return null;
        }

        private IGraphicsEffectSource _source = new CompositionEffectSourceParameter("backdrop");

        public IGraphicsEffectSource GetSource(uint index)
        {
            if (index == 0)
            {
                return _source;
            }
            else
            {
                return null;
            }
        }
    }

    class CompositionHost
    {
        //typedef enum DISPATCHERQUEUE_THREAD_APARTMENTTYPE
        //{
        //    DQTAT_COM_NONE,
        //    DQTAT_COM_ASTA,
        //    DQTAT_COM_STA
        //};
        internal enum DISPATCHERQUEUE_THREAD_APARTMENTTYPE
        {
            DQTAT_COM_NONE = 0,
            DQTAT_COM_ASTA = 1,
            DQTAT_COM_STA = 2
        };

        //typedef enum DISPATCHERQUEUE_THREAD_TYPE
        //{
        //    DQTYPE_THREAD_DEDICATED,
        //    DQTYPE_THREAD_CURRENT
        //};
        internal enum DISPATCHERQUEUE_THREAD_TYPE
        {
            DQTYPE_THREAD_DEDICATED = 1,
            DQTYPE_THREAD_CURRENT = 2,
        };

        //struct DispatcherQueueOptions
        //{
        //    DWORD dwSize;
        //    DISPATCHERQUEUE_THREAD_TYPE threadType;
        //    DISPATCHERQUEUE_THREAD_APARTMENTTYPE apartmentType;
        //};
        [StructLayout(LayoutKind.Sequential)]
        internal struct DispatcherQueueOptions
        {
            public int dwSize;

            [MarshalAs(UnmanagedType.I4)]
            public DISPATCHERQUEUE_THREAD_TYPE threadType;

            [MarshalAs(UnmanagedType.I4)]
            public DISPATCHERQUEUE_THREAD_APARTMENTTYPE apartmentType;
        };

        //HRESULT CreateDispatcherQueueController(
        //  DispatcherQueueOptions options,
        //  ABI::Windows::System::IDispatcherQueueController** dispatcherQueueController
        //);
        [DllImport("coremessaging.dll", EntryPoint = "CreateDispatcherQueueController", CharSet = CharSet.Unicode)]
        internal static extern IntPtr CreateDispatcherQueueController(DispatcherQueueOptions options, out IntPtr dispatcherQueueController);







        public static CompositionHost Instance { get; } = new CompositionHost();

        private Windows.UI.Composition.Compositor _compositor;
        private Windows.System.DispatcherQueueController _dispatcherQueueController;
        private Windows.UI.Composition.Desktop.DesktopWindowTarget _target;

        private CompositionHost()
        {

        }

        public void AddElement(float size, float x, float y)
        {
            if (_target.Root != null)
            {
                var visuals = _target.Root.As<ContainerVisual>().Children;

                var visual = _compositor.CreateSpriteVisual();

                var element = _compositor.CreateSpriteVisual();
                var rand = new Random();

                element.Brush = _compositor.CreateColorBrush(new Windows.UI.Color { A = 255, R = (byte)(rand.NextDouble() * 255), G = (byte)(rand.NextDouble() * 255), B = (byte)(rand.NextDouble() * 255) });
                element.Size = new System.Numerics.Vector2(size, size);
                element.Offset = new System.Numerics.Vector3(x, y, 0.0f);

                var animation = _compositor.CreateVector3KeyFrameAnimation();
                var bottom = (float)600 - element.Size.Y;
                animation.InsertKeyFrame(1, new System.Numerics.Vector3(element.Offset.X, bottom, 0));

                animation.Duration = TimeSpan.FromSeconds(2);
                animation.DelayTime = TimeSpan.FromSeconds(3);
                element.StartAnimation("Offset", animation);
                visuals.InsertAtTop(element);

                visuals.InsertAtTop(visual);
            }
        }

        public void Initialize(IntPtr hwnd)
        {
            EnsureDispatcherQueue();
            if (_dispatcherQueueController != null)
                _compositor = new Windows.UI.Composition.Compositor();

            CreateDesktopWindowTarget(hwnd);
            CreateCompositionRoot();
        }

        public void CreateBlur()
        {
            var effect = new GaussianBlurEffect();
            var effectFactory = _compositor.CreateEffectFactory(effect);
            var blurBrush = effectFactory.CreateBrush();

            var backDropBrush = _compositor.CreateBackdropBrush();

            blurBrush.SetSourceParameter("backdrop", backDropBrush);

            var visual = _compositor.CreateSpriteVisual();

            visual.RelativeSizeAdjustment = new System.Numerics.Vector2(1.0f, 1.0f);
            visual.Brush = blurBrush;

            _target.Root = visual;
        }

        void CreateCompositionRoot()
        {
            var root = _compositor.CreateContainerVisual();
            root.RelativeSizeAdjustment = new System.Numerics.Vector2(1.0f, 1.0f);
            //root.Offset = new System.Numerics.Vector3(0, 0, 0);
            _target.Root = root;
        }

        void CreateDesktopWindowTarget(IntPtr window)
        {
            var interop = _compositor.As<global::Windows.UI.Composition.Desktop.ICompositorDesktopInterop>();

            interop.CreateDesktopWindowTarget(window, false, out var windowTarget);
            _target = Windows.UI.Composition.Desktop.DesktopWindowTarget.FromAbi(windowTarget);
        }

        void EnsureDispatcherQueue()
        {

            if (_dispatcherQueueController == null)
            {
                DispatcherQueueOptions options = new DispatcherQueueOptions();
                options.apartmentType = DISPATCHERQUEUE_THREAD_APARTMENTTYPE.DQTAT_COM_STA;
                options.threadType = DISPATCHERQUEUE_THREAD_TYPE.DQTYPE_THREAD_CURRENT;
                options.dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions));

                CreateDispatcherQueueController(options, out var queue);
                _dispatcherQueueController = Windows.System.DispatcherQueueController.FromAbi(queue);
            }
        }
    }
}


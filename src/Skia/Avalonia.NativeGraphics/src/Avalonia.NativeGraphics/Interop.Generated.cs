#pragma warning disable 108
// ReSharper disable RedundantUsingDirective
// ReSharper disable JoinDeclarationAndInitializer
// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable UnusedType.Local
// ReSharper disable InconsistentNaming
// ReSharper disable RedundantNameQualifier
// ReSharper disable RedundantCast
// ReSharper disable IdentifierTypo
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable RedundantUnsafeContext
// ReSharper disable RedundantBaseQualifier
// ReSharper disable EmptyStatement
// ReSharper disable RedundantAttributeParentheses
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using MicroCom.Runtime;

namespace Avalonia.Native.Interop
{
    public enum AvgSweepDirection
    {
        CounterClockwise = 0,
        ClockWise = 1
    }

    public enum AvgFillRule
    {
        EvenOdd = 0,
        NonZero = 1
    }

    public enum AvgFontStyle
    {
        Normal = 0,
        Italic = 1,
        Oblique = 2
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public unsafe partial struct AvgPoint
    {
        public double X;
        public double Y;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public unsafe partial struct AvgVector
    {
        public double X;
        public double Y;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public unsafe partial struct AvgRect
    {
        public double X;
        public double Y;
        public double Width;
        public double Height;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public unsafe partial struct AvgSize
    {
        public double Width;
        public double Height;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public unsafe partial struct AvgRoundRect
    {
        public AvgRect Rect;
        public int IsRounded;
        public AvgVector RadiiTopLeft;
        public AvgVector RadiiTopRight;
        public AvgVector RadiiBottomLeft;
        public AvgVector RadiiBottomRight;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public unsafe partial struct AvgPixelSize
    {
        public int Width;
        public int Height;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public unsafe partial struct AvgMatrix3x2
    {
        public double M11;
        public double M12;
        public double M21;
        public double M22;
        public double M31;
        public double M32;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public unsafe partial struct AvgColor
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public unsafe partial struct AvgBrush
    {
        public int Valid;
        public double Opacity;
        public AvgColor Color;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public unsafe partial struct AvgPen
    {
        public int Valid;
        public AvgBrush Brush;
        public double MiterLimit;
        public double Thickness;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public unsafe partial struct AvgBoxShadow
    {
        public double OffsetX;
        public double OffsetY;
        public double Blur;
        public double Spread;
        public int color;
        public int IsInset;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public unsafe partial struct AvgTypeface
    {
        public AvgFontStyle FontStyle;
        public int FontWeight;
        public int FontStretch;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public unsafe partial struct AvgGlyphInfo
    {
        public uint codepoint;
        public uint mask;
        public uint cluster;
        public uint var1;
        public uint var2;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public unsafe partial struct AvgGlphPosition
    {
        public int x_advance;
        public int y_advance;
        public int x_offset;
        public int y_offset;
        public uint var;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public unsafe partial struct AvgSkPosition
    {
        public float x;
        public float y;
    }

    public unsafe partial interface IAvgGetProcAddressDelegate : global::MicroCom.Runtime.IUnknown
    {
        IntPtr GetProcAddress(string proc);
    }

    public unsafe partial interface IAvgFactory : global::MicroCom.Runtime.IUnknown
    {
        int Version { get; }

        IAvgGpu CreateGlGpu(int gles, IAvgGetProcAddressDelegate glGetProcAddress);
        IAvgRenderTarget CreateGlGpuRenderTarget(IAvgGpu gpu, IAvgGlPlatformSurfaceRenderTarget gl);
        IAvgPath CreateAvgPath();
        IAvgFontManager CreateAvgFontManager();
        IAvgGlyphRun CreateAvgGlyphRun(IAvgFontManager fontManager, IAvgGlyphTypeface typeface);
        IAvgFontShapeBuffer CreateAvgFontShapeBuffer(IAvgGlyphTypeface typeface);
    }

    public unsafe partial interface IAvgDrawingContext : global::MicroCom.Runtime.IUnknown
    {
        double Scaling { get; }

        void SetTransform(AvgMatrix3x2* matrix);
        void Clear(uint color);
        void DrawGeometry(IAvgPath path, AvgBrush brush, AvgPen pen);
        void DrawRectangle(AvgRoundRect rect, AvgBrush brush, AvgPen pen, AvgBoxShadow* boxshadows, int n_boxshadows);
        void DrawLine(AvgPoint p1, AvgPoint p2, AvgPen pen);
        void DrawGlyphRun(IAvgGlyphRun glyphrun, double x, double y, AvgBrush brush);
        void PushOpacity(double opacity);
        void PopOpacity();
        void PushClip(AvgRoundRect clip);
        void PopClip();
    }

    public unsafe partial interface IAvgPath : global::MicroCom.Runtime.IUnknown
    {
        void ArcTo(AvgPoint point, AvgSize size, double rotationAngle, int isLargeArc, AvgSweepDirection sweepDirection);
        void BeginFigure(AvgPoint startPoint, int isFilled);
        void CubicBezierTo(AvgPoint p1, AvgPoint p2, AvgPoint p3);
        void QuadraticBezierTo(AvgPoint p1, AvgPoint p2);
        void LineTo(AvgPoint point);
        void EndFigure(int isClosed);
        void SetFillRule(AvgFillRule fillRule);
        void AddRect(AvgRect rect);
        void MoveTo(AvgPoint point);
        void AddOval(AvgRect rect);
    }

    public unsafe partial interface IAvgString : global::MicroCom.Runtime.IUnknown
    {
        void* Pointer();
        int Length();
    }

    public unsafe partial interface IAvgFontManager : global::MicroCom.Runtime.IUnknown
    {
        IAvgString DefaultFamilyName { get; }

        int FontFamilyCount { get; }

        IAvgString GetFamilyName(int index);
        IAvgGlyphTypeface CreateGlyphTypeface(string fontFamily, AvgTypeface typeface);
    }

    public unsafe partial interface IAvgRenderInterface : global::MicroCom.Runtime.IUnknown
    {
    }

    public unsafe partial interface IAvgGlyphTypeface : global::MicroCom.Runtime.IUnknown
    {
        uint GetGlyph(uint codepoint);
        int GetGlyphAdvance(uint glyph);
        int DesignEmHeight { get; }

        int Ascent { get; }

        int Descent { get; }

        int LineGap { get; }

        int UnderlinePosition { get; }

        int UnderlineThickness { get; }

        int StrikethroughPosition { get; }

        int StrikethroughThickness { get; }

        int IsFixedPitch { get; }

        int IsFakeBold { get; }

        int IsFakeItalic { get; }
    }

    public unsafe partial interface IAvgFontShapeBuffer : global::MicroCom.Runtime.IUnknown
    {
        int Length { get; }

        void GuessSegmentProperties();
        void SetDirection(int direction);
        void SetLanguage(void* language);
        void AddUtf16(void* utf16, int length, int itemOffset, int itemLength);
        void Shape();
        void GetScale(int* x, int* y);
        void* GetGlyphInfoSpan(uint* length);
        void* GetGlyphPositionSpan(uint* length);
    }

    public unsafe partial interface IAvgGlyphRun : global::MicroCom.Runtime.IUnknown
    {
        void AllocRun(int count);
        void AllocHorizontalRun(int count);
        void AllocPositionedRun(int count);
        void SetFontSize(float size);
        void* GlyphBuffer { get; }

        void* PositionsBuffer { get; }

        void BuildText();
    }

    public unsafe partial interface IAvgRenderTarget : global::MicroCom.Runtime.IUnknown
    {
        IAvgDrawingContext CreateDrawingContext();
    }

    public unsafe partial interface IAvgGpuControl : global::MicroCom.Runtime.IUnknown
    {
        IUnknown Lock();
    }

    public unsafe partial interface IAvgGpu : global::MicroCom.Runtime.IUnknown
    {
    }

    public unsafe partial interface IAvgGlPlatformSurfaceRenderTarget : global::MicroCom.Runtime.IUnknown
    {
        IAvgGlPlatformSurfaceRenderSession BeginDraw();
    }

    public unsafe partial interface IAvgGlPlatformSurfaceRenderSession : global::MicroCom.Runtime.IUnknown
    {
        void GetPixelSize(AvgPixelSize* rv);
        double Scaling { get; }

        int SampleCount { get; }

        int StencilSize { get; }

        int FboId { get; }

        int IsYFlipped { get; }
    }
}

namespace Avalonia.Native.Interop.Impl
{
    public unsafe partial class __MicroComIAvgGetProcAddressDelegateProxy : global::MicroCom.Runtime.MicroComProxyBase, IAvgGetProcAddressDelegate
    {
        public IntPtr GetProcAddress(string proc)
        {
            IntPtr __result;
            var __bytemarshal_proc = new byte[System.Text.Encoding.UTF8.GetByteCount(proc) + 1];
            System.Text.Encoding.UTF8.GetBytes(proc, 0, proc.Length, __bytemarshal_proc, 0);
            fixed (byte* __fixedmarshal_proc = __bytemarshal_proc)
                __result = (IntPtr)((delegate* unmanaged[Stdcall]<void*, void*, IntPtr>)(*PPV)[base.VTableSize + 0])(PPV, __fixedmarshal_proc);
            return __result;
        }

        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit()
        {
            global::MicroCom.Runtime.MicroComRuntime.Register(typeof(IAvgGetProcAddressDelegate), new Guid("084B6D03-4545-43D1-971D-3D3A968A3127"), (p, owns) => new __MicroComIAvgGetProcAddressDelegateProxy(p, owns));
        }

        protected __MicroComIAvgGetProcAddressDelegateProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 1;
    }

    unsafe class __MicroComIAvgGetProcAddressDelegateVTable : global::MicroCom.Runtime.MicroComVtblBase
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate IntPtr GetProcAddressDelegate(void* @this, byte* proc);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static IntPtr GetProcAddress(void* @this, byte* proc)
        {
            IAvgGetProcAddressDelegate __target = null;
            try
            {
                {
                    __target = (IAvgGetProcAddressDelegate)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.GetProcAddress((proc == null ? null : System.Runtime.InteropServices.Marshal.PtrToStringAnsi(new IntPtr(proc))));
                        return __result;
                    }
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return default;
            }
        }

        protected __MicroComIAvgGetProcAddressDelegateVTable()
        {
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, byte*, IntPtr>)&GetProcAddress); 
#else
            base.AddMethod((GetProcAddressDelegate)GetProcAddress); 
#endif
        }

        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit() => global::MicroCom.Runtime.MicroComRuntime.RegisterVTable(typeof(IAvgGetProcAddressDelegate), new __MicroComIAvgGetProcAddressDelegateVTable().CreateVTable());
    }

    public unsafe partial class __MicroComIAvgFactoryProxy : global::MicroCom.Runtime.MicroComProxyBase, IAvgFactory
    {
        public int Version
        {
            get
            {
                int __result;
                __result = (int)((delegate* unmanaged[Stdcall]<void*, int>)(*PPV)[base.VTableSize + 0])(PPV);
                return __result;
            }
        }

        public IAvgGpu CreateGlGpu(int gles, IAvgGetProcAddressDelegate glGetProcAddress)
        {
            int __result;
            void* __marshal_ppv = null;
            __result = (int)((delegate* unmanaged[Stdcall]<void*, int, void*, void*, int>)(*PPV)[base.VTableSize + 1])(PPV, gles, global::MicroCom.Runtime.MicroComRuntime.GetNativePointer(glGetProcAddress), &__marshal_ppv);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateGlGpu failed", __result);
            return global::MicroCom.Runtime.MicroComRuntime.CreateProxyOrNullFor<IAvgGpu>(__marshal_ppv, true);
        }

        public IAvgRenderTarget CreateGlGpuRenderTarget(IAvgGpu gpu, IAvgGlPlatformSurfaceRenderTarget gl)
        {
            int __result;
            void* __marshal_ppv = null;
            __result = (int)((delegate* unmanaged[Stdcall]<void*, void*, void*, void*, int>)(*PPV)[base.VTableSize + 2])(PPV, global::MicroCom.Runtime.MicroComRuntime.GetNativePointer(gpu), global::MicroCom.Runtime.MicroComRuntime.GetNativePointer(gl), &__marshal_ppv);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateGlGpuRenderTarget failed", __result);
            return global::MicroCom.Runtime.MicroComRuntime.CreateProxyOrNullFor<IAvgRenderTarget>(__marshal_ppv, true);
        }

        public IAvgPath CreateAvgPath()
        {
            int __result;
            void* __marshal_ppv = null;
            __result = (int)((delegate* unmanaged[Stdcall]<void*, void*, int>)(*PPV)[base.VTableSize + 3])(PPV, &__marshal_ppv);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateAvgPath failed", __result);
            return global::MicroCom.Runtime.MicroComRuntime.CreateProxyOrNullFor<IAvgPath>(__marshal_ppv, true);
        }

        public IAvgFontManager CreateAvgFontManager()
        {
            int __result;
            void* __marshal_ppv = null;
            __result = (int)((delegate* unmanaged[Stdcall]<void*, void*, int>)(*PPV)[base.VTableSize + 4])(PPV, &__marshal_ppv);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateAvgFontManager failed", __result);
            return global::MicroCom.Runtime.MicroComRuntime.CreateProxyOrNullFor<IAvgFontManager>(__marshal_ppv, true);
        }

        public IAvgGlyphRun CreateAvgGlyphRun(IAvgFontManager fontManager, IAvgGlyphTypeface typeface)
        {
            int __result;
            void* __marshal_ppv = null;
            __result = (int)((delegate* unmanaged[Stdcall]<void*, void*, void*, void*, int>)(*PPV)[base.VTableSize + 5])(PPV, global::MicroCom.Runtime.MicroComRuntime.GetNativePointer(fontManager), global::MicroCom.Runtime.MicroComRuntime.GetNativePointer(typeface), &__marshal_ppv);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateAvgGlyphRun failed", __result);
            return global::MicroCom.Runtime.MicroComRuntime.CreateProxyOrNullFor<IAvgGlyphRun>(__marshal_ppv, true);
        }

        public IAvgFontShapeBuffer CreateAvgFontShapeBuffer(IAvgGlyphTypeface typeface)
        {
            int __result;
            void* __marshal_ppv = null;
            __result = (int)((delegate* unmanaged[Stdcall]<void*, void*, void*, int>)(*PPV)[base.VTableSize + 6])(PPV, global::MicroCom.Runtime.MicroComRuntime.GetNativePointer(typeface), &__marshal_ppv);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateAvgFontShapeBuffer failed", __result);
            return global::MicroCom.Runtime.MicroComRuntime.CreateProxyOrNullFor<IAvgFontShapeBuffer>(__marshal_ppv, true);
        }

        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit()
        {
            global::MicroCom.Runtime.MicroComRuntime.Register(typeof(IAvgFactory), new Guid("52434e9c-5438-4ac9-9823-9f5a3fe90d53"), (p, owns) => new __MicroComIAvgFactoryProxy(p, owns));
        }

        protected __MicroComIAvgFactoryProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 7;
    }

    unsafe class __MicroComIAvgFactoryVTable : global::MicroCom.Runtime.MicroComVtblBase
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetVersionDelegate(void* @this);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int GetVersion(void* @this)
        {
            IAvgFactory __target = null;
            try
            {
                {
                    __target = (IAvgFactory)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.Version;
                        return __result;
                    }
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return default;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateGlGpuDelegate(void* @this, int gles, void* glGetProcAddress, void** ppv);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int CreateGlGpu(void* @this, int gles, void* glGetProcAddress, void** ppv)
        {
            IAvgFactory __target = null;
            try
            {
                {
                    __target = (IAvgFactory)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.CreateGlGpu(gles, global::MicroCom.Runtime.MicroComRuntime.CreateProxyOrNullFor<IAvgGetProcAddressDelegate>(glGetProcAddress, false));
                        *ppv = global::MicroCom.Runtime.MicroComRuntime.GetNativePointer(__result, true);
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateGlGpuRenderTargetDelegate(void* @this, void* gpu, void* gl, void** ppv);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int CreateGlGpuRenderTarget(void* @this, void* gpu, void* gl, void** ppv)
        {
            IAvgFactory __target = null;
            try
            {
                {
                    __target = (IAvgFactory)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.CreateGlGpuRenderTarget(global::MicroCom.Runtime.MicroComRuntime.CreateProxyOrNullFor<IAvgGpu>(gpu, false), global::MicroCom.Runtime.MicroComRuntime.CreateProxyOrNullFor<IAvgGlPlatformSurfaceRenderTarget>(gl, false));
                        *ppv = global::MicroCom.Runtime.MicroComRuntime.GetNativePointer(__result, true);
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateAvgPathDelegate(void* @this, void** ppv);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int CreateAvgPath(void* @this, void** ppv)
        {
            IAvgFactory __target = null;
            try
            {
                {
                    __target = (IAvgFactory)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.CreateAvgPath();
                        *ppv = global::MicroCom.Runtime.MicroComRuntime.GetNativePointer(__result, true);
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateAvgFontManagerDelegate(void* @this, void** ppv);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int CreateAvgFontManager(void* @this, void** ppv)
        {
            IAvgFactory __target = null;
            try
            {
                {
                    __target = (IAvgFactory)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.CreateAvgFontManager();
                        *ppv = global::MicroCom.Runtime.MicroComRuntime.GetNativePointer(__result, true);
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateAvgGlyphRunDelegate(void* @this, void* fontManager, void* typeface, void** ppv);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int CreateAvgGlyphRun(void* @this, void* fontManager, void* typeface, void** ppv)
        {
            IAvgFactory __target = null;
            try
            {
                {
                    __target = (IAvgFactory)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.CreateAvgGlyphRun(global::MicroCom.Runtime.MicroComRuntime.CreateProxyOrNullFor<IAvgFontManager>(fontManager, false), global::MicroCom.Runtime.MicroComRuntime.CreateProxyOrNullFor<IAvgGlyphTypeface>(typeface, false));
                        *ppv = global::MicroCom.Runtime.MicroComRuntime.GetNativePointer(__result, true);
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateAvgFontShapeBufferDelegate(void* @this, void* typeface, void** ppv);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int CreateAvgFontShapeBuffer(void* @this, void* typeface, void** ppv)
        {
            IAvgFactory __target = null;
            try
            {
                {
                    __target = (IAvgFactory)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.CreateAvgFontShapeBuffer(global::MicroCom.Runtime.MicroComRuntime.CreateProxyOrNullFor<IAvgGlyphTypeface>(typeface, false));
                        *ppv = global::MicroCom.Runtime.MicroComRuntime.GetNativePointer(__result, true);
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        protected __MicroComIAvgFactoryVTable()
        {
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, int>)&GetVersion); 
#else
            base.AddMethod((GetVersionDelegate)GetVersion); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, int, void*, void**, int>)&CreateGlGpu); 
#else
            base.AddMethod((CreateGlGpuDelegate)CreateGlGpu); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, void*, void*, void**, int>)&CreateGlGpuRenderTarget); 
#else
            base.AddMethod((CreateGlGpuRenderTargetDelegate)CreateGlGpuRenderTarget); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, void**, int>)&CreateAvgPath); 
#else
            base.AddMethod((CreateAvgPathDelegate)CreateAvgPath); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, void**, int>)&CreateAvgFontManager); 
#else
            base.AddMethod((CreateAvgFontManagerDelegate)CreateAvgFontManager); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, void*, void*, void**, int>)&CreateAvgGlyphRun); 
#else
            base.AddMethod((CreateAvgGlyphRunDelegate)CreateAvgGlyphRun); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, void*, void**, int>)&CreateAvgFontShapeBuffer); 
#else
            base.AddMethod((CreateAvgFontShapeBufferDelegate)CreateAvgFontShapeBuffer); 
#endif
        }

        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit() => global::MicroCom.Runtime.MicroComRuntime.RegisterVTable(typeof(IAvgFactory), new __MicroComIAvgFactoryVTable().CreateVTable());
    }

    public unsafe partial class __MicroComIAvgDrawingContextProxy : global::MicroCom.Runtime.MicroComProxyBase, IAvgDrawingContext
    {
        public double Scaling
        {
            get
            {
                double __result;
                __result = (double)((delegate* unmanaged[Stdcall]<void*, double>)(*PPV)[base.VTableSize + 0])(PPV);
                return __result;
            }
        }

        public void SetTransform(AvgMatrix3x2* matrix)
        {
            ((delegate* unmanaged[Stdcall]<void*, void*, void>)(*PPV)[base.VTableSize + 1])(PPV, matrix);
        }

        public void Clear(uint color)
        {
            ((delegate* unmanaged[Stdcall]<void*, uint, void>)(*PPV)[base.VTableSize + 2])(PPV, color);
        }

        public void DrawGeometry(IAvgPath path, AvgBrush brush, AvgPen pen)
        {
            ((delegate* unmanaged[Stdcall]<void*, void*, AvgBrush, AvgPen, void>)(*PPV)[base.VTableSize + 3])(PPV, global::MicroCom.Runtime.MicroComRuntime.GetNativePointer(path), brush, pen);
        }

        public void DrawRectangle(AvgRoundRect rect, AvgBrush brush, AvgPen pen, AvgBoxShadow* boxshadows, int n_boxshadows)
        {
            ((delegate* unmanaged[Stdcall]<void*, AvgRoundRect, AvgBrush, AvgPen, void*, int, void>)(*PPV)[base.VTableSize + 4])(PPV, rect, brush, pen, boxshadows, n_boxshadows);
        }

        public void DrawLine(AvgPoint p1, AvgPoint p2, AvgPen pen)
        {
            ((delegate* unmanaged[Stdcall]<void*, AvgPoint, AvgPoint, AvgPen, void>)(*PPV)[base.VTableSize + 5])(PPV, p1, p2, pen);
        }

        public void DrawGlyphRun(IAvgGlyphRun glyphrun, double x, double y, AvgBrush brush)
        {
            ((delegate* unmanaged[Stdcall]<void*, void*, double, double, AvgBrush, void>)(*PPV)[base.VTableSize + 6])(PPV, global::MicroCom.Runtime.MicroComRuntime.GetNativePointer(glyphrun), x, y, brush);
        }

        public void PushOpacity(double opacity)
        {
            ((delegate* unmanaged[Stdcall]<void*, double, void>)(*PPV)[base.VTableSize + 7])(PPV, opacity);
        }

        public void PopOpacity()
        {
            ((delegate* unmanaged[Stdcall]<void*, void>)(*PPV)[base.VTableSize + 8])(PPV);
        }

        public void PushClip(AvgRoundRect clip)
        {
            ((delegate* unmanaged[Stdcall]<void*, AvgRoundRect, void>)(*PPV)[base.VTableSize + 9])(PPV, clip);
        }

        public void PopClip()
        {
            ((delegate* unmanaged[Stdcall]<void*, void>)(*PPV)[base.VTableSize + 10])(PPV);
        }

        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit()
        {
            global::MicroCom.Runtime.MicroComRuntime.Register(typeof(IAvgDrawingContext), new Guid("309466F0-B5CA-4ABA-8469-2C902FE5D8F3"), (p, owns) => new __MicroComIAvgDrawingContextProxy(p, owns));
        }

        protected __MicroComIAvgDrawingContextProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 11;
    }

    unsafe class __MicroComIAvgDrawingContextVTable : global::MicroCom.Runtime.MicroComVtblBase
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate double GetScalingDelegate(void* @this);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static double GetScaling(void* @this)
        {
            IAvgDrawingContext __target = null;
            try
            {
                {
                    __target = (IAvgDrawingContext)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.Scaling;
                        return __result;
                    }
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return default;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate void SetTransformDelegate(void* @this, AvgMatrix3x2* matrix);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static void SetTransform(void* @this, AvgMatrix3x2* matrix)
        {
            IAvgDrawingContext __target = null;
            try
            {
                {
                    __target = (IAvgDrawingContext)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    __target.SetTransform(matrix);
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                ;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate void ClearDelegate(void* @this, uint color);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static void Clear(void* @this, uint color)
        {
            IAvgDrawingContext __target = null;
            try
            {
                {
                    __target = (IAvgDrawingContext)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    __target.Clear(color);
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                ;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate void DrawGeometryDelegate(void* @this, void* path, AvgBrush brush, AvgPen pen);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static void DrawGeometry(void* @this, void* path, AvgBrush brush, AvgPen pen)
        {
            IAvgDrawingContext __target = null;
            try
            {
                {
                    __target = (IAvgDrawingContext)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    __target.DrawGeometry(global::MicroCom.Runtime.MicroComRuntime.CreateProxyOrNullFor<IAvgPath>(path, false), brush, pen);
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                ;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate void DrawRectangleDelegate(void* @this, AvgRoundRect rect, AvgBrush brush, AvgPen pen, AvgBoxShadow* boxshadows, int n_boxshadows);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static void DrawRectangle(void* @this, AvgRoundRect rect, AvgBrush brush, AvgPen pen, AvgBoxShadow* boxshadows, int n_boxshadows)
        {
            IAvgDrawingContext __target = null;
            try
            {
                {
                    __target = (IAvgDrawingContext)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    __target.DrawRectangle(rect, brush, pen, boxshadows, n_boxshadows);
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                ;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate void DrawLineDelegate(void* @this, AvgPoint p1, AvgPoint p2, AvgPen pen);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static void DrawLine(void* @this, AvgPoint p1, AvgPoint p2, AvgPen pen)
        {
            IAvgDrawingContext __target = null;
            try
            {
                {
                    __target = (IAvgDrawingContext)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    __target.DrawLine(p1, p2, pen);
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                ;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate void DrawGlyphRunDelegate(void* @this, void* glyphrun, double x, double y, AvgBrush brush);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static void DrawGlyphRun(void* @this, void* glyphrun, double x, double y, AvgBrush brush)
        {
            IAvgDrawingContext __target = null;
            try
            {
                {
                    __target = (IAvgDrawingContext)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    __target.DrawGlyphRun(global::MicroCom.Runtime.MicroComRuntime.CreateProxyOrNullFor<IAvgGlyphRun>(glyphrun, false), x, y, brush);
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                ;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate void PushOpacityDelegate(void* @this, double opacity);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static void PushOpacity(void* @this, double opacity)
        {
            IAvgDrawingContext __target = null;
            try
            {
                {
                    __target = (IAvgDrawingContext)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    __target.PushOpacity(opacity);
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                ;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate void PopOpacityDelegate(void* @this);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static void PopOpacity(void* @this)
        {
            IAvgDrawingContext __target = null;
            try
            {
                {
                    __target = (IAvgDrawingContext)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    __target.PopOpacity();
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                ;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate void PushClipDelegate(void* @this, AvgRoundRect clip);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static void PushClip(void* @this, AvgRoundRect clip)
        {
            IAvgDrawingContext __target = null;
            try
            {
                {
                    __target = (IAvgDrawingContext)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    __target.PushClip(clip);
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                ;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate void PopClipDelegate(void* @this);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static void PopClip(void* @this)
        {
            IAvgDrawingContext __target = null;
            try
            {
                {
                    __target = (IAvgDrawingContext)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    __target.PopClip();
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                ;
            }
        }

        protected __MicroComIAvgDrawingContextVTable()
        {
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, double>)&GetScaling); 
#else
            base.AddMethod((GetScalingDelegate)GetScaling); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, AvgMatrix3x2*, void>)&SetTransform); 
#else
            base.AddMethod((SetTransformDelegate)SetTransform); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, uint, void>)&Clear); 
#else
            base.AddMethod((ClearDelegate)Clear); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, void*, AvgBrush, AvgPen, void>)&DrawGeometry); 
#else
            base.AddMethod((DrawGeometryDelegate)DrawGeometry); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, AvgRoundRect, AvgBrush, AvgPen, AvgBoxShadow*, int, void>)&DrawRectangle); 
#else
            base.AddMethod((DrawRectangleDelegate)DrawRectangle); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, AvgPoint, AvgPoint, AvgPen, void>)&DrawLine); 
#else
            base.AddMethod((DrawLineDelegate)DrawLine); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, void*, double, double, AvgBrush, void>)&DrawGlyphRun); 
#else
            base.AddMethod((DrawGlyphRunDelegate)DrawGlyphRun); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, double, void>)&PushOpacity); 
#else
            base.AddMethod((PushOpacityDelegate)PushOpacity); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, void>)&PopOpacity); 
#else
            base.AddMethod((PopOpacityDelegate)PopOpacity); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, AvgRoundRect, void>)&PushClip); 
#else
            base.AddMethod((PushClipDelegate)PushClip); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, void>)&PopClip); 
#else
            base.AddMethod((PopClipDelegate)PopClip); 
#endif
        }

        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit() => global::MicroCom.Runtime.MicroComRuntime.RegisterVTable(typeof(IAvgDrawingContext), new __MicroComIAvgDrawingContextVTable().CreateVTable());
    }

    public unsafe partial class __MicroComIAvgPathProxy : global::MicroCom.Runtime.MicroComProxyBase, IAvgPath
    {
        public void ArcTo(AvgPoint point, AvgSize size, double rotationAngle, int isLargeArc, AvgSweepDirection sweepDirection)
        {
            ((delegate* unmanaged[Stdcall]<void*, AvgPoint, AvgSize, double, int, AvgSweepDirection, void>)(*PPV)[base.VTableSize + 0])(PPV, point, size, rotationAngle, isLargeArc, sweepDirection);
        }

        public void BeginFigure(AvgPoint startPoint, int isFilled)
        {
            ((delegate* unmanaged[Stdcall]<void*, AvgPoint, int, void>)(*PPV)[base.VTableSize + 1])(PPV, startPoint, isFilled);
        }

        public void CubicBezierTo(AvgPoint p1, AvgPoint p2, AvgPoint p3)
        {
            ((delegate* unmanaged[Stdcall]<void*, AvgPoint, AvgPoint, AvgPoint, void>)(*PPV)[base.VTableSize + 2])(PPV, p1, p2, p3);
        }

        public void QuadraticBezierTo(AvgPoint p1, AvgPoint p2)
        {
            ((delegate* unmanaged[Stdcall]<void*, AvgPoint, AvgPoint, void>)(*PPV)[base.VTableSize + 3])(PPV, p1, p2);
        }

        public void LineTo(AvgPoint point)
        {
            ((delegate* unmanaged[Stdcall]<void*, AvgPoint, void>)(*PPV)[base.VTableSize + 4])(PPV, point);
        }

        public void EndFigure(int isClosed)
        {
            ((delegate* unmanaged[Stdcall]<void*, int, void>)(*PPV)[base.VTableSize + 5])(PPV, isClosed);
        }

        public void SetFillRule(AvgFillRule fillRule)
        {
            ((delegate* unmanaged[Stdcall]<void*, AvgFillRule, void>)(*PPV)[base.VTableSize + 6])(PPV, fillRule);
        }

        public void AddRect(AvgRect rect)
        {
            ((delegate* unmanaged[Stdcall]<void*, AvgRect, void>)(*PPV)[base.VTableSize + 7])(PPV, rect);
        }

        public void MoveTo(AvgPoint point)
        {
            ((delegate* unmanaged[Stdcall]<void*, AvgPoint, void>)(*PPV)[base.VTableSize + 8])(PPV, point);
        }

        public void AddOval(AvgRect rect)
        {
            ((delegate* unmanaged[Stdcall]<void*, AvgRect, void>)(*PPV)[base.VTableSize + 9])(PPV, rect);
        }

        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit()
        {
            global::MicroCom.Runtime.MicroComRuntime.Register(typeof(IAvgPath), new Guid("22E1D577-1248-4737-9220-56B7DAC49BF2"), (p, owns) => new __MicroComIAvgPathProxy(p, owns));
        }

        protected __MicroComIAvgPathProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 10;
    }

    unsafe class __MicroComIAvgPathVTable : global::MicroCom.Runtime.MicroComVtblBase
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate void ArcToDelegate(void* @this, AvgPoint point, AvgSize size, double rotationAngle, int isLargeArc, AvgSweepDirection sweepDirection);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static void ArcTo(void* @this, AvgPoint point, AvgSize size, double rotationAngle, int isLargeArc, AvgSweepDirection sweepDirection)
        {
            IAvgPath __target = null;
            try
            {
                {
                    __target = (IAvgPath)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    __target.ArcTo(point, size, rotationAngle, isLargeArc, sweepDirection);
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                ;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate void BeginFigureDelegate(void* @this, AvgPoint startPoint, int isFilled);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static void BeginFigure(void* @this, AvgPoint startPoint, int isFilled)
        {
            IAvgPath __target = null;
            try
            {
                {
                    __target = (IAvgPath)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    __target.BeginFigure(startPoint, isFilled);
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                ;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate void CubicBezierToDelegate(void* @this, AvgPoint p1, AvgPoint p2, AvgPoint p3);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static void CubicBezierTo(void* @this, AvgPoint p1, AvgPoint p2, AvgPoint p3)
        {
            IAvgPath __target = null;
            try
            {
                {
                    __target = (IAvgPath)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    __target.CubicBezierTo(p1, p2, p3);
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                ;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate void QuadraticBezierToDelegate(void* @this, AvgPoint p1, AvgPoint p2);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static void QuadraticBezierTo(void* @this, AvgPoint p1, AvgPoint p2)
        {
            IAvgPath __target = null;
            try
            {
                {
                    __target = (IAvgPath)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    __target.QuadraticBezierTo(p1, p2);
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                ;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate void LineToDelegate(void* @this, AvgPoint point);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static void LineTo(void* @this, AvgPoint point)
        {
            IAvgPath __target = null;
            try
            {
                {
                    __target = (IAvgPath)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    __target.LineTo(point);
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                ;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate void EndFigureDelegate(void* @this, int isClosed);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static void EndFigure(void* @this, int isClosed)
        {
            IAvgPath __target = null;
            try
            {
                {
                    __target = (IAvgPath)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    __target.EndFigure(isClosed);
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                ;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate void SetFillRuleDelegate(void* @this, AvgFillRule fillRule);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static void SetFillRule(void* @this, AvgFillRule fillRule)
        {
            IAvgPath __target = null;
            try
            {
                {
                    __target = (IAvgPath)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    __target.SetFillRule(fillRule);
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                ;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate void AddRectDelegate(void* @this, AvgRect rect);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static void AddRect(void* @this, AvgRect rect)
        {
            IAvgPath __target = null;
            try
            {
                {
                    __target = (IAvgPath)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    __target.AddRect(rect);
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                ;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate void MoveToDelegate(void* @this, AvgPoint point);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static void MoveTo(void* @this, AvgPoint point)
        {
            IAvgPath __target = null;
            try
            {
                {
                    __target = (IAvgPath)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    __target.MoveTo(point);
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                ;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate void AddOvalDelegate(void* @this, AvgRect rect);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static void AddOval(void* @this, AvgRect rect)
        {
            IAvgPath __target = null;
            try
            {
                {
                    __target = (IAvgPath)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    __target.AddOval(rect);
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                ;
            }
        }

        protected __MicroComIAvgPathVTable()
        {
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, AvgPoint, AvgSize, double, int, AvgSweepDirection, void>)&ArcTo); 
#else
            base.AddMethod((ArcToDelegate)ArcTo); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, AvgPoint, int, void>)&BeginFigure); 
#else
            base.AddMethod((BeginFigureDelegate)BeginFigure); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, AvgPoint, AvgPoint, AvgPoint, void>)&CubicBezierTo); 
#else
            base.AddMethod((CubicBezierToDelegate)CubicBezierTo); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, AvgPoint, AvgPoint, void>)&QuadraticBezierTo); 
#else
            base.AddMethod((QuadraticBezierToDelegate)QuadraticBezierTo); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, AvgPoint, void>)&LineTo); 
#else
            base.AddMethod((LineToDelegate)LineTo); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, int, void>)&EndFigure); 
#else
            base.AddMethod((EndFigureDelegate)EndFigure); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, AvgFillRule, void>)&SetFillRule); 
#else
            base.AddMethod((SetFillRuleDelegate)SetFillRule); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, AvgRect, void>)&AddRect); 
#else
            base.AddMethod((AddRectDelegate)AddRect); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, AvgPoint, void>)&MoveTo); 
#else
            base.AddMethod((MoveToDelegate)MoveTo); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, AvgRect, void>)&AddOval); 
#else
            base.AddMethod((AddOvalDelegate)AddOval); 
#endif
        }

        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit() => global::MicroCom.Runtime.MicroComRuntime.RegisterVTable(typeof(IAvgPath), new __MicroComIAvgPathVTable().CreateVTable());
    }

    public unsafe partial class __MicroComIAvgStringProxy : global::MicroCom.Runtime.MicroComProxyBase, IAvgString
    {
        public void* Pointer()
        {
            int __result;
            void* retOut = default;
            __result = (int)((delegate* unmanaged[Stdcall]<void*, void*, int>)(*PPV)[base.VTableSize + 0])(PPV, &retOut);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("Pointer failed", __result);
            return retOut;
        }

        public int Length()
        {
            int __result;
            int ret = default;
            __result = (int)((delegate* unmanaged[Stdcall]<void*, void*, int>)(*PPV)[base.VTableSize + 1])(PPV, &ret);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("Length failed", __result);
            return ret;
        }

        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit()
        {
            global::MicroCom.Runtime.MicroComRuntime.Register(typeof(IAvgString), new Guid("233e094f-9b9f-44a3-9a6e-6948bbdd9fb1"), (p, owns) => new __MicroComIAvgStringProxy(p, owns));
        }

        protected __MicroComIAvgStringProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 2;
    }

    unsafe class __MicroComIAvgStringVTable : global::MicroCom.Runtime.MicroComVtblBase
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int PointerDelegate(void* @this, void** retOut);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int Pointer(void* @this, void** retOut)
        {
            IAvgString __target = null;
            try
            {
                {
                    __target = (IAvgString)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.Pointer();
                        *retOut = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int LengthDelegate(void* @this, int* ret);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int Length(void* @this, int* ret)
        {
            IAvgString __target = null;
            try
            {
                {
                    __target = (IAvgString)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.Length();
                        *ret = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        protected __MicroComIAvgStringVTable()
        {
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, void**, int>)&Pointer); 
#else
            base.AddMethod((PointerDelegate)Pointer); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, int*, int>)&Length); 
#else
            base.AddMethod((LengthDelegate)Length); 
#endif
        }

        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit() => global::MicroCom.Runtime.MicroComRuntime.RegisterVTable(typeof(IAvgString), new __MicroComIAvgStringVTable().CreateVTable());
    }

    public unsafe partial class __MicroComIAvgFontManagerProxy : global::MicroCom.Runtime.MicroComProxyBase, IAvgFontManager
    {
        public IAvgString DefaultFamilyName
        {
            get
            {
                void* __result;
                __result = (void*)((delegate* unmanaged[Stdcall]<void*, void*>)(*PPV)[base.VTableSize + 0])(PPV);
                return global::MicroCom.Runtime.MicroComRuntime.CreateProxyOrNullFor<IAvgString>(__result, true);
            }
        }

        public int FontFamilyCount
        {
            get
            {
                int __result;
                __result = (int)((delegate* unmanaged[Stdcall]<void*, int>)(*PPV)[base.VTableSize + 1])(PPV);
                return __result;
            }
        }

        public IAvgString GetFamilyName(int index)
        {
            void* __result;
            __result = (void*)((delegate* unmanaged[Stdcall]<void*, int, void*>)(*PPV)[base.VTableSize + 2])(PPV, index);
            return global::MicroCom.Runtime.MicroComRuntime.CreateProxyOrNullFor<IAvgString>(__result, true);
        }

        public IAvgGlyphTypeface CreateGlyphTypeface(string fontFamily, AvgTypeface typeface)
        {
            void* __result;
            var __bytemarshal_fontFamily = new byte[System.Text.Encoding.UTF8.GetByteCount(fontFamily) + 1];
            System.Text.Encoding.UTF8.GetBytes(fontFamily, 0, fontFamily.Length, __bytemarshal_fontFamily, 0);
            fixed (byte* __fixedmarshal_fontFamily = __bytemarshal_fontFamily)
                __result = (void*)((delegate* unmanaged[Stdcall]<void*, void*, AvgTypeface, void*>)(*PPV)[base.VTableSize + 3])(PPV, __fixedmarshal_fontFamily, typeface);
            return global::MicroCom.Runtime.MicroComRuntime.CreateProxyOrNullFor<IAvgGlyphTypeface>(__result, true);
        }

        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit()
        {
            global::MicroCom.Runtime.MicroComRuntime.Register(typeof(IAvgFontManager), new Guid("58B0D106-EB8C-4DDB-AB3D-A61DBC7BCB6D"), (p, owns) => new __MicroComIAvgFontManagerProxy(p, owns));
        }

        protected __MicroComIAvgFontManagerProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 4;
    }

    unsafe class __MicroComIAvgFontManagerVTable : global::MicroCom.Runtime.MicroComVtblBase
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate void* GetDefaultFamilyNameDelegate(void* @this);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static void* GetDefaultFamilyName(void* @this)
        {
            IAvgFontManager __target = null;
            try
            {
                {
                    __target = (IAvgFontManager)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.DefaultFamilyName;
                        return global::MicroCom.Runtime.MicroComRuntime.GetNativePointer(__result, true);
                    }
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return default;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetFontFamilyCountDelegate(void* @this);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int GetFontFamilyCount(void* @this)
        {
            IAvgFontManager __target = null;
            try
            {
                {
                    __target = (IAvgFontManager)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.FontFamilyCount;
                        return __result;
                    }
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return default;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate void* GetFamilyNameDelegate(void* @this, int index);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static void* GetFamilyName(void* @this, int index)
        {
            IAvgFontManager __target = null;
            try
            {
                {
                    __target = (IAvgFontManager)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.GetFamilyName(index);
                        return global::MicroCom.Runtime.MicroComRuntime.GetNativePointer(__result, true);
                    }
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return default;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate void* CreateGlyphTypefaceDelegate(void* @this, byte* fontFamily, AvgTypeface typeface);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static void* CreateGlyphTypeface(void* @this, byte* fontFamily, AvgTypeface typeface)
        {
            IAvgFontManager __target = null;
            try
            {
                {
                    __target = (IAvgFontManager)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.CreateGlyphTypeface((fontFamily == null ? null : System.Runtime.InteropServices.Marshal.PtrToStringAnsi(new IntPtr(fontFamily))), typeface);
                        return global::MicroCom.Runtime.MicroComRuntime.GetNativePointer(__result, true);
                    }
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return default;
            }
        }

        protected __MicroComIAvgFontManagerVTable()
        {
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, void*>)&GetDefaultFamilyName); 
#else
            base.AddMethod((GetDefaultFamilyNameDelegate)GetDefaultFamilyName); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, int>)&GetFontFamilyCount); 
#else
            base.AddMethod((GetFontFamilyCountDelegate)GetFontFamilyCount); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, int, void*>)&GetFamilyName); 
#else
            base.AddMethod((GetFamilyNameDelegate)GetFamilyName); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, byte*, AvgTypeface, void*>)&CreateGlyphTypeface); 
#else
            base.AddMethod((CreateGlyphTypefaceDelegate)CreateGlyphTypeface); 
#endif
        }

        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit() => global::MicroCom.Runtime.MicroComRuntime.RegisterVTable(typeof(IAvgFontManager), new __MicroComIAvgFontManagerVTable().CreateVTable());
    }

    public unsafe partial class __MicroComIAvgRenderInterfaceProxy : global::MicroCom.Runtime.MicroComProxyBase, IAvgRenderInterface
    {
        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit()
        {
            global::MicroCom.Runtime.MicroComRuntime.Register(typeof(IAvgRenderInterface), new Guid("02F671AE-B7B9-45EE-A89E-D5BF14DFAE84"), (p, owns) => new __MicroComIAvgRenderInterfaceProxy(p, owns));
        }

        protected __MicroComIAvgRenderInterfaceProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 0;
    }

    unsafe class __MicroComIAvgRenderInterfaceVTable : global::MicroCom.Runtime.MicroComVtblBase
    {
        protected __MicroComIAvgRenderInterfaceVTable()
        {
        }

        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit() => global::MicroCom.Runtime.MicroComRuntime.RegisterVTable(typeof(IAvgRenderInterface), new __MicroComIAvgRenderInterfaceVTable().CreateVTable());
    }

    public unsafe partial class __MicroComIAvgGlyphTypefaceProxy : global::MicroCom.Runtime.MicroComProxyBase, IAvgGlyphTypeface
    {
        public uint GetGlyph(uint codepoint)
        {
            uint __result;
            __result = (uint)((delegate* unmanaged[Stdcall]<void*, uint, uint>)(*PPV)[base.VTableSize + 0])(PPV, codepoint);
            return __result;
        }

        public int GetGlyphAdvance(uint glyph)
        {
            int __result;
            __result = (int)((delegate* unmanaged[Stdcall]<void*, uint, int>)(*PPV)[base.VTableSize + 1])(PPV, glyph);
            return __result;
        }

        public int DesignEmHeight
        {
            get
            {
                int __result;
                __result = (int)((delegate* unmanaged[Stdcall]<void*, int>)(*PPV)[base.VTableSize + 2])(PPV);
                return __result;
            }
        }

        public int Ascent
        {
            get
            {
                int __result;
                __result = (int)((delegate* unmanaged[Stdcall]<void*, int>)(*PPV)[base.VTableSize + 3])(PPV);
                return __result;
            }
        }

        public int Descent
        {
            get
            {
                int __result;
                __result = (int)((delegate* unmanaged[Stdcall]<void*, int>)(*PPV)[base.VTableSize + 4])(PPV);
                return __result;
            }
        }

        public int LineGap
        {
            get
            {
                int __result;
                __result = (int)((delegate* unmanaged[Stdcall]<void*, int>)(*PPV)[base.VTableSize + 5])(PPV);
                return __result;
            }
        }

        public int UnderlinePosition
        {
            get
            {
                int __result;
                __result = (int)((delegate* unmanaged[Stdcall]<void*, int>)(*PPV)[base.VTableSize + 6])(PPV);
                return __result;
            }
        }

        public int UnderlineThickness
        {
            get
            {
                int __result;
                __result = (int)((delegate* unmanaged[Stdcall]<void*, int>)(*PPV)[base.VTableSize + 7])(PPV);
                return __result;
            }
        }

        public int StrikethroughPosition
        {
            get
            {
                int __result;
                __result = (int)((delegate* unmanaged[Stdcall]<void*, int>)(*PPV)[base.VTableSize + 8])(PPV);
                return __result;
            }
        }

        public int StrikethroughThickness
        {
            get
            {
                int __result;
                __result = (int)((delegate* unmanaged[Stdcall]<void*, int>)(*PPV)[base.VTableSize + 9])(PPV);
                return __result;
            }
        }

        public int IsFixedPitch
        {
            get
            {
                int __result;
                __result = (int)((delegate* unmanaged[Stdcall]<void*, int>)(*PPV)[base.VTableSize + 10])(PPV);
                return __result;
            }
        }

        public int IsFakeBold
        {
            get
            {
                int __result;
                __result = (int)((delegate* unmanaged[Stdcall]<void*, int>)(*PPV)[base.VTableSize + 11])(PPV);
                return __result;
            }
        }

        public int IsFakeItalic
        {
            get
            {
                int __result;
                __result = (int)((delegate* unmanaged[Stdcall]<void*, int>)(*PPV)[base.VTableSize + 12])(PPV);
                return __result;
            }
        }

        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit()
        {
            global::MicroCom.Runtime.MicroComRuntime.Register(typeof(IAvgGlyphTypeface), new Guid("D10A435A-7378-4753-946B-94009D0DF2B6"), (p, owns) => new __MicroComIAvgGlyphTypefaceProxy(p, owns));
        }

        protected __MicroComIAvgGlyphTypefaceProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 13;
    }

    unsafe class __MicroComIAvgGlyphTypefaceVTable : global::MicroCom.Runtime.MicroComVtblBase
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate uint GetGlyphDelegate(void* @this, uint codepoint);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static uint GetGlyph(void* @this, uint codepoint)
        {
            IAvgGlyphTypeface __target = null;
            try
            {
                {
                    __target = (IAvgGlyphTypeface)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.GetGlyph(codepoint);
                        return __result;
                    }
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return default;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetGlyphAdvanceDelegate(void* @this, uint glyph);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int GetGlyphAdvance(void* @this, uint glyph)
        {
            IAvgGlyphTypeface __target = null;
            try
            {
                {
                    __target = (IAvgGlyphTypeface)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.GetGlyphAdvance(glyph);
                        return __result;
                    }
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return default;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetDesignEmHeightDelegate(void* @this);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int GetDesignEmHeight(void* @this)
        {
            IAvgGlyphTypeface __target = null;
            try
            {
                {
                    __target = (IAvgGlyphTypeface)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.DesignEmHeight;
                        return __result;
                    }
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return default;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetAscentDelegate(void* @this);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int GetAscent(void* @this)
        {
            IAvgGlyphTypeface __target = null;
            try
            {
                {
                    __target = (IAvgGlyphTypeface)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.Ascent;
                        return __result;
                    }
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return default;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetDescentDelegate(void* @this);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int GetDescent(void* @this)
        {
            IAvgGlyphTypeface __target = null;
            try
            {
                {
                    __target = (IAvgGlyphTypeface)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.Descent;
                        return __result;
                    }
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return default;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetLineGapDelegate(void* @this);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int GetLineGap(void* @this)
        {
            IAvgGlyphTypeface __target = null;
            try
            {
                {
                    __target = (IAvgGlyphTypeface)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.LineGap;
                        return __result;
                    }
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return default;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetUnderlinePositionDelegate(void* @this);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int GetUnderlinePosition(void* @this)
        {
            IAvgGlyphTypeface __target = null;
            try
            {
                {
                    __target = (IAvgGlyphTypeface)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.UnderlinePosition;
                        return __result;
                    }
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return default;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetUnderlineThicknessDelegate(void* @this);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int GetUnderlineThickness(void* @this)
        {
            IAvgGlyphTypeface __target = null;
            try
            {
                {
                    __target = (IAvgGlyphTypeface)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.UnderlineThickness;
                        return __result;
                    }
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return default;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetStrikethroughPositionDelegate(void* @this);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int GetStrikethroughPosition(void* @this)
        {
            IAvgGlyphTypeface __target = null;
            try
            {
                {
                    __target = (IAvgGlyphTypeface)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.StrikethroughPosition;
                        return __result;
                    }
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return default;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetStrikethroughThicknessDelegate(void* @this);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int GetStrikethroughThickness(void* @this)
        {
            IAvgGlyphTypeface __target = null;
            try
            {
                {
                    __target = (IAvgGlyphTypeface)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.StrikethroughThickness;
                        return __result;
                    }
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return default;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetIsFixedPitchDelegate(void* @this);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int GetIsFixedPitch(void* @this)
        {
            IAvgGlyphTypeface __target = null;
            try
            {
                {
                    __target = (IAvgGlyphTypeface)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.IsFixedPitch;
                        return __result;
                    }
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return default;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetIsFakeBoldDelegate(void* @this);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int GetIsFakeBold(void* @this)
        {
            IAvgGlyphTypeface __target = null;
            try
            {
                {
                    __target = (IAvgGlyphTypeface)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.IsFakeBold;
                        return __result;
                    }
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return default;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetIsFakeItalicDelegate(void* @this);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int GetIsFakeItalic(void* @this)
        {
            IAvgGlyphTypeface __target = null;
            try
            {
                {
                    __target = (IAvgGlyphTypeface)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.IsFakeItalic;
                        return __result;
                    }
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return default;
            }
        }

        protected __MicroComIAvgGlyphTypefaceVTable()
        {
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, uint, uint>)&GetGlyph); 
#else
            base.AddMethod((GetGlyphDelegate)GetGlyph); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, uint, int>)&GetGlyphAdvance); 
#else
            base.AddMethod((GetGlyphAdvanceDelegate)GetGlyphAdvance); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, int>)&GetDesignEmHeight); 
#else
            base.AddMethod((GetDesignEmHeightDelegate)GetDesignEmHeight); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, int>)&GetAscent); 
#else
            base.AddMethod((GetAscentDelegate)GetAscent); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, int>)&GetDescent); 
#else
            base.AddMethod((GetDescentDelegate)GetDescent); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, int>)&GetLineGap); 
#else
            base.AddMethod((GetLineGapDelegate)GetLineGap); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, int>)&GetUnderlinePosition); 
#else
            base.AddMethod((GetUnderlinePositionDelegate)GetUnderlinePosition); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, int>)&GetUnderlineThickness); 
#else
            base.AddMethod((GetUnderlineThicknessDelegate)GetUnderlineThickness); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, int>)&GetStrikethroughPosition); 
#else
            base.AddMethod((GetStrikethroughPositionDelegate)GetStrikethroughPosition); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, int>)&GetStrikethroughThickness); 
#else
            base.AddMethod((GetStrikethroughThicknessDelegate)GetStrikethroughThickness); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, int>)&GetIsFixedPitch); 
#else
            base.AddMethod((GetIsFixedPitchDelegate)GetIsFixedPitch); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, int>)&GetIsFakeBold); 
#else
            base.AddMethod((GetIsFakeBoldDelegate)GetIsFakeBold); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, int>)&GetIsFakeItalic); 
#else
            base.AddMethod((GetIsFakeItalicDelegate)GetIsFakeItalic); 
#endif
        }

        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit() => global::MicroCom.Runtime.MicroComRuntime.RegisterVTable(typeof(IAvgGlyphTypeface), new __MicroComIAvgGlyphTypefaceVTable().CreateVTable());
    }

    public unsafe partial class __MicroComIAvgFontShapeBufferProxy : global::MicroCom.Runtime.MicroComProxyBase, IAvgFontShapeBuffer
    {
        public int Length
        {
            get
            {
                int __result;
                __result = (int)((delegate* unmanaged[Stdcall]<void*, int>)(*PPV)[base.VTableSize + 0])(PPV);
                return __result;
            }
        }

        public void GuessSegmentProperties()
        {
            ((delegate* unmanaged[Stdcall]<void*, void>)(*PPV)[base.VTableSize + 1])(PPV);
        }

        public void SetDirection(int direction)
        {
            ((delegate* unmanaged[Stdcall]<void*, int, void>)(*PPV)[base.VTableSize + 2])(PPV, direction);
        }

        public void SetLanguage(void* language)
        {
            ((delegate* unmanaged[Stdcall]<void*, void*, void>)(*PPV)[base.VTableSize + 3])(PPV, language);
        }

        public void AddUtf16(void* utf16, int length, int itemOffset, int itemLength)
        {
            ((delegate* unmanaged[Stdcall]<void*, void*, int, int, int, void>)(*PPV)[base.VTableSize + 4])(PPV, utf16, length, itemOffset, itemLength);
        }

        public void Shape()
        {
            ((delegate* unmanaged[Stdcall]<void*, void>)(*PPV)[base.VTableSize + 5])(PPV);
        }

        public void GetScale(int* x, int* y)
        {
            ((delegate* unmanaged[Stdcall]<void*, void*, void*, void>)(*PPV)[base.VTableSize + 6])(PPV, x, y);
        }

        public void* GetGlyphInfoSpan(uint* length)
        {
            void* __result;
            __result = (void*)((delegate* unmanaged[Stdcall]<void*, void*, void*>)(*PPV)[base.VTableSize + 7])(PPV, length);
            return __result;
        }

        public void* GetGlyphPositionSpan(uint* length)
        {
            void* __result;
            __result = (void*)((delegate* unmanaged[Stdcall]<void*, void*, void*>)(*PPV)[base.VTableSize + 8])(PPV, length);
            return __result;
        }

        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit()
        {
            global::MicroCom.Runtime.MicroComRuntime.Register(typeof(IAvgFontShapeBuffer), new Guid("98765BAA-1FEC-400F-94A0-B74C299152F9"), (p, owns) => new __MicroComIAvgFontShapeBufferProxy(p, owns));
        }

        protected __MicroComIAvgFontShapeBufferProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 9;
    }

    unsafe class __MicroComIAvgFontShapeBufferVTable : global::MicroCom.Runtime.MicroComVtblBase
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetLengthDelegate(void* @this);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int GetLength(void* @this)
        {
            IAvgFontShapeBuffer __target = null;
            try
            {
                {
                    __target = (IAvgFontShapeBuffer)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.Length;
                        return __result;
                    }
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return default;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate void GuessSegmentPropertiesDelegate(void* @this);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static void GuessSegmentProperties(void* @this)
        {
            IAvgFontShapeBuffer __target = null;
            try
            {
                {
                    __target = (IAvgFontShapeBuffer)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    __target.GuessSegmentProperties();
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                ;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate void SetDirectionDelegate(void* @this, int direction);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static void SetDirection(void* @this, int direction)
        {
            IAvgFontShapeBuffer __target = null;
            try
            {
                {
                    __target = (IAvgFontShapeBuffer)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    __target.SetDirection(direction);
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                ;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate void SetLanguageDelegate(void* @this, void* language);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static void SetLanguage(void* @this, void* language)
        {
            IAvgFontShapeBuffer __target = null;
            try
            {
                {
                    __target = (IAvgFontShapeBuffer)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    __target.SetLanguage(language);
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                ;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate void AddUtf16Delegate(void* @this, void* utf16, int length, int itemOffset, int itemLength);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static void AddUtf16(void* @this, void* utf16, int length, int itemOffset, int itemLength)
        {
            IAvgFontShapeBuffer __target = null;
            try
            {
                {
                    __target = (IAvgFontShapeBuffer)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    __target.AddUtf16(utf16, length, itemOffset, itemLength);
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                ;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate void ShapeDelegate(void* @this);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static void Shape(void* @this)
        {
            IAvgFontShapeBuffer __target = null;
            try
            {
                {
                    __target = (IAvgFontShapeBuffer)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    __target.Shape();
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                ;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate void GetScaleDelegate(void* @this, int* x, int* y);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static void GetScale(void* @this, int* x, int* y)
        {
            IAvgFontShapeBuffer __target = null;
            try
            {
                {
                    __target = (IAvgFontShapeBuffer)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    __target.GetScale(x, y);
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                ;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate void* GetGlyphInfoSpanDelegate(void* @this, uint* length);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static void* GetGlyphInfoSpan(void* @this, uint* length)
        {
            IAvgFontShapeBuffer __target = null;
            try
            {
                {
                    __target = (IAvgFontShapeBuffer)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.GetGlyphInfoSpan(length);
                        return __result;
                    }
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return default;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate void* GetGlyphPositionSpanDelegate(void* @this, uint* length);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static void* GetGlyphPositionSpan(void* @this, uint* length)
        {
            IAvgFontShapeBuffer __target = null;
            try
            {
                {
                    __target = (IAvgFontShapeBuffer)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.GetGlyphPositionSpan(length);
                        return __result;
                    }
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return default;
            }
        }

        protected __MicroComIAvgFontShapeBufferVTable()
        {
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, int>)&GetLength); 
#else
            base.AddMethod((GetLengthDelegate)GetLength); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, void>)&GuessSegmentProperties); 
#else
            base.AddMethod((GuessSegmentPropertiesDelegate)GuessSegmentProperties); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, int, void>)&SetDirection); 
#else
            base.AddMethod((SetDirectionDelegate)SetDirection); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, void*, void>)&SetLanguage); 
#else
            base.AddMethod((SetLanguageDelegate)SetLanguage); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, void*, int, int, int, void>)&AddUtf16); 
#else
            base.AddMethod((AddUtf16Delegate)AddUtf16); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, void>)&Shape); 
#else
            base.AddMethod((ShapeDelegate)Shape); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, int*, int*, void>)&GetScale); 
#else
            base.AddMethod((GetScaleDelegate)GetScale); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, uint*, void*>)&GetGlyphInfoSpan); 
#else
            base.AddMethod((GetGlyphInfoSpanDelegate)GetGlyphInfoSpan); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, uint*, void*>)&GetGlyphPositionSpan); 
#else
            base.AddMethod((GetGlyphPositionSpanDelegate)GetGlyphPositionSpan); 
#endif
        }

        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit() => global::MicroCom.Runtime.MicroComRuntime.RegisterVTable(typeof(IAvgFontShapeBuffer), new __MicroComIAvgFontShapeBufferVTable().CreateVTable());
    }

    public unsafe partial class __MicroComIAvgGlyphRunProxy : global::MicroCom.Runtime.MicroComProxyBase, IAvgGlyphRun
    {
        public void AllocRun(int count)
        {
            int __result;
            __result = (int)((delegate* unmanaged[Stdcall]<void*, int, int>)(*PPV)[base.VTableSize + 0])(PPV, count);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("AllocRun failed", __result);
        }

        public void AllocHorizontalRun(int count)
        {
            int __result;
            __result = (int)((delegate* unmanaged[Stdcall]<void*, int, int>)(*PPV)[base.VTableSize + 1])(PPV, count);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("AllocHorizontalRun failed", __result);
        }

        public void AllocPositionedRun(int count)
        {
            int __result;
            __result = (int)((delegate* unmanaged[Stdcall]<void*, int, int>)(*PPV)[base.VTableSize + 2])(PPV, count);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("AllocPositionedRun failed", __result);
        }

        public void SetFontSize(float size)
        {
            ((delegate* unmanaged[Stdcall]<void*, float, void>)(*PPV)[base.VTableSize + 3])(PPV, size);
        }

        public void* GlyphBuffer
        {
            get
            {
                void* __result;
                __result = (void*)((delegate* unmanaged[Stdcall]<void*, void*>)(*PPV)[base.VTableSize + 4])(PPV);
                return __result;
            }
        }

        public void* PositionsBuffer
        {
            get
            {
                void* __result;
                __result = (void*)((delegate* unmanaged[Stdcall]<void*, void*>)(*PPV)[base.VTableSize + 5])(PPV);
                return __result;
            }
        }

        public void BuildText()
        {
            ((delegate* unmanaged[Stdcall]<void*, void>)(*PPV)[base.VTableSize + 6])(PPV);
        }

        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit()
        {
            global::MicroCom.Runtime.MicroComRuntime.Register(typeof(IAvgGlyphRun), new Guid("DF88C411-3ED3-4EE6-BAF6-33BA027AB6DF"), (p, owns) => new __MicroComIAvgGlyphRunProxy(p, owns));
        }

        protected __MicroComIAvgGlyphRunProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 7;
    }

    unsafe class __MicroComIAvgGlyphRunVTable : global::MicroCom.Runtime.MicroComVtblBase
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int AllocRunDelegate(void* @this, int count);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int AllocRun(void* @this, int count)
        {
            IAvgGlyphRun __target = null;
            try
            {
                {
                    __target = (IAvgGlyphRun)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    __target.AllocRun(count);
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int AllocHorizontalRunDelegate(void* @this, int count);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int AllocHorizontalRun(void* @this, int count)
        {
            IAvgGlyphRun __target = null;
            try
            {
                {
                    __target = (IAvgGlyphRun)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    __target.AllocHorizontalRun(count);
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int AllocPositionedRunDelegate(void* @this, int count);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int AllocPositionedRun(void* @this, int count)
        {
            IAvgGlyphRun __target = null;
            try
            {
                {
                    __target = (IAvgGlyphRun)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    __target.AllocPositionedRun(count);
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate void SetFontSizeDelegate(void* @this, float size);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static void SetFontSize(void* @this, float size)
        {
            IAvgGlyphRun __target = null;
            try
            {
                {
                    __target = (IAvgGlyphRun)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    __target.SetFontSize(size);
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                ;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate void* GetGlyphBufferDelegate(void* @this);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static void* GetGlyphBuffer(void* @this)
        {
            IAvgGlyphRun __target = null;
            try
            {
                {
                    __target = (IAvgGlyphRun)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.GlyphBuffer;
                        return __result;
                    }
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return default;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate void* GetPositionsBufferDelegate(void* @this);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static void* GetPositionsBuffer(void* @this)
        {
            IAvgGlyphRun __target = null;
            try
            {
                {
                    __target = (IAvgGlyphRun)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.PositionsBuffer;
                        return __result;
                    }
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return default;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate void BuildTextDelegate(void* @this);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static void BuildText(void* @this)
        {
            IAvgGlyphRun __target = null;
            try
            {
                {
                    __target = (IAvgGlyphRun)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    __target.BuildText();
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                ;
            }
        }

        protected __MicroComIAvgGlyphRunVTable()
        {
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, int, int>)&AllocRun); 
#else
            base.AddMethod((AllocRunDelegate)AllocRun); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, int, int>)&AllocHorizontalRun); 
#else
            base.AddMethod((AllocHorizontalRunDelegate)AllocHorizontalRun); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, int, int>)&AllocPositionedRun); 
#else
            base.AddMethod((AllocPositionedRunDelegate)AllocPositionedRun); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, float, void>)&SetFontSize); 
#else
            base.AddMethod((SetFontSizeDelegate)SetFontSize); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, void*>)&GetGlyphBuffer); 
#else
            base.AddMethod((GetGlyphBufferDelegate)GetGlyphBuffer); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, void*>)&GetPositionsBuffer); 
#else
            base.AddMethod((GetPositionsBufferDelegate)GetPositionsBuffer); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, void>)&BuildText); 
#else
            base.AddMethod((BuildTextDelegate)BuildText); 
#endif
        }

        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit() => global::MicroCom.Runtime.MicroComRuntime.RegisterVTable(typeof(IAvgGlyphRun), new __MicroComIAvgGlyphRunVTable().CreateVTable());
    }

    public unsafe partial class __MicroComIAvgRenderTargetProxy : global::MicroCom.Runtime.MicroComProxyBase, IAvgRenderTarget
    {
        public IAvgDrawingContext CreateDrawingContext()
        {
            int __result;
            void* __marshal_ppv = null;
            __result = (int)((delegate* unmanaged[Stdcall]<void*, void*, int>)(*PPV)[base.VTableSize + 0])(PPV, &__marshal_ppv);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateDrawingContext failed", __result);
            return global::MicroCom.Runtime.MicroComRuntime.CreateProxyOrNullFor<IAvgDrawingContext>(__marshal_ppv, true);
        }

        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit()
        {
            global::MicroCom.Runtime.MicroComRuntime.Register(typeof(IAvgRenderTarget), new Guid("04D2ADF7-F4DF-4836-A60E-7699E5E53EC7"), (p, owns) => new __MicroComIAvgRenderTargetProxy(p, owns));
        }

        protected __MicroComIAvgRenderTargetProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 1;
    }

    unsafe class __MicroComIAvgRenderTargetVTable : global::MicroCom.Runtime.MicroComVtblBase
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateDrawingContextDelegate(void* @this, void** ppv);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int CreateDrawingContext(void* @this, void** ppv)
        {
            IAvgRenderTarget __target = null;
            try
            {
                {
                    __target = (IAvgRenderTarget)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.CreateDrawingContext();
                        *ppv = global::MicroCom.Runtime.MicroComRuntime.GetNativePointer(__result, true);
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        protected __MicroComIAvgRenderTargetVTable()
        {
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, void**, int>)&CreateDrawingContext); 
#else
            base.AddMethod((CreateDrawingContextDelegate)CreateDrawingContext); 
#endif
        }

        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit() => global::MicroCom.Runtime.MicroComRuntime.RegisterVTable(typeof(IAvgRenderTarget), new __MicroComIAvgRenderTargetVTable().CreateVTable());
    }

    public unsafe partial class __MicroComIAvgGpuControlProxy : global::MicroCom.Runtime.MicroComProxyBase, IAvgGpuControl
    {
        public IUnknown Lock()
        {
            int __result;
            void* __marshal_ppv = null;
            __result = (int)((delegate* unmanaged[Stdcall]<void*, void*, int>)(*PPV)[base.VTableSize + 0])(PPV, &__marshal_ppv);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("Lock failed", __result);
            return global::MicroCom.Runtime.MicroComRuntime.CreateProxyOrNullFor<IUnknown>(__marshal_ppv, true);
        }

        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit()
        {
            global::MicroCom.Runtime.MicroComRuntime.Register(typeof(IAvgGpuControl), new Guid("7BB8B147-F9C7-49CE-905D-F08AB0EC632F"), (p, owns) => new __MicroComIAvgGpuControlProxy(p, owns));
        }

        protected __MicroComIAvgGpuControlProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 1;
    }

    unsafe class __MicroComIAvgGpuControlVTable : global::MicroCom.Runtime.MicroComVtblBase
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int LockDelegate(void* @this, void** ppv);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int Lock(void* @this, void** ppv)
        {
            IAvgGpuControl __target = null;
            try
            {
                {
                    __target = (IAvgGpuControl)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.Lock();
                        *ppv = global::MicroCom.Runtime.MicroComRuntime.GetNativePointer(__result, true);
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        protected __MicroComIAvgGpuControlVTable()
        {
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, void**, int>)&Lock); 
#else
            base.AddMethod((LockDelegate)Lock); 
#endif
        }

        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit() => global::MicroCom.Runtime.MicroComRuntime.RegisterVTable(typeof(IAvgGpuControl), new __MicroComIAvgGpuControlVTable().CreateVTable());
    }

    public unsafe partial class __MicroComIAvgGpuProxy : global::MicroCom.Runtime.MicroComProxyBase, IAvgGpu
    {
        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit()
        {
            global::MicroCom.Runtime.MicroComRuntime.Register(typeof(IAvgGpu), new Guid("5E4C1E66-1A35-47C6-A9D3-C26A42EAFD1B"), (p, owns) => new __MicroComIAvgGpuProxy(p, owns));
        }

        protected __MicroComIAvgGpuProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 0;
    }

    unsafe class __MicroComIAvgGpuVTable : global::MicroCom.Runtime.MicroComVtblBase
    {
        protected __MicroComIAvgGpuVTable()
        {
        }

        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit() => global::MicroCom.Runtime.MicroComRuntime.RegisterVTable(typeof(IAvgGpu), new __MicroComIAvgGpuVTable().CreateVTable());
    }

    public unsafe partial class __MicroComIAvgGlPlatformSurfaceRenderTargetProxy : global::MicroCom.Runtime.MicroComProxyBase, IAvgGlPlatformSurfaceRenderTarget
    {
        public IAvgGlPlatformSurfaceRenderSession BeginDraw()
        {
            int __result;
            void* __marshal_ppv = null;
            __result = (int)((delegate* unmanaged[Stdcall]<void*, void*, int>)(*PPV)[base.VTableSize + 0])(PPV, &__marshal_ppv);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("BeginDraw failed", __result);
            return global::MicroCom.Runtime.MicroComRuntime.CreateProxyOrNullFor<IAvgGlPlatformSurfaceRenderSession>(__marshal_ppv, true);
        }

        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit()
        {
            global::MicroCom.Runtime.MicroComRuntime.Register(typeof(IAvgGlPlatformSurfaceRenderTarget), new Guid("BCE2AEA0-18EF-46D8-910A-A01BC19450E4"), (p, owns) => new __MicroComIAvgGlPlatformSurfaceRenderTargetProxy(p, owns));
        }

        protected __MicroComIAvgGlPlatformSurfaceRenderTargetProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 1;
    }

    unsafe class __MicroComIAvgGlPlatformSurfaceRenderTargetVTable : global::MicroCom.Runtime.MicroComVtblBase
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int BeginDrawDelegate(void* @this, void** ppv);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int BeginDraw(void* @this, void** ppv)
        {
            IAvgGlPlatformSurfaceRenderTarget __target = null;
            try
            {
                {
                    __target = (IAvgGlPlatformSurfaceRenderTarget)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.BeginDraw();
                        *ppv = global::MicroCom.Runtime.MicroComRuntime.GetNativePointer(__result, true);
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        protected __MicroComIAvgGlPlatformSurfaceRenderTargetVTable()
        {
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, void**, int>)&BeginDraw); 
#else
            base.AddMethod((BeginDrawDelegate)BeginDraw); 
#endif
        }

        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit() => global::MicroCom.Runtime.MicroComRuntime.RegisterVTable(typeof(IAvgGlPlatformSurfaceRenderTarget), new __MicroComIAvgGlPlatformSurfaceRenderTargetVTable().CreateVTable());
    }

    public unsafe partial class __MicroComIAvgGlPlatformSurfaceRenderSessionProxy : global::MicroCom.Runtime.MicroComProxyBase, IAvgGlPlatformSurfaceRenderSession
    {
        public void GetPixelSize(AvgPixelSize* rv)
        {
            ((delegate* unmanaged[Stdcall]<void*, void*, void>)(*PPV)[base.VTableSize + 0])(PPV, rv);
        }

        public double Scaling
        {
            get
            {
                double __result;
                __result = (double)((delegate* unmanaged[Stdcall]<void*, double>)(*PPV)[base.VTableSize + 1])(PPV);
                return __result;
            }
        }

        public int SampleCount
        {
            get
            {
                int __result;
                __result = (int)((delegate* unmanaged[Stdcall]<void*, int>)(*PPV)[base.VTableSize + 2])(PPV);
                return __result;
            }
        }

        public int StencilSize
        {
            get
            {
                int __result;
                __result = (int)((delegate* unmanaged[Stdcall]<void*, int>)(*PPV)[base.VTableSize + 3])(PPV);
                return __result;
            }
        }

        public int FboId
        {
            get
            {
                int __result;
                __result = (int)((delegate* unmanaged[Stdcall]<void*, int>)(*PPV)[base.VTableSize + 4])(PPV);
                return __result;
            }
        }

        public int IsYFlipped
        {
            get
            {
                int __result;
                __result = (int)((delegate* unmanaged[Stdcall]<void*, int>)(*PPV)[base.VTableSize + 5])(PPV);
                return __result;
            }
        }

        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit()
        {
            global::MicroCom.Runtime.MicroComRuntime.Register(typeof(IAvgGlPlatformSurfaceRenderSession), new Guid("38504E1E-EE85-4336-8D01-91FCB67B7197"), (p, owns) => new __MicroComIAvgGlPlatformSurfaceRenderSessionProxy(p, owns));
        }

        protected __MicroComIAvgGlPlatformSurfaceRenderSessionProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 6;
    }

    unsafe class __MicroComIAvgGlPlatformSurfaceRenderSessionVTable : global::MicroCom.Runtime.MicroComVtblBase
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate void GetPixelSizeDelegate(void* @this, AvgPixelSize* rv);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static void GetPixelSize(void* @this, AvgPixelSize* rv)
        {
            IAvgGlPlatformSurfaceRenderSession __target = null;
            try
            {
                {
                    __target = (IAvgGlPlatformSurfaceRenderSession)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    __target.GetPixelSize(rv);
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                ;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate double GetScalingDelegate(void* @this);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static double GetScaling(void* @this)
        {
            IAvgGlPlatformSurfaceRenderSession __target = null;
            try
            {
                {
                    __target = (IAvgGlPlatformSurfaceRenderSession)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.Scaling;
                        return __result;
                    }
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return default;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetSampleCountDelegate(void* @this);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int GetSampleCount(void* @this)
        {
            IAvgGlPlatformSurfaceRenderSession __target = null;
            try
            {
                {
                    __target = (IAvgGlPlatformSurfaceRenderSession)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.SampleCount;
                        return __result;
                    }
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return default;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetStencilSizeDelegate(void* @this);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int GetStencilSize(void* @this)
        {
            IAvgGlPlatformSurfaceRenderSession __target = null;
            try
            {
                {
                    __target = (IAvgGlPlatformSurfaceRenderSession)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.StencilSize;
                        return __result;
                    }
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return default;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetFboIdDelegate(void* @this);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int GetFboId(void* @this)
        {
            IAvgGlPlatformSurfaceRenderSession __target = null;
            try
            {
                {
                    __target = (IAvgGlPlatformSurfaceRenderSession)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.FboId;
                        return __result;
                    }
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return default;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetIsYFlippedDelegate(void* @this);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int GetIsYFlipped(void* @this)
        {
            IAvgGlPlatformSurfaceRenderSession __target = null;
            try
            {
                {
                    __target = (IAvgGlPlatformSurfaceRenderSession)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.IsYFlipped;
                        return __result;
                    }
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return default;
            }
        }

        protected __MicroComIAvgGlPlatformSurfaceRenderSessionVTable()
        {
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, AvgPixelSize*, void>)&GetPixelSize); 
#else
            base.AddMethod((GetPixelSizeDelegate)GetPixelSize); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, double>)&GetScaling); 
#else
            base.AddMethod((GetScalingDelegate)GetScaling); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, int>)&GetSampleCount); 
#else
            base.AddMethod((GetSampleCountDelegate)GetSampleCount); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, int>)&GetStencilSize); 
#else
            base.AddMethod((GetStencilSizeDelegate)GetStencilSize); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, int>)&GetFboId); 
#else
            base.AddMethod((GetFboIdDelegate)GetFboId); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, int>)&GetIsYFlipped); 
#else
            base.AddMethod((GetIsYFlippedDelegate)GetIsYFlipped); 
#endif
        }

        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit() => global::MicroCom.Runtime.MicroComRuntime.RegisterVTable(typeof(IAvgGlPlatformSurfaceRenderSession), new __MicroComIAvgGlPlatformSurfaceRenderSessionVTable().CreateVTable());
    }
}
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
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public unsafe partial struct AvgRect
    {
        public double X;
        public double Y;
        public double Width;
        public double Height;
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

    public unsafe partial interface IAvgGetProcAddressDelegate : global::MicroCom.Runtime.IUnknown
    {
        IntPtr GetProcAddress(string proc);
    }

    public unsafe partial interface IAvgFactory : global::MicroCom.Runtime.IUnknown
    {
        int Version { get; }

        IAvgGpu CreateGlGpu(int gles, IAvgGetProcAddressDelegate glGetProcAddress);
        IAvgRenderTarget CreateGlGpuRenderTarget(IAvgGpu gpu, IAvgGlPlatformSurfaceRenderTarget gl);
    }

    public unsafe partial interface IAvgDrawingContext : global::MicroCom.Runtime.IUnknown
    {
        void SetTransform(AvgMatrix3x2* matrix);
        void Clear(uint color);
        void FillRect(AvgRect rect, uint color);
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

        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit()
        {
            global::MicroCom.Runtime.MicroComRuntime.Register(typeof(IAvgFactory), new Guid("52434e9c-5438-4ac9-9823-9f5a3fe90d53"), (p, owns) => new __MicroComIAvgFactoryProxy(p, owns));
        }

        protected __MicroComIAvgFactoryProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 3;
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
        }

        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit() => global::MicroCom.Runtime.MicroComRuntime.RegisterVTable(typeof(IAvgFactory), new __MicroComIAvgFactoryVTable().CreateVTable());
    }

    public unsafe partial class __MicroComIAvgDrawingContextProxy : global::MicroCom.Runtime.MicroComProxyBase, IAvgDrawingContext
    {
        public void SetTransform(AvgMatrix3x2* matrix)
        {
            ((delegate* unmanaged[Stdcall]<void*, void*, void>)(*PPV)[base.VTableSize + 0])(PPV, matrix);
        }

        public void Clear(uint color)
        {
            ((delegate* unmanaged[Stdcall]<void*, uint, void>)(*PPV)[base.VTableSize + 1])(PPV, color);
        }

        public void FillRect(AvgRect rect, uint color)
        {
            ((delegate* unmanaged[Stdcall]<void*, AvgRect, uint, void>)(*PPV)[base.VTableSize + 2])(PPV, rect, color);
        }

        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit()
        {
            global::MicroCom.Runtime.MicroComRuntime.Register(typeof(IAvgDrawingContext), new Guid("309466F0-B5CA-4ABA-8469-2C902FE5D8F3"), (p, owns) => new __MicroComIAvgDrawingContextProxy(p, owns));
        }

        protected __MicroComIAvgDrawingContextProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 3;
    }

    unsafe class __MicroComIAvgDrawingContextVTable : global::MicroCom.Runtime.MicroComVtblBase
    {
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
        delegate void FillRectDelegate(void* @this, AvgRect rect, uint color);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static void FillRect(void* @this, AvgRect rect, uint color)
        {
            IAvgDrawingContext __target = null;
            try
            {
                {
                    __target = (IAvgDrawingContext)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    __target.FillRect(rect, color);
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
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, AvgRect, uint, void>)&FillRect); 
#else
            base.AddMethod((FillRectDelegate)FillRect); 
#endif
        }

        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit() => global::MicroCom.Runtime.MicroComRuntime.RegisterVTable(typeof(IAvgDrawingContext), new __MicroComIAvgDrawingContextVTable().CreateVTable());
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
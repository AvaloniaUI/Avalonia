using System;
using Avalonia.OpenGL;

namespace Avalonia.Win32.OpenGl
{
    public unsafe class WglInterface : GlInterfaceBase
    {
        public WglInterface(Func<string, IntPtr> getProcAddress) : base(getProcAddress)
        {
        }
        
        public delegate bool WglChoosePixelFormatARBDelegate(IntPtr hdc, int[] piAttribIList, float[] pfAttribFList,
            int nMaxFormats, int[] piFormats, out int nNumFormats);
        
        [GlEntryPoint("wglChoosePixelFormatARB")]
        public WglChoosePixelFormatARBDelegate ChoosePixelFormatARB { get; }


        public delegate IntPtr WglCreateContextAttribsARBDelegate(IntPtr hDC, IntPtr hShareContext, int[] attribList);

        [GlEntryPoint("wglCreateContextAttribsARB")]
        public WglCreateContextAttribsARBDelegate CreateContextAttribsArb { get; }
        
        public delegate void GlDebugMessageCallbackDelegate(IntPtr callback, IntPtr userParam);

        [GlEntryPoint("glDebugMessageCallback")]
        public GlDebugMessageCallbackDelegate DebugMessageCallback { get; }

        public delegate void DebugCallbackDelegate(int source, int type, int id, int severity, int len, IntPtr message,
            IntPtr userParam);

        public delegate IntPtr WglDXOpenDeviceNVDelegate(IntPtr device);
        
        [GlOptionalEntryPoint()]
        [GlEntryPoint("wglDXOpenDeviceNV")]
        public WglDXOpenDeviceNVDelegate DXOpenDeviceNV { get; }

        public delegate int WglDXCloseDeviceNVDelegate(IntPtr handle);
        [GlOptionalEntryPoint()]
        [GlEntryPoint("wglDXCloseDeviceNV")]
        public WglDXCloseDeviceNVDelegate DXCloseDeviceNV { get; }

        public delegate IntPtr WglDXRegisterObjectNVDelegate(IntPtr hDevice, IntPtr dxObject,
            int name, int type, int access);
        [GlOptionalEntryPoint()]
        [GlEntryPoint("wglDXRegisterObjectNV")]
        public WglDXRegisterObjectNVDelegate DXRegisterObjectNV { get; }

        public delegate int WglDXUnregisterObjectNVDelegate(IntPtr hDevice, IntPtr hObject);
        [GlOptionalEntryPoint()]
        [GlEntryPoint("wglDXUnregisterObjectNV")]
        public WglDXUnregisterObjectNVDelegate DXUnregisterObjectNV { get; }

        public delegate int WglDXLockObjectsNVDelegate(IntPtr hDevice, int count, IntPtr[] objects);
        [GlOptionalEntryPoint]
        [GlEntryPoint("wglDXLockObjectsNV")]
        public WglDXLockObjectsNVDelegate DXLockObjectsNV { get; }
        
        [GlOptionalEntryPoint]
        [GlEntryPoint("wglDXUnlockObjectsNV")]
        public WglDXLockObjectsNVDelegate DXUnlockObjectsNV { get; }
        
        public delegate int DXSetResourceShareHandleNVDelegate(IntPtr dxObject, IntPtr shareHandle);
        [GlOptionalEntryPoint]
        [GlEntryPoint("wglDXSetResourceShareHandleNV")]
        public DXSetResourceShareHandleNVDelegate DXSetResourceShareHandleNV { get; }
    }
}

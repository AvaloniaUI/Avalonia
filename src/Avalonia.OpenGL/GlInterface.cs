using System;
using System.Runtime.InteropServices;

namespace Avalonia.OpenGL
{
    public delegate IntPtr GlGetProcAddressDelegate(string procName);
    
    public class GlInterface : GlInterfaceBase
    {
        private readonly Func<string, bool, IntPtr> _getProcAddress;

        public GlInterface(Func<string, bool, IntPtr> getProcAddress) : base(getProcAddress)
        {
            _getProcAddress = getProcAddress;
        }

        public IntPtr GetProcAddress(string proc) => _getProcAddress(proc, true);

        public T GetProcAddress<T>(string proc) => Marshal.GetDelegateForFunctionPointer<T>(GetProcAddress(proc));

        // ReSharper disable UnassignedGetOnlyAutoProperty
        
        public delegate void GlClearStencil(int s);
        [EntryPoint("glClearStencil")]
        public GlClearStencil ClearStencil { get; }

        public delegate void GlClearColor(int r, int g, int b, int a);
        [EntryPoint("glClearColor")]
        public GlClearColor ClearColor { get; }

        public delegate void GlClear(int bits);
        [EntryPoint("glClear")]
        public GlClear Clear { get; }

        public delegate void GlViewport(int x, int y, int width, int height);
        [EntryPoint("glViewport")]
        public GlViewport Viewport { get; }
        
        [EntryPoint("glFlush")]
        public Action Flush { get; }

        public delegate void GlGetIntegerv(int name, out int rv);
        [EntryPoint("glGetIntegerv")]
        public GlGetIntegerv GetIntegerv { get; }

        // ReSharper restore UnassignedGetOnlyAutoProperty
    }
}

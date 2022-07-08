using System;
using Avalonia.Native.Interop;
using Avalonia.OpenGL;
using MicroCom.Runtime;

namespace Avalonia.NativeGraphics.Backend
{
    internal class GlGpuControl : CallbackBase, IAvgGpuControl, IAvgGetProcAddressDelegate
    {
        private readonly IGlContext _context;

        public GlGpuControl(IGlContext context)
        {
            _context = context;
        }

        public IntPtr GetProcAddress(string proc) => _context.GlInterface.GetProcAddress(proc);

        class LockedContext : CallbackBase
        {
            private readonly IDisposable _wrapped;

            public LockedContext(IDisposable wrapped)
            {
                _wrapped = wrapped;
            }
            
            public override void OnUnreferencedFromNative()
            {
                _wrapped.Dispose();
            }
        }
        
        public IUnknown Lock() => new LockedContext(_context.MakeCurrent());
    }
}
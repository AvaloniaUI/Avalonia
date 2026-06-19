using System;
using Avalonia.Native.Interop;
using Avalonia.Platform;

namespace Avalonia.Native;

class GpuHandleWrapFeature : IExternalObjectsHandleWrapRenderInterfaceContextFeature
{
    private readonly IAvnNativeObjectsMemoryManagement _helper;

    public GpuHandleWrapFeature(IAvaloniaNativeFactory factory)
    {
        _helper = factory.CreateMemoryManagementHelper();
    }
    public IExternalObjectsWrappedGpuHandle? WrapImageHandleOnAnyThread(IPlatformHandle handle, PlatformGraphicsExternalImageProperties properties)
    {
        if (handle.HandleDescriptor == KnownPlatformGraphicsExternalImageHandleTypes.IOSurfaceRef)
        {
            _helper.RetainCFObject(handle.Handle);
            return new CFObjectWrapper(_helper, handle.Handle, handle.HandleDescriptor);
        }

        return null;
    }

    public IExternalObjectsWrappedGpuHandle? WrapSemaphoreHandleOnAnyThread(IPlatformHandle handle)
    {
        if (handle.HandleDescriptor == KnownPlatformGraphicsExternalSemaphoreHandleTypes.MetalSharedEvent)
        {
            _helper.RetainNSObject(handle.Handle);
            return new NSObjectWrapper(_helper, handle.Handle, handle.HandleDescriptor);
        }

        return null;
    }

    class NSObjectWrapper(IAvnNativeObjectsMemoryManagement helper, IntPtr handle, string descriptor) : IExternalObjectsWrappedGpuHandle
    {
        public void Dispose() => helper.ReleaseNSObject(handle);

        public IntPtr Handle => handle;
        public string HandleDescriptor => descriptor;
    }
    
    class CFObjectWrapper(IAvnNativeObjectsMemoryManagement helper, IntPtr handle, string descriptor) : IExternalObjectsWrappedGpuHandle
    {
        public void Dispose()
        {
            helper.ReleaseCFObject(handle);
        }

        public IntPtr Handle => handle;
        public string HandleDescriptor => descriptor;
    }
}

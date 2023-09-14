using System;
using System.Runtime.InteropServices;
using Avalonia.Platform;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using SilkNetDemo;

namespace GpuInterop.VulkanDemo;

class VulkanSemaphorePair : IDisposable
{
    private readonly VulkanContext _resources;

    public unsafe VulkanSemaphorePair(VulkanContext resources, bool exportable)
    {
        _resources = resources;

        var semaphoreExportInfo = new ExportSemaphoreCreateInfo
        {
            SType = StructureType.ExportSemaphoreCreateInfo,
            HandleTypes = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                ExternalSemaphoreHandleTypeFlags.OpaqueWin32Bit :
                ExternalSemaphoreHandleTypeFlags.OpaqueFDBit
        };

        var semaphoreCreateInfo = new SemaphoreCreateInfo
        {
            SType = StructureType.SemaphoreCreateInfo,
            PNext = exportable ? &semaphoreExportInfo : null
        };

        resources.Api.CreateSemaphore(resources.Device, semaphoreCreateInfo, null, out var semaphore).ThrowOnError();
        ImageAvailableSemaphore = semaphore;

        resources.Api.CreateSemaphore(resources.Device, semaphoreCreateInfo, null, out semaphore).ThrowOnError();
        RenderFinishedSemaphore = semaphore;
    }

    public int ExportFd(bool renderFinished)
    {
        if (!_resources.Api.TryGetDeviceExtension<KhrExternalSemaphoreFd>(_resources.Instance, _resources.Device,
                out var ext))
            throw new InvalidOperationException();
        var info = new SemaphoreGetFdInfoKHR()
        {
            SType = StructureType.SemaphoreGetFDInfoKhr,
            Semaphore = renderFinished ? RenderFinishedSemaphore : ImageAvailableSemaphore,
            HandleType = ExternalSemaphoreHandleTypeFlags.OpaqueFDBit
        };
        ext.GetSemaphoreF(_resources.Device, info, out var fd).ThrowOnError();
        return fd;
    }
    
    public IntPtr ExportWin32(bool renderFinished)
    {
        if (!_resources.Api.TryGetDeviceExtension<KhrExternalSemaphoreWin32>(_resources.Instance, _resources.Device,
                out var ext))
            throw new InvalidOperationException();
        var info = new SemaphoreGetWin32HandleInfoKHR()
        {
            SType = StructureType.SemaphoreGetWin32HandleInfoKhr,
            Semaphore = renderFinished ? RenderFinishedSemaphore : ImageAvailableSemaphore,
            HandleType = ExternalSemaphoreHandleTypeFlags.OpaqueWin32Bit
        };
        ext.GetSemaphoreWin32Handle(_resources.Device, info, out var fd).ThrowOnError();
        return fd;
    }

    public IPlatformHandle Export(bool renderFinished)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new PlatformHandle(ExportWin32(renderFinished),
                KnownPlatformGraphicsExternalSemaphoreHandleTypes.VulkanOpaqueNtHandle);
        return new PlatformHandle(new IntPtr(ExportFd(renderFinished)),
            KnownPlatformGraphicsExternalSemaphoreHandleTypes.VulkanOpaquePosixFileDescriptor);
    }

    internal Semaphore ImageAvailableSemaphore { get; }
    internal Semaphore RenderFinishedSemaphore { get; }

    public unsafe void Dispose()
    {
        _resources.Api.DestroySemaphore(_resources.Device, ImageAvailableSemaphore, null);
        _resources.Api.DestroySemaphore(_resources.Device, RenderFinishedSemaphore, null);
    }
}

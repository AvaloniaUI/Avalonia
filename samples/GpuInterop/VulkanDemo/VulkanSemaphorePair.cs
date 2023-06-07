using System;
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
            HandleTypes = ExternalSemaphoreHandleTypeFlags.OpaqueFDBit
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

    internal Semaphore ImageAvailableSemaphore { get; }
    internal Semaphore RenderFinishedSemaphore { get; }

    public unsafe void Dispose()
    {
        _resources.Api.DestroySemaphore(_resources.Device, ImageAvailableSemaphore, null);
        _resources.Api.DestroySemaphore(_resources.Device, RenderFinishedSemaphore, null);
    }
}
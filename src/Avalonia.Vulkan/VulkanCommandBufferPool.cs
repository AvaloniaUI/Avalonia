using System;
using System.Collections.Generic;
using Silk.NET.Vulkan;

namespace Avalonia.Vulkan
{
    public class VulkanCommandBufferPool : IDisposable
    {
        private readonly VulkanDevice _device;
        private readonly CommandPool _commandPool;

        private readonly List<VulkanCommandBuffer> _usedCommandBuffers = new();

        public unsafe VulkanCommandBufferPool(VulkanDevice device, VulkanPhysicalDevice physicalDevice)
        {
            _device = device;

            var commandPoolCreateInfo = new CommandPoolCreateInfo
            {
                SType = StructureType.CommandPoolCreateInfo,
                Flags = CommandPoolCreateFlags.CommandPoolCreateResetCommandBufferBit,
                QueueFamilyIndex = physicalDevice.QueueFamilyIndex
            };

            device.Api.CreateCommandPool(_device.InternalHandle, commandPoolCreateInfo, null, out _commandPool)
                .ThrowOnError();
        }

        public unsafe void Dispose()
        {
            FreeUsedCommandBuffers();
            _device.Api.DestroyCommandPool(_device.InternalHandle, _commandPool, null);
        }

        private CommandBuffer AllocateCommandBuffer()
        {
            var commandBufferAllocateInfo = new CommandBufferAllocateInfo
            {
                SType = StructureType.CommandBufferAllocateInfo,
                CommandPool = _commandPool,
                CommandBufferCount = 1,
                Level = CommandBufferLevel.Primary
            };

            _device.Api.AllocateCommandBuffers(_device.InternalHandle, commandBufferAllocateInfo, out var commandBuffer);

            return commandBuffer;
        }

        public VulkanCommandBuffer CreateCommandBuffer()
        {
            return new(_device, this);
        }

        public void FreeUsedCommandBuffers()
        {
            lock (_usedCommandBuffers)
            {
                foreach (var usedCommandBuffer in _usedCommandBuffers) usedCommandBuffer.Dispose();

                _usedCommandBuffers.Clear();
            }
        }

        private void DisposeCommandBuffer(VulkanCommandBuffer commandBuffer)
        {
            lock (_usedCommandBuffers)
            {
                _usedCommandBuffers.Add(commandBuffer);
            }
        }

        public class VulkanCommandBuffer : IDisposable
        {
            private readonly VulkanCommandBufferPool _commandBufferPool;
            private readonly VulkanDevice _device;
            private readonly Fence _fence;
            private bool _hasEnded;
            private bool _hasStarted;

            public IntPtr Handle => InternalHandle.Handle;

            internal CommandBuffer InternalHandle { get; }

            internal unsafe VulkanCommandBuffer(VulkanDevice device, VulkanCommandBufferPool commandBufferPool)
            {
                _device = device;
                _commandBufferPool = commandBufferPool;

                InternalHandle = _commandBufferPool.AllocateCommandBuffer();

                var fenceCreateInfo = new FenceCreateInfo()
                {
                    SType = StructureType.FenceCreateInfo,
                    Flags = FenceCreateFlags.FenceCreateSignaledBit
                };

                device.Api.CreateFence(device.InternalHandle, fenceCreateInfo, null, out _fence);
            }

            public unsafe void Dispose()
            {
                _device.Api.WaitForFences(_device.InternalHandle, 1, _fence, true, ulong.MaxValue);
                _device.Api.FreeCommandBuffers(_device.InternalHandle, _commandBufferPool._commandPool, 1, InternalHandle);
                _device.Api.DestroyFence(_device.InternalHandle, _fence, null);
            }

            public void BeginRecording()
            {
                if (!_hasStarted)
                {
                    _hasStarted = true;

                    var beginInfo = new CommandBufferBeginInfo
                    {
                        SType = StructureType.CommandBufferBeginInfo,
                        Flags = CommandBufferUsageFlags.CommandBufferUsageOneTimeSubmitBit
                    };

                    _device.Api.BeginCommandBuffer(InternalHandle, beginInfo);
                }
            }

            public void EndRecording()
            {
                if (_hasStarted && !_hasEnded)
                {
                    _hasEnded = true;

                    _device.Api.EndCommandBuffer(InternalHandle);
                }
            }

            public void Submit()
            {
                Submit(null, null, null, _fence);
            }

            public unsafe void Submit(
                ReadOnlySpan<Semaphore> waitSemaphores,
                ReadOnlySpan<PipelineStageFlags> waitDstStageMask,
                ReadOnlySpan<Semaphore> signalSemaphores,
                Fence? fence = null)
            {
                EndRecording();

                if (!fence.HasValue)
                    fence = _fence;

                fixed (Semaphore* pWaitSemaphores = waitSemaphores, pSignalSemaphores = signalSemaphores)
                {
                    fixed (PipelineStageFlags* pWaitDstStageMask = waitDstStageMask)
                    {
                        var commandBuffer = InternalHandle;
                        var submitInfo = new SubmitInfo
                        {
                            SType = StructureType.SubmitInfo,
                            WaitSemaphoreCount = waitSemaphores != null ? (uint)waitSemaphores.Length : 0,
                            PWaitSemaphores = pWaitSemaphores,
                            PWaitDstStageMask = pWaitDstStageMask,
                            CommandBufferCount = 1,
                            PCommandBuffers = &commandBuffer,
                            SignalSemaphoreCount = signalSemaphores != null ? (uint)signalSemaphores.Length : 0,
                            PSignalSemaphores = pSignalSemaphores,
                        };

                        _device.Api.ResetFences(_device.InternalHandle, 1, fence.Value);

                        _device.Submit(submitInfo, fence.Value);
                    }
                }

                _commandBufferPool.DisposeCommandBuffer(this);
            }
        }
    }
}

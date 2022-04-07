using Silk.NET.Vulkan;

namespace Avalonia.Vulkan
{
    public enum PresentMode
    {
        Immediate = PresentModeKHR.PresentModeImmediateKhr,
        Fifo = PresentModeKHR.PresentModeFifoKhr,
        Mailbox = PresentModeKHR.PresentModeMailboxKhr,
        FifoRelaxed = PresentModeKHR.PresentModeFifoRelaxedKhr
    }
}

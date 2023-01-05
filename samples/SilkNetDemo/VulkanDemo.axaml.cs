using Avalonia.Controls;

namespace SilkNetDemo;

public class VulkanDemo : UserControl
{
    public void Dispose()
    {
        var vulkanDemoControl = this.Get<VulkanDemoControl>("Vulkan");
        vulkanDemoControl?.DestroyVulkanResources();
    }
}

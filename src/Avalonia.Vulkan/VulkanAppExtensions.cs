using Avalonia.Controls;
using Avalonia.Skia;
using Avalonia.Vulkan.Skia;

namespace Avalonia.Vulkan
{
    public static class VulkanAppExtensions
    {
        public static T UseVulkan<T>(this T builder) where T : AppBuilderBase<T>, new()
        {
            return builder.UseSkia()
                .With(new SkiaOptions()
                {
                    CustomGpuFactory = VulkanSkiaGpu.CreateGpu
                });
        }
    }
}

using System;
using System.Runtime.InteropServices;
using Avalonia.Logging;
using Avalonia.Vulkan.UnmanagedInterop;

namespace Avalonia.Vulkan.Interop;

unsafe static class VulkanDebugLogger
{
    private static VkDebugUtilsMessengerCallbackEXTDelegate s_Delegate = WriteLogEvent;
    public static IntPtr CallbackPtr { get;  } = Marshal.GetFunctionPointerForDelegate(s_Delegate);

    private static uint WriteLogEvent(VkDebugUtilsMessageSeverityFlagsEXT messageSeverity,
        VkDebugUtilsMessageTypeFlagsEXT messagetypes,
        VkDebugUtilsMessengerCallbackDataEXT* pCallbackData,
        void* puserdata)
    {
        var level = messageSeverity switch
        {
            VkDebugUtilsMessageSeverityFlagsEXT.VK_DEBUG_UTILS_MESSAGE_SEVERITY_ERROR_BIT_EXT => LogEventLevel.Error,
            VkDebugUtilsMessageSeverityFlagsEXT.VK_DEBUG_UTILS_MESSAGE_SEVERITY_WARNING_BIT_EXT =>
                LogEventLevel.Warning,
            VkDebugUtilsMessageSeverityFlagsEXT.VK_DEBUG_UTILS_MESSAGE_SEVERITY_VERBOSE_BIT_EXT =>
                LogEventLevel.Verbose,
            VkDebugUtilsMessageSeverityFlagsEXT.VK_DEBUG_UTILS_MESSAGE_SEVERITY_INFO_BIT_EXT => LogEventLevel
                .Information,
            _ => LogEventLevel.Information
        };
        if (Logger.TryGet(level, "Vulkan", out var logger))
        {
            var msg  =Marshal.PtrToStringAnsi((nint)pCallbackData->pMessage);
            logger.Log("Vulkan", "Vulkan: {0}", msg);
        }
        
        return 0;
    }
}
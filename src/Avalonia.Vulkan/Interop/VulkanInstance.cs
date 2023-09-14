using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Reactive;
using System.Runtime.InteropServices;
using Avalonia.Platform.Interop;
using Avalonia.Vulkan.Interop;
using Avalonia.Vulkan.UnmanagedInterop;

namespace Avalonia.Vulkan;
internal class VulkanInstance : IVulkanInstance
{
    private readonly VkGetInstanceProcAddressDelegate _getProcAddress;
    private readonly VulkanInstanceApi _api;

    public VulkanInstance(VkInstance handle, VkGetInstanceProcAddressDelegate getProcAddress, string[] enabledExtensions)
    {
        Handle = handle;
        _getProcAddress = getProcAddress;
        _api = new VulkanInstanceApi(this);
        EnabledExtensions = enabledExtensions;
    }

    internal static unsafe IVulkanInstance Create(
        VulkanInstanceCreationOptions options,
        VulkanPlatformSpecificOptions platformOptions)
    {
        var getProcAddress = options.CustomGetProcAddressDelegate ??
                             platformOptions.GetProcAddressDelegate ??
                             throw new ArgumentException("No VkGetInstanceProcAddr provided");
        
        
        using var name = new Utf8Buffer(options.ApplicationName ?? "AvaloniaUI");
        using var engineName = new Utf8Buffer("AvaloniaUI");

        var gapi = new UnmanagedInterop.VulkanGlobalApi(getProcAddress);

        var supportedExtensions = GetSupportedExtensions(gapi);
        var appInfo = new VkApplicationInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_APPLICATION_INFO,
            apiVersion = VulkanHelpers.MakeVersion(options.VulkanVersion),
            applicationVersion = VulkanHelpers.MakeVersion(1, 0, 0),
            engineVersion = VulkanHelpers.MakeVersion(1, 0, 0),
            pApplicationName = name,
            pEngineName = engineName,
        };

        var enabledExtensions = new HashSet<string>(options.InstanceExtensions
            .Concat(platformOptions.RequiredInstanceExtensions)
            .Append("VK_KHR_surface"));
        
        var enabledLayers = options.EnabledLayers.ToList();

        void AddExtensionsIfSupported(params string[] names)
        {
            if(names.All(n=>supportedExtensions.Contains(n)))
                foreach (var n in names)
                    enabledExtensions.Add(n);
        }
        
        if (options.UseDebug)
        {
            AddExtensionsIfSupported("VK_EXT_debug_utils");
            if (IsLayerAvailable(gapi, "VK_LAYER_KHRONOS_validation"))
                enabledLayers.Add("VK_LAYER_KHRONOS_validation");
        }

        AddExtensionsIfSupported(VulkanExternalObjectsFeature.RequiredInstanceExtensions);
        
        using var enabledExtensionBuffers = new Utf8BufferArray(
            enabledExtensions
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct());
        
        using var enabledLayersBuffers = new Utf8BufferArray(enabledLayers
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct());

        
        var createInfo = new VkInstanceCreateInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_INSTANCE_CREATE_INFO,
            pApplicationInfo = &appInfo,
            ppEnabledLayerNames = enabledLayersBuffers,
            enabledLayerCount = enabledLayersBuffers.UCount,
            ppEnabledExtensionNames = enabledExtensionBuffers,
            enabledExtensionCount = enabledExtensionBuffers.UCount
        };

        gapi.vkCreateInstance(ref createInfo, IntPtr.Zero, out var pInstance)
            .ThrowOnError(nameof(gapi.vkCreateInstance));
        
        var instance = new VulkanInstance(pInstance, getProcAddress, enabledExtensions.ToArray());
        var instanceApi = new VulkanInstanceApi(instance);

        if (options.UseDebug)
        {
            var debugCreateInfo = new VkDebugUtilsMessengerCreateInfoEXT
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_DEBUG_UTILS_MESSENGER_CREATE_INFO_EXT,
                messageSeverity = VkDebugUtilsMessageSeverityFlagsEXT.VK_DEBUG_UTILS_MESSAGE_SEVERITY_VERBOSE_BIT_EXT
                    |VkDebugUtilsMessageSeverityFlagsEXT.VK_DEBUG_UTILS_MESSAGE_SEVERITY_WARNING_BIT_EXT
                    |VkDebugUtilsMessageSeverityFlagsEXT.VK_DEBUG_UTILS_MESSAGE_SEVERITY_INFO_BIT_EXT
                    |VkDebugUtilsMessageSeverityFlagsEXT.VK_DEBUG_UTILS_MESSAGE_SEVERITY_ERROR_BIT_EXT,
                messageType = VkDebugUtilsMessageTypeFlagsEXT.VK_DEBUG_UTILS_MESSAGE_TYPE_GENERAL_BIT_EXT
                    |VkDebugUtilsMessageTypeFlagsEXT.VK_DEBUG_UTILS_MESSAGE_TYPE_VALIDATION_BIT_EXT
                    |VkDebugUtilsMessageTypeFlagsEXT.VK_DEBUG_UTILS_MESSAGE_TYPE_PERFORMANCE_BIT_EXT,
                pfnUserCallback = VulkanDebugLogger.CallbackPtr
            };

            instanceApi.CreateDebugUtilsMessengerEXT(pInstance, ref debugCreateInfo, IntPtr.Zero, out var messenger);
        }
        
        return instance;
    }

    private static unsafe bool IsLayerAvailable(VulkanGlobalApi api, string layerName)
    {
        uint layerPropertiesCount = 0;

        api.EnumerateInstanceLayerProperties(ref layerPropertiesCount, null)
            .ThrowOnError("vkEnumerateInstanceLayerProperties");

        var layerProperties = new VkLayerProperties[layerPropertiesCount];

        fixed (VkLayerProperties* pLayerProperties = layerProperties)
        {
            api.EnumerateInstanceLayerProperties(ref layerPropertiesCount, pLayerProperties)
                .ThrowOnError("vkEnumerateInstanceLayerProperties");

            for (var i = 0; i < layerPropertiesCount; i++)
            {
                var currentLayerName = Marshal.PtrToStringAnsi((IntPtr)pLayerProperties[i].layerName);
                if (currentLayerName == layerName) return true;
            }
        }

        return false;
    }

    private static unsafe HashSet<string> GetSupportedExtensions(VulkanGlobalApi api)
    {
        var supportedExtensions = new HashSet<string>();
        uint supportedExtensionCount = 0;
        api.vkEnumerateInstanceExtensionProperties(IntPtr.Zero, &supportedExtensionCount, null);
        if (supportedExtensionCount > 0)
        {
            var ptr = (VkExtensionProperties*)Marshal.AllocHGlobal(Unsafe.SizeOf<VkExtensionProperties>() *
                                                                   (int)supportedExtensionCount);
            try
            {
                api.vkEnumerateInstanceExtensionProperties(IntPtr.Zero, &supportedExtensionCount, ptr)
                    .ThrowOnError("vkEnumerateInstanceExtensionProperties");
                for (var c = 0; c < supportedExtensionCount; c++)
                    supportedExtensions.Add(Marshal.PtrToStringAnsi((IntPtr)ptr[c].extensionName) ?? "");
            }
            finally
            {
                Marshal.FreeHGlobal((IntPtr)ptr);
            }
        }

        return supportedExtensions;
    }

    public VkInstance Handle { get; }
    IntPtr IVulkanInstance.Handle => Handle.Handle;
    
    public IntPtr GetInstanceProcAddress(IntPtr instance, string name) => _getProcAddress(instance, name);
    public IntPtr GetDeviceProcAddress(IntPtr device, string name)
    {
        using var buf = new Utf8Buffer(name);
        return _api.GetDeviceProcAddr(new(device), buf);
    }

    public IEnumerable<string> EnabledExtensions { get; }
}
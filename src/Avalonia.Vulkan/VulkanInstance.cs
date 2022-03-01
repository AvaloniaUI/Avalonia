using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Logging;
using Silk.NET.Core;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;

namespace Avalonia.Vulkan
{
    public class VulkanInstance : IDisposable
    {
        private readonly DebugUtilsMessengerEXT _messenger;
        private const string EngineName = "Avalonia Vulkan";

        private VulkanInstance(Instance apiHandle, Vk api, DebugUtilsMessengerEXT messenger)
        {
            _messenger = messenger;
            InternalHandle = apiHandle;
            Api = api;
        }

        public IntPtr Handle => InternalHandle.Handle;

        internal Instance InternalHandle { get; }
        public Vk Api { get; }

        internal static IList<string> RequiredInstanceExtensions
        {
            get
            {
                var extensions = new List<string> { "VK_KHR_surface" };
#if NET6_0_OR_GREATER
                if (OperatingSystem.IsAndroid())
                    extensions.Add("VK_KHR_android_surface");
                else
#endif
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    extensions.Add("VK_KHR_xlib_surface");

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    extensions.Add("VK_KHR_win32_surface");

                return extensions;
            }
        }

        public unsafe void Dispose()
        {
            if (_messenger.Handle != 0)
            {
                if (Api.TryGetInstanceExtension(InternalHandle, out ExtDebugUtils debugUtils))
                {
                    debugUtils.DestroyDebugUtilsMessenger(InternalHandle, _messenger, null);
                }
            }
            Api.DestroyInstance(InternalHandle, null);
            Api?.Dispose();
        }

        internal static unsafe VulkanInstance Create(VulkanOptions options)
        {
            var api = Vk.GetApi();
            var applicationName = Marshal.StringToHGlobalAnsi(options.ApplicationName);
            var engineName = Marshal.StringToHGlobalAnsi(EngineName);
            var enabledExtensions = new List<string>(options.InstanceExtensions);

            enabledExtensions.AddRange(RequiredInstanceExtensions);

            var applicationInfo = new ApplicationInfo
            {
                PApplicationName = (byte*)applicationName,
                ApiVersion = new Version32((uint)options.VulkanVersion.Major, (uint)options.VulkanVersion.Minor,
                    (uint)options.VulkanVersion.Build),
                PEngineName = (byte*)engineName,
                EngineVersion = new Version32(1, 0, 0),
                ApplicationVersion = new Version32(1, 0, 0)
            };

            var enabledLayers = new HashSet<string>();

            if (options.UseDebug)
            {
                enabledExtensions.Add(ExtDebugUtils.ExtensionName);
                if (IsLayerAvailable(api, "VK_LAYER_KHRONOS_validation"))
                    enabledLayers.Add("VK_LAYER_KHRONOS_validation");
            }

            foreach (var layer in options.EnabledLayers)
                enabledLayers.Add(layer);

            var ppEnabledExtensions = stackalloc IntPtr[enabledExtensions.Count];
            var ppEnabledLayers = stackalloc IntPtr[enabledLayers.Count];

            for (var i = 0; i < enabledExtensions.Count; i++)
                ppEnabledExtensions[i] = Marshal.StringToHGlobalAnsi(enabledExtensions[i]);

            var layers = enabledLayers.ToList();

            for (var i = 0; i < enabledLayers.Count; i++)
                ppEnabledLayers[i] = Marshal.StringToHGlobalAnsi(layers[i]);

            var instanceCreateInfo = new InstanceCreateInfo
            {
                SType = StructureType.InstanceCreateInfo,
                PApplicationInfo = &applicationInfo,
                PpEnabledExtensionNames = (byte**)ppEnabledExtensions,
                PpEnabledLayerNames = (byte**)ppEnabledLayers,
                EnabledExtensionCount = (uint)enabledExtensions.Count,
                EnabledLayerCount = (uint)enabledLayers.Count
            };

            api.CreateInstance(in instanceCreateInfo, null, out var instance).ThrowOnError();
            
            Marshal.FreeHGlobal(applicationName);
            Marshal.FreeHGlobal(engineName);

            for (var i = 0; i < enabledExtensions.Count; i++) Marshal.FreeHGlobal(ppEnabledExtensions[i]);

            for (var i = 0; i < enabledLayers.Count; i++) Marshal.FreeHGlobal(ppEnabledLayers[i]);

            DebugUtilsMessengerEXT messenger = default;

            if (options.UseDebug && api.TryGetInstanceExtension(instance, out ExtDebugUtils debugUtils))
            {
                var createInfo = new DebugUtilsMessengerCreateInfoEXT
                {
                    SType = StructureType.DebugUtilsMessengerCreateInfoExt,
                    MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityVerboseBitExt | DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityWarningBitExt | DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityErrorBitExt,
                    MessageType = DebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypeGeneralBitExt | DebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypeValidationBitExt | DebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypePerformanceBitExt,
                    PfnUserCallback = new PfnDebugUtilsMessengerCallbackEXT(LogCallback),
                };

                debugUtils.CreateDebugUtilsMessenger(instance, createInfo, null, out messenger);
            }

            return new VulkanInstance(instance, api, messenger);
        }

        private static unsafe uint LogCallback(DebugUtilsMessageSeverityFlagsEXT messageSeverity, DebugUtilsMessageTypeFlagsEXT messageTypes, DebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData)
        {
            var message = Marshal.PtrToStringAnsi((nint)pCallbackData->PMessage);
            switch (messageSeverity)
            {
                case DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityWarningBitExt:
                    Logger.TryGet(LogEventLevel.Warning, "Vulkan")?.Log(null, message);
                    break;
                case DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityInfoBitExt:
                    Logger.TryGet(LogEventLevel.Information, "Vulkan")?.Log(null, message);
                    break;
                case DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityErrorBitExt:
                    Logger.TryGet(LogEventLevel.Error, "Vulkan")?.Log(null, message);
                    break;
                case DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityVerboseBitExt:
                    Logger.TryGet(LogEventLevel.Verbose, "Vulkan")?.Log(null, message);
                    break;
            }

            return Vk.False;
        }

        private static unsafe bool IsLayerAvailable(Vk api, string layerName)
        {
            uint layerPropertiesCount;

            api.EnumerateInstanceLayerProperties(&layerPropertiesCount, null).ThrowOnError();

            var layerProperties = new LayerProperties[layerPropertiesCount];

            fixed (LayerProperties* pLayerProperties = layerProperties)
            {
                api.EnumerateInstanceLayerProperties(&layerPropertiesCount, layerProperties).ThrowOnError();

                for (var i = 0; i < layerPropertiesCount; i++)
                {
                    var currentLayerName = Marshal.PtrToStringAnsi((IntPtr)pLayerProperties[i].LayerName);

                    if (currentLayerName == layerName) return true;
                }
            }

            return false;
        }
    }
}

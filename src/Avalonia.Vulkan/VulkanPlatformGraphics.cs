using System;
using Avalonia.Logging;
using Avalonia.Platform;

namespace Avalonia.Vulkan;

public class VulkanPlatformGraphics : IPlatformGraphics
{
    private readonly IVulkanDeviceFactory _factory;
    private IVulkanPlatformGraphicsContext? _currentSharedContext;
    private readonly VulkanPlatformSpecificOptions _platformOptions;
    public VulkanPlatformGraphics(IVulkanDeviceFactory factory, VulkanPlatformSpecificOptions platformOptions)
    {
        _factory = factory;
        _platformOptions = platformOptions;
    }

    public IPlatformGraphicsContext CreateContext() =>
        new VulkanContext(_factory.CreateDevice(_platformOptions), _platformOptions.PlatformFeatures);

    public IPlatformGraphicsContext GetSharedContext()
    {
        if (_currentSharedContext?.IsLost == true)
            _currentSharedContext = null;
        return _currentSharedContext =
            new VulkanContext(_factory.GetSharedDevice(_platformOptions), _platformOptions.PlatformFeatures);
    }
    
    public bool UsesSharedContext => _factory.UsesShadedDevice;

    
    public interface IVulkanDeviceFactory
    {
        bool UsesShadedDevice { get; }
        IVulkanDevice GetSharedDevice(VulkanPlatformSpecificOptions platformOptions);
        IVulkanDevice CreateDevice(VulkanPlatformSpecificOptions platformOptions);
    }

    
    class DefaultDeviceFactory : IVulkanDeviceFactory
    {
        private readonly IVulkanInstance _instance;
        private readonly VulkanDeviceCreationOptions _deviceOptions;

        public DefaultDeviceFactory(IVulkanInstance instance, VulkanDeviceCreationOptions deviceOptions)
        {
            _instance = instance;
            _deviceOptions = deviceOptions;
        }

        public bool UsesShadedDevice => false;
        
        public IVulkanDevice GetSharedDevice(VulkanPlatformSpecificOptions platformOptions) => throw new NotSupportedException();

        public IVulkanDevice CreateDevice(VulkanPlatformSpecificOptions platformOptions)
        {
            return Interop.VulkanDevice.Create(_instance, _deviceOptions, platformOptions);
        }
    }

    class CustomSharedDeviceFactory : IVulkanDeviceFactory
    {
        private readonly IVulkanDevice _device;

        public CustomSharedDeviceFactory(IVulkanDevice device)
        {
            _device = device;
        }

        public bool UsesShadedDevice => true;
        public IVulkanDevice GetSharedDevice(VulkanPlatformSpecificOptions platformOptions) => _device;

        public IVulkanDevice CreateDevice(VulkanPlatformSpecificOptions platformOptions) =>
            throw new NotSupportedException();
    }
    
    public static VulkanPlatformGraphics? TryCreate(VulkanOptions options, VulkanPlatformSpecificOptions platformOptions)
    {
        if (options.CustomSharedDevice != null)
            return new(new CustomSharedDeviceFactory(options.CustomSharedDevice), platformOptions);

        IVulkanInstance? instance = null;
        try
        {
            instance = VulkanInstance.Create(options.VulkanInstanceCreationOptions ?? new(),
                platformOptions);

            var devOpts = options.VulkanDeviceCreationOptions ?? new();
            Interop.VulkanDevice.Create(instance, devOpts, platformOptions)
                .Dispose();

            return new VulkanPlatformGraphics(new DefaultDeviceFactory(instance, devOpts), platformOptions);
        }
        catch (Exception e)
        {
            //instance?.Dispose();
            Logger.TryGet(LogEventLevel.Error, "Vulkan")?.Log(null, "Unable to initialize Vulkan rendering: {0}", e);
            return null;
        }
    }
}



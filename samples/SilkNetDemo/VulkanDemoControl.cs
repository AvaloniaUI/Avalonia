using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Threading;
using Avalonia.Vulkan;
using Silk.NET.Core.Contexts;
using Silk.NET.Vulkan;
using Buffer = System.Buffer;
using Image = Silk.NET.Vulkan.Image;
// ReSharper disable RedundantUnsafeContext
// ReSharper disable RedundantEmptyObjectCreationArgumentList
// ReSharper disable MustUseReturnValue
#pragma warning disable CS0618

namespace SilkNetDemo;

public unsafe class VulkanDemoControl : VulkanControlBase
{
    private float _yaw;

    public static readonly DirectProperty<VulkanDemoControl, float> YawProperty =
        AvaloniaProperty.RegisterDirect<VulkanDemoControl, float>("Yaw", o => o.Yaw, (o, v) => o.Yaw = v);

    public float Yaw
    {
        get => _yaw;
        set => SetAndRaise(YawProperty, ref _yaw, value);
    }

    private float _pitch;

    public static readonly DirectProperty<VulkanDemoControl, float> PitchProperty =
        AvaloniaProperty.RegisterDirect<VulkanDemoControl, float>("Pitch", o => o.Pitch, (o, v) => o.Pitch = v);

    public float Pitch
    {
        get => _pitch;
        set => SetAndRaise(PitchProperty, ref _pitch, value);
    }


    private float _roll;

    public static readonly DirectProperty<VulkanDemoControl, float> RollProperty =
        AvaloniaProperty.RegisterDirect<VulkanDemoControl, float>("Roll", o => o.Roll, (o, v) => o.Roll = v);

    public float Roll
    {
        get => _roll;
        set => SetAndRaise(RollProperty, ref _roll, value);
    }


    private float _disco;

    public static readonly DirectProperty<VulkanDemoControl, float> DiscoProperty =
        AvaloniaProperty.RegisterDirect<VulkanDemoControl, float>("Disco", o => o.Disco, (o, v) => o.Disco = v);

    public float Disco
    {
        get => _disco;
        set => SetAndRaise(DiscoProperty, ref _disco, value);
    }

    private string _info;

    public static readonly DirectProperty<VulkanDemoControl, string> InfoProperty =
        AvaloniaProperty.RegisterDirect<VulkanDemoControl, string>("Info", o => o.Info, (o, v) => o.Info = v);

    public string Info
    {
        get => _info;
        private set => SetAndRaise(InfoProperty, ref _info, value);
    }
    
    static VulkanDemoControl()
    {
        AffectsRender<VulkanDemoControl>(YawProperty, PitchProperty, RollProperty, DiscoProperty);
    }

    private ShaderModule _vertShader;
    private ShaderModule _fragShader;
    private PipelineLayout _pipelineLayout;
    private RenderPass _renderPass;
    private Pipeline _pipeline;
    private Silk.NET.Vulkan.Buffer _vertexBuffer;
    private DeviceMemory _vertexBufferMemory;
    private Silk.NET.Vulkan.Buffer _indexBuffer;
    private DeviceMemory _indexBufferMemory;
    private Framebuffer[] _framebuffers;

    private Image _depthImage;
    private DeviceMemory _depthImageMemory;
    private ImageView _depthImageView;
    private VulkanCommandBufferPool _commandPool;
    private Vk _vk;

    private byte[] GetShader(bool fragment)
    {
        var name = typeof(VulkanDemoControl).Assembly.GetManifestResourceNames()
            .First(x => x.Contains((fragment ? "frag" : "vert") + ".spirv"));
        using (var sr = typeof(VulkanDemoControl).Assembly.GetManifestResourceStream(name))
        {
            using (var mem = new MemoryStream())
            {
                sr.CopyTo(mem);
                return mem.ToArray();
            }
        }
    }

    private ISwapchain? _previousSwapchain;

    protected override void OnVulkanRender(IVulkanSharedDevice sharedDevice, ISwapchain swapchain)
    {
        using (sharedDevice.Device.Lock())
        {
            var api = _vk;
            var device = new Device(sharedDevice.Device.Handle);
            var physicalDevice = new PhysicalDevice(sharedDevice.Device.PhysicalDeviceHandle);
            _commandPool?.FreeUsedCommandBuffers();
            if (_previousSwapchain != swapchain)
                CreateTemporalObjects(api, device, physicalDevice, swapchain);

            _previousSwapchain = swapchain;

            var pixelSize = swapchain.Size;

            var view = Matrix4x4.CreateLookAt(new Vector3(25, 25, 25), new Vector3(), new Vector3(0, -1, 0));
            var model = Matrix4x4.CreateFromYawPitchRoll(_yaw, _pitch, _roll);
            var projection =
                Matrix4x4.CreatePerspectiveFieldOfView((float)Math.PI / 4f, (float)pixelSize.Width / (float)pixelSize.Height,
                    0.01f, 1000);

            var vertexConstant = new VertextPushConstant()
            {
                Disco = _disco,
                Model = model,
                Projection = projection,
                Time = (float)St.Elapsed.TotalSeconds,
                View = view
            };

            var fragConstant = new FragmentPushConstant()
            {
                Disco = _disco,
                Time = (float)St.Elapsed.TotalSeconds,
                MinY = _minY,
                MaxY = _maxY,
            };

            var commandBuffer = _commandPool.CreateCommandBuffer();
            commandBuffer.BeginRecording();
            var commandBufferHandle = new CommandBuffer(commandBuffer.Handle);

            api.CmdSetViewport(commandBufferHandle, 0, 1,
                new Viewport()
                {
                    Width = (float)pixelSize.Width,
                    Height = (float)pixelSize.Height,
                    MaxDepth = 1,
                    MinDepth = 0,
                    X = 0,
                    Y = 0
                });

            var scissor = new Rect2D
            {
                Extent = new Extent2D((uint?)pixelSize.Width, (uint?)pixelSize.Height)
            };

            api.CmdSetScissor(commandBufferHandle, 0, 1, &scissor);

            var clearColor = new ClearValue(new ClearColorValue(0, 0, 0, 0), new ClearDepthStencilValue(1, 0));

            var clearValues = new[] { clearColor, clearColor };


            fixed (ClearValue* clearValue = clearValues)
            {
                var beginInfo = new RenderPassBeginInfo()
                {
                    SType = StructureType.RenderPassBeginInfo,
                    RenderPass = _renderPass,
                    Framebuffer = _framebuffers[swapchain.CurrentImageIndex],
                    RenderArea =
                        new Rect2D(new Offset2D(0, 0), new Extent2D((uint?)pixelSize.Width, (uint?)pixelSize.Height)),
                    ClearValueCount = 2,
                    PClearValues = clearValue
                };

                api.CmdBeginRenderPass(commandBufferHandle, beginInfo, SubpassContents.Inline);
            }

            api.CmdBindPipeline(commandBufferHandle, PipelineBindPoint.Graphics, _pipeline);

            api.CmdPushConstants(commandBufferHandle, _pipelineLayout, ShaderStageFlags.ShaderStageVertexBit, 0,
                (uint)Marshal.SizeOf<VertextPushConstant>(), &vertexConstant);
            api.CmdPushConstants(commandBufferHandle, _pipelineLayout, ShaderStageFlags.ShaderStageFragmentBit,
                (uint)Marshal.SizeOf<VertextPushConstant>(), (uint)Marshal.SizeOf<FragmentPushConstant>(),
                &fragConstant);

            api.CmdBindVertexBuffers(commandBufferHandle, 0, 1, _vertexBuffer, 0);
            api.CmdBindIndexBuffer(commandBufferHandle, _indexBuffer, 0, IndexType.Uint16);

            api.CmdDrawIndexed(commandBufferHandle, (uint)_indices.Length, 1, 0, 0, 0);

            api.CmdEndRenderPass(commandBufferHandle);
            
            commandBuffer.Submit();

            if (_disco > 0.01)
                Dispatcher.UIThread.InvokeAsync(InvalidateVisual, DispatcherPriority.Background);
        }
    }

    protected override void OnVulkanDeInit(IVulkanSharedDevice sharedDevice)
    {

        if (_isInit)
        {
            lock (sharedDevice.Device.Lock())
            {
                var api = _vk;
                var device = new Device(sharedDevice.Device.Handle);
                _vk.DeviceWaitIdle(device);

                DestroyTemporalObjects(api, device);

                api.DestroyShaderModule(device, _vertShader, null);
                api.DestroyShaderModule(device, _fragShader, null);

                api.DestroyBuffer(device, _vertexBuffer, null);
                api.FreeMemory(device, _vertexBufferMemory, null);

                api.DestroyBuffer(device, _indexBuffer, null);
                api.FreeMemory(device, _indexBufferMemory, null);
                
                _commandPool.Dispose();
            }
        }
        
        _isInit = false;
    }

    public unsafe void DestroyTemporalObjects(Vk api, Device device)
    {
        if (_isInit)
        {
            if (_renderPass.Handle != 0)
            {
                api.DestroyImageView(device, _depthImageView, null);
                api.DestroyImage(device, _depthImage, null);
                api.FreeMemory(device, _depthImageMemory, null);

                foreach (var fb in _framebuffers)
                    api.DestroyFramebuffer(device, fb, null);
                api.DestroyPipeline(device, _pipeline, null);
                api.DestroyPipelineLayout(device, _pipelineLayout, null);
                api.DestroyRenderPass(device, _renderPass, null);

                _depthImage = default;
                _depthImageView = default;
                _depthImageView = default;
                _framebuffers = Array.Empty<Framebuffer>();
                _pipeline = default;
                _renderPass = default;
                _pipelineLayout = default;
            }
        }
    }

    private unsafe void CreateDepthAttachment(Vk api, Device device, PhysicalDevice physicalDevice, PixelSize size)
    {
        var imageCreateInfo = new ImageCreateInfo
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.ImageType2D,
            Format = Format.D32Sfloat,
            Extent =
                new Extent3D((uint?)size.Width,
                    (uint?)size.Height, 1),
            MipLevels = 1,
            ArrayLayers = 1,
            Samples = SampleCountFlags.SampleCount1Bit,
            Tiling = ImageTiling.Optimal,
            Usage = ImageUsageFlags.ImageUsageDepthStencilAttachmentBit,
            SharingMode = SharingMode.Exclusive,
            InitialLayout = ImageLayout.Undefined,
            Flags = ImageCreateFlags.ImageCreateMutableFormatBit
        };

        api
            .CreateImage(device, imageCreateInfo, null, out _depthImage).ThrowOnError();

        api.GetImageMemoryRequirements(device, _depthImage,
            out var memoryRequirements);

        var memoryAllocateInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memoryRequirements.Size,
            MemoryTypeIndex = (uint)FindSuitableMemoryTypeIndex(api,
                physicalDevice,
                memoryRequirements.MemoryTypeBits, MemoryPropertyFlags.MemoryPropertyDeviceLocalBit)
        };

        api.AllocateMemory(device, memoryAllocateInfo, null,
            out _depthImageMemory).ThrowOnError();

        api.BindImageMemory(device, _depthImage, _depthImageMemory, 0);

        var componentMapping = new ComponentMapping(
            ComponentSwizzle.R,
            ComponentSwizzle.G,
            ComponentSwizzle.B,
            ComponentSwizzle.A);

        var subresourceRange = new ImageSubresourceRange(ImageAspectFlags.ImageAspectDepthBit,
            0, 1, 0, 1);

        var imageViewCreateInfo = new ImageViewCreateInfo
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = _depthImage,
            ViewType = ImageViewType.ImageViewType2D,
            Format = Format.D32Sfloat,
            Components = componentMapping,
            SubresourceRange = subresourceRange
        };

        api
            .CreateImageView(device, imageViewCreateInfo, null, out _depthImageView)
            .ThrowOnError();
    }

    private unsafe void CreateTemporalObjects(Vk api, Device device, PhysicalDevice physicalDevice,
        ISwapchain swapchain)
    {
        DestroyTemporalObjects(api, device);

        CreateDepthAttachment(api, device, physicalDevice, swapchain.Size);

        var current = swapchain.GetImage(swapchain.CurrentImageIndex);
        // create renderpasses
        var colorAttachment = new AttachmentDescription()
        {
            Format = (Format)current.Format,
            Samples = SampleCountFlags.SampleCount1Bit,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store,
            InitialLayout = (ImageLayout)current.Layout,
            FinalLayout = (ImageLayout)current.Layout,
            StencilLoadOp = AttachmentLoadOp.DontCare,
            StencilStoreOp = AttachmentStoreOp.DontCare
        };

        var depthAttachment = new AttachmentDescription()
        {
            Format = Format.D32Sfloat,
            Samples = SampleCountFlags.SampleCount1Bit,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.DontCare,
            InitialLayout = ImageLayout.Undefined,
            FinalLayout = ImageLayout.DepthStencilAttachmentOptimal,
            StencilLoadOp = AttachmentLoadOp.DontCare,
            StencilStoreOp = AttachmentStoreOp.DontCare
        };

        var subpassDependency = new SubpassDependency()
        {
            SrcSubpass = Vk.SubpassExternal,
            DstSubpass = 0,
            SrcStageMask = PipelineStageFlags.PipelineStageColorAttachmentOutputBit,
            SrcAccessMask = 0,
            DstStageMask = PipelineStageFlags.PipelineStageColorAttachmentOutputBit,
            DstAccessMask = AccessFlags.AccessColorAttachmentWriteBit
        };

        var colorAttachmentReference = new AttachmentReference()
        {
            Attachment = 0, Layout = ImageLayout.ColorAttachmentOptimal
        };

        var depthAttachmentReference = new AttachmentReference()
        {
            Attachment = 1, Layout = ImageLayout.DepthStencilAttachmentOptimal
        };

        var subpassDescription = new SubpassDescription()
        {
            PipelineBindPoint = PipelineBindPoint.Graphics,
            ColorAttachmentCount = 1,
            PColorAttachments = &colorAttachmentReference,
            PDepthStencilAttachment = &depthAttachmentReference
        };

        var attachments = new[] { colorAttachment, depthAttachment };

        fixed (AttachmentDescription* atPtr = attachments)
        {
            var renderPassCreateInfo = new RenderPassCreateInfo()
            {
                SType = StructureType.RenderPassCreateInfo,
                AttachmentCount = (uint)attachments.Length,
                PAttachments = atPtr,
                SubpassCount = 1,
                PSubpasses = &subpassDescription,
                DependencyCount = 1,
                PDependencies = &subpassDependency
            };

            api.CreateRenderPass(device, renderPassCreateInfo, null, out _renderPass).ThrowOnError();


            _framebuffers = new Framebuffer[swapchain.ImageCount];
            for (var c = 0; c < _framebuffers.Length; c++)
            {
                // create framebuffer
                var frameBufferAttachments = new[] { new ImageView((ulong)swapchain.GetImage(c).ViewHandle), _depthImageView };

                fixed (ImageView* frAtPtr = frameBufferAttachments)
                {
                    var framebufferCreateInfo = new FramebufferCreateInfo()
                    {
                        SType = StructureType.FramebufferCreateInfo,
                        RenderPass = _renderPass,
                        AttachmentCount = (uint)frameBufferAttachments.Length,
                        PAttachments = frAtPtr,
                        Width = (uint)current.PixelSize.Width,
                        Height = (uint)current.PixelSize.Height,
                        Layers = 1
                    };

                    api.CreateFramebuffer(device, framebufferCreateInfo, null, out var fb).ThrowOnError();
                    _framebuffers[c] = fb;
                }
            }
        }

        // Create pipeline
        var pname = Marshal.StringToHGlobalAnsi("main");
        var vertShaderStageInfo = new PipelineShaderStageCreateInfo()
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.ShaderStageVertexBit,
            Module = _vertShader,
            PName = (byte*)pname,
        };
        var fragShaderStageInfo = new PipelineShaderStageCreateInfo()
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.ShaderStageFragmentBit,
            Module = _fragShader,
            PName = (byte*)pname,
        };

        var stages = new[] { vertShaderStageInfo, fragShaderStageInfo };

        var bindingDescription = Vertex.VertexInputBindingDescription;
        var attributeDescription = Vertex.VertexInputAttributeDescription;

        fixed (VertexInputAttributeDescription* attrPtr = attributeDescription)
        {
            var vertextInputInfo = new PipelineVertexInputStateCreateInfo()
            {
                SType = StructureType.PipelineVertexInputStateCreateInfo,
                VertexAttributeDescriptionCount = (uint)attributeDescription.Length,
                VertexBindingDescriptionCount = 1,
                PVertexAttributeDescriptions = attrPtr,
                PVertexBindingDescriptions = &bindingDescription
            };

            var inputAssembly = new PipelineInputAssemblyStateCreateInfo()
            {
                SType = StructureType.PipelineInputAssemblyStateCreateInfo,
                Topology = PrimitiveTopology.TriangleList,
                PrimitiveRestartEnable = false
            };

            var viewport = new Viewport()
            {
                X = 0,
                Y = 0,
                Width = (float)swapchain.Size.Width,
                Height = (float)swapchain.Size.Height,
                MinDepth = 0,
                MaxDepth = 1
            };

            var scissor = new Rect2D()
            {
                Offset = new Offset2D(0, 0), Extent = new Extent2D((uint)viewport.Width, (uint)viewport.Height)
            };

            var pipelineViewPortCreateInfo = new PipelineViewportStateCreateInfo()
            {
                SType = StructureType.PipelineViewportStateCreateInfo,
                ViewportCount = 1,
                PViewports = &viewport,
                ScissorCount = 1,
                PScissors = &scissor
            };

            var rasterizerStateCreateInfo = new PipelineRasterizationStateCreateInfo()
            {
                SType = StructureType.PipelineRasterizationStateCreateInfo,
                DepthClampEnable = false,
                RasterizerDiscardEnable = false,
                PolygonMode = PolygonMode.Fill,
                LineWidth = 1,
                CullMode = CullModeFlags.CullModeNone,
                DepthBiasEnable = false
            };

            var multisampleStateCreateInfo = new PipelineMultisampleStateCreateInfo()
            {
                SType = StructureType.PipelineMultisampleStateCreateInfo,
                SampleShadingEnable = false,
                RasterizationSamples = SampleCountFlags.SampleCount1Bit
            };

            var depthStencilCreateInfo = new PipelineDepthStencilStateCreateInfo()
            {
                SType = StructureType.PipelineDepthStencilStateCreateInfo,
                StencilTestEnable = false,
                DepthCompareOp = CompareOp.Less,
                DepthTestEnable = true,
                DepthWriteEnable = true,
                DepthBoundsTestEnable = false,
            };

            var colorBlendAttachmentState = new PipelineColorBlendAttachmentState()
            {
                ColorWriteMask = ColorComponentFlags.ColorComponentABit |
                                 ColorComponentFlags.ColorComponentRBit |
                                 ColorComponentFlags.ColorComponentGBit |
                                 ColorComponentFlags.ColorComponentBBit,
                BlendEnable = false
            };

            var colorBlendState = new PipelineColorBlendStateCreateInfo()
            {
                SType = StructureType.PipelineColorBlendStateCreateInfo,
                LogicOpEnable = false,
                AttachmentCount = 1,
                PAttachments = &colorBlendAttachmentState
            };

            var dynamicStates = new[] { DynamicState.Viewport, DynamicState.Scissor };

            fixed (DynamicState* states = dynamicStates)
            {
                var dynamicStateCreateInfo = new PipelineDynamicStateCreateInfo()
                {
                    SType = StructureType.PipelineDynamicStateCreateInfo,
                    DynamicStateCount = (uint)dynamicStates.Length,
                    PDynamicStates = states
                };

                var vertexPushConstantRange = new PushConstantRange()
                {
                    Offset = 0,
                    Size = (uint)Marshal.SizeOf<VertextPushConstant>(),
                    StageFlags = ShaderStageFlags.ShaderStageVertexBit
                };

                var fragPushConstantRange = new PushConstantRange()
                {
                    Offset = vertexPushConstantRange.Size,
                    Size = (uint)Marshal.SizeOf<FragmentPushConstant>(),
                    StageFlags = ShaderStageFlags.ShaderStageFragmentBit
                };

                var constants = new[] { vertexPushConstantRange, fragPushConstantRange };

                fixed (PushConstantRange* constant = constants)
                {
                    var pipelineLayoutCreateInfo = new PipelineLayoutCreateInfo()
                    {
                        SType = StructureType.PipelineLayoutCreateInfo,
                        PushConstantRangeCount = (uint)constants.Length,
                        PPushConstantRanges = constant,
                        SetLayoutCount = 0
                    };

                    api.CreatePipelineLayout(device, pipelineLayoutCreateInfo, null, out _pipelineLayout)
                        .ThrowOnError();
                }


                fixed (PipelineShaderStageCreateInfo* stPtr = stages)
                {
                    var pipelineCreateInfo = new GraphicsPipelineCreateInfo()
                    {
                        SType = StructureType.GraphicsPipelineCreateInfo,
                        StageCount = 2,
                        PStages = stPtr,
                        PVertexInputState = &vertextInputInfo,
                        PInputAssemblyState = &inputAssembly,
                        PViewportState = &pipelineViewPortCreateInfo,
                        PRasterizationState = &rasterizerStateCreateInfo,
                        PMultisampleState = &multisampleStateCreateInfo,
                        PDepthStencilState = &depthStencilCreateInfo,
                        PColorBlendState = &colorBlendState,
                        PDynamicState = &dynamicStateCreateInfo,
                        Layout = _pipelineLayout,
                        RenderPass = _renderPass,
                        Subpass = 0,
                        BasePipelineHandle = _pipeline.Handle != 0 ? _pipeline : new Pipeline(),
                        BasePipelineIndex = _pipeline.Handle != 0 ? 0 : -1
                    };

                    api.CreateGraphicsPipelines(device, new PipelineCache(), 1, &pipelineCreateInfo, null,
                        out _pipeline).ThrowOnError();
                }
            }
        }

        Marshal.FreeHGlobal(pname);
        _isInit = true;
    }

    private unsafe void CreateBuffers(Vk api, Device device, IVulkanSharedDevice sharedDevice)
    {
        // vertex buffer
        var vertexSize = Marshal.SizeOf<Vertex>();

        var bufferInfo = new BufferCreateInfo()
        {
            SType = StructureType.BufferCreateInfo,
            Size = (ulong)(_points.Length * vertexSize),
            Usage = BufferUsageFlags.BufferUsageVertexBufferBit,
            SharingMode = SharingMode.Exclusive
        };

        api.CreateBuffer(device, bufferInfo, null, out _vertexBuffer).ThrowOnError();

        api.GetBufferMemoryRequirements(device, _vertexBuffer, out var memoryRequirements);

        var physicalDevice = new PhysicalDevice(sharedDevice.Device.PhysicalDeviceHandle);

        var memoryAllocateInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memoryRequirements.Size,
            MemoryTypeIndex = (uint)FindSuitableMemoryTypeIndex(api,
                physicalDevice,
                memoryRequirements.MemoryTypeBits,
                MemoryPropertyFlags.MemoryPropertyHostCoherentBit |
                MemoryPropertyFlags.MemoryPropertyHostVisibleBit)
        };

        api.AllocateMemory(device, memoryAllocateInfo, null, out _vertexBufferMemory).ThrowOnError();
        api.BindBufferMemory(device, _vertexBuffer, _vertexBufferMemory, 0);

        fixed (Vertex* points = _points)
        {
            void* pointer = null;
            api.MapMemory(device, _vertexBufferMemory, 0, bufferInfo.Size, 0, ref pointer);

            Buffer.MemoryCopy(points, pointer, bufferInfo.Size, bufferInfo.Size);
            api.UnmapMemory(device, _vertexBufferMemory);
        }

        var indexSize = Marshal.SizeOf<ushort>();

        bufferInfo = new BufferCreateInfo()
        {
            SType = StructureType.BufferCreateInfo,
            Size = (ulong)(_indices.Length * indexSize),
            Usage = BufferUsageFlags.BufferUsageIndexBufferBit,
            SharingMode = SharingMode.Exclusive
        };

        api.CreateBuffer(device, bufferInfo, null, out _indexBuffer).ThrowOnError();

        api.GetBufferMemoryRequirements(device, _indexBuffer, out memoryRequirements);

        memoryAllocateInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memoryRequirements.Size,
            MemoryTypeIndex = (uint)FindSuitableMemoryTypeIndex(api,
                physicalDevice,
                memoryRequirements.MemoryTypeBits,
                MemoryPropertyFlags.MemoryPropertyHostCoherentBit |
                MemoryPropertyFlags.MemoryPropertyHostVisibleBit)
        };

        api.AllocateMemory(device, memoryAllocateInfo, null, out _indexBufferMemory).ThrowOnError();
        api.BindBufferMemory(device, _indexBuffer, _indexBufferMemory, 0);

        fixed (ushort* indice = _indices)
        {
            void* pointer = null;
            api.MapMemory(device, _indexBufferMemory, 0, bufferInfo.Size, 0, ref pointer);

            Buffer.MemoryCopy(indice, pointer, bufferInfo.Size, bufferInfo.Size);
            api.UnmapMemory(device, _indexBufferMemory);
        }
    }

    private static int FindSuitableMemoryTypeIndex(Vk api, PhysicalDevice physicalDevice, uint memoryTypeBits,
        MemoryPropertyFlags flags)
    {
        api.GetPhysicalDeviceMemoryProperties(physicalDevice, out var properties);

        for (var i = 0; i < properties.MemoryTypeCount; i++)
        {
            var type = properties.MemoryTypes[i];

            if ((memoryTypeBits & (1 << i)) != 0 && type.PropertyFlags.HasFlag(flags)) return i;
        }

        return -1;
    }

    static Vk GetApi(IVulkanDevice device) =>
        new(new LamdaNativeContext(name =>
        {
            var deviceApi = device.Instance.GetDeviceProcAddress(device.Handle, name);
            if (deviceApi != IntPtr.Zero)
                return deviceApi;
            var instanceApi = device.Instance.GetInstanceProcAddress(device.Instance.Handle, name);
            if (instanceApi != IntPtr.Zero)
                return instanceApi;
            return device.Instance.GetInstanceProcAddress(IntPtr.Zero, name);
        }));


    protected override void OnVulkanInit(IVulkanSharedDevice sharedDevice)
    {
        using (sharedDevice.Device.Lock())
        {
            _vk = GetApi(sharedDevice.Device);

            var api = _vk;
            var device = new Device(sharedDevice.Device.Handle);
            api.DeviceWaitIdle(device);
            

            /*var deviceName = platformInterface.PhysicalDevice.DeviceName;
            var version = platformInterface.PhysicalDevice.ApiVersion;

            Info = $"Renderer: {deviceName} Version: {version.Major}.{version.Minor}.{version.Revision}";
*/
            var vertShaderData = GetShader(false);
            var fragShaderData = GetShader(true);

            fixed (byte* ptr = vertShaderData)
            {
                var shaderCreateInfo = new ShaderModuleCreateInfo()
                {
                    SType = StructureType.ShaderModuleCreateInfo,
                    CodeSize = (nuint)vertShaderData.Length,
                    PCode = (uint*)ptr,
                };

                api.CreateShaderModule(device, shaderCreateInfo, null, out _vertShader);
            }

            fixed (byte* ptr = fragShaderData)
            {
                var shaderCreateInfo = new ShaderModuleCreateInfo()
                {
                    SType = StructureType.ShaderModuleCreateInfo,
                    CodeSize = (nuint)fragShaderData.Length,
                    PCode = (uint*)ptr,
                };

                api.CreateShaderModule(device, shaderCreateInfo, null, out _fragShader);
            }

            CreateBuffers(api, device, sharedDevice);
            
            var fenceCreateInfo = new FenceCreateInfo()
            {
                SType = StructureType.FenceCreateInfo,
                Flags = FenceCreateFlags.FenceCreateSignaledBit
            };

            _commandPool = new VulkanCommandBufferPool(api, device, new Queue(sharedDevice.Device.MainQueueHandle),
                sharedDevice.Device.GraphicsQueueFamilyIndex);
        }
    }


    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct Vertex
    {
        public Vector3 Position;
        public Vector3 Normal;

        public static unsafe VertexInputBindingDescription VertexInputBindingDescription =>
            new VertexInputBindingDescription()
            {
                Binding = 0,
                Stride = (uint)Marshal.SizeOf<Vertex>(),
                InputRate = VertexInputRate.Vertex
            };

        public static unsafe VertexInputAttributeDescription[] VertexInputAttributeDescription
        {
            get
            {
                return new[]
                {
                    new VertexInputAttributeDescription
                    {
                        Binding = 0,
                        Location = 0,
                        Format = Format.R32G32B32Sfloat,
                        Offset = (uint)Marshal.OffsetOf<Vertex>("Position")
                    },
                    new VertexInputAttributeDescription
                    {
                        Binding = 0,
                        Location = 1,
                        Format = Format.R32G32B32Sfloat,
                        Offset = (uint)Marshal.OffsetOf<Vertex>("Normal")
                    }
                };
            }
        }
    }

    private readonly Vertex[] _points;
    private readonly ushort[] _indices;
    private readonly float _minY;
    private readonly float _maxY;


    static Stopwatch St = Stopwatch.StartNew();
    private bool _isInit;

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct VertextPushConstant
    {
        public Matrix4x4 Model;
        public Matrix4x4 Projection;
        public Matrix4x4 View;

        public float Time;
        public float Disco;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct FragmentPushConstant
    {
        public float MaxY;
        public float MinY;

        public float Time;
        public float Disco;
    }

    public VulkanDemoControl()
    {
        var name = GetType().Assembly.GetManifestResourceNames().First(x => x.Contains("teapot.bin"));
        using (var sr = new BinaryReader(GetType().Assembly.GetManifestResourceStream(name)!))
        {
            var buf = new byte[sr.ReadInt32()];
            sr.Read(buf, 0, buf.Length);
            var points = new float[buf.Length / 4];
            Buffer.BlockCopy(buf, 0, points, 0, buf.Length);
            buf = new byte[sr.ReadInt32()];
            sr.Read(buf, 0, buf.Length);
            _indices = new ushort[buf.Length / 2];
            Buffer.BlockCopy(buf, 0, _indices, 0, buf.Length);
            _points = new Vertex[points.Length / 3];
            for (var primitive = 0; primitive < points.Length / 3; primitive++)
            {
                var srci = primitive * 3;
                _points[primitive] = new Vertex
                {
                    Position = new Vector3(points[srci], points[srci + 1], points[srci + 2])
                };
            }

            for (int i = 0; i < _indices.Length; i += 3)
            {
                Vector3 a = _points[_indices[i]].Position;
                Vector3 b = _points[_indices[i + 1]].Position;
                Vector3 c = _points[_indices[i + 2]].Position;
                var normal = Vector3.Normalize(Vector3.Cross(c - b, a - b));

                _points[_indices[i]].Normal += normal;
                _points[_indices[i + 1]].Normal += normal;
                _points[_indices[i + 2]].Normal += normal;
            }

            for (int i = 0; i < _points.Length; i++)
            {
                _points[i].Normal = Vector3.Normalize(_points[i].Normal);
                _maxY = Math.Max(_maxY, _points[i].Position.Y);
                _minY = Math.Min(_minY, _points[i].Position.Y);
            }
        }

    }
}

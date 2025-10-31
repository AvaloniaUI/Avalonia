using System.Collections.Generic;

namespace Avalonia.Vulkan;

static class KnownExtensions
{
    // https://github.com/google/skia/blob/c381e69aac29ec1f64dee6be2872aced3ad05d59/tools/gpu/vk/VkTestUtils.cpp#L197
    public static List<string> SkiaKnownExtensions2 = new();
    public static List<string> SkiaKnownExtensions = new()
    {
        "VK_ARM_rasterization_order_attachment_access",
        "VK_EXT_blend_operation_advanced",
        "VK_EXT_conservative_rasterization",
        "VK_EXT_device_fault",
        "VK_EXT_extended_dynamic_state",
        "VK_EXT_extended_dynamic_state2",
        "VK_EXT_graphics_pipeline_library",
        "VK_EXT_image_drm_format_modifier",
        "VK_EXT_queue_family_foreign",
        "VK_EXT_pipeline_creation_cache_control",
        "VK_EXT_rasterization_order_attachment_access",
        "VK_EXT_rgba10x6_formats",
        "VK_EXT_vertex_input_dynamic_state",
        "VK_KHR_bind_memory2",
        "VK_KHR_copy_commands2",
        "VK_KHR_dedicated_allocation",
        "VK_KHR_driver_properties",
        "VK_KHR_external_memory_capabilities",
        "VK_KHR_external_memory",
        "VK_KHR_format_feature_flags2",
        "VK_KHR_get_memory_requirements2",
        "VK_KHR_get_physical_device_properties2",
        "VK_KHR_image_format_list",
        "VK_KHR_maintenance1",
        "VK_KHR_maintenance2",
        "VK_KHR_maintenance3",
        "VK_KHR_pipeline_library",
        "VK_KHR_sampler_ycbcr_conversion",
    };
}
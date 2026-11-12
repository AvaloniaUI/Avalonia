namespace Avalonia.Impeller;

/// <summary>
/// Options for the Impeller rendering backend.
/// </summary>
public class ImpellerOptions
{
    /// <summary>
    /// Gets or sets whether Impeller enables the Vulkan validation layers, when they are installed.
    /// </summary>
    public bool EnableVulkanValidation { get; set; }
}

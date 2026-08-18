using System.Threading.Tasks;
using Avalonia.FreeDesktop.AtSpi.DBusXml;

namespace Avalonia.FreeDesktop.AtSpi;

/// <summary>
/// <see cref="IOrgA11yAtspiApplication"/> implementation for <see cref="ApplicationAtSpiNode"/>.
/// </summary>
internal sealed class ApplicationNodeApplicationHandler : IOrgA11yAtspiApplication
{
    public ApplicationNodeApplicationHandler()
    {
        var version = AtSpiConstants.ResolveToolkitVersion();
        ToolkitName = "Avalonia";
        Version = version;
        ToolkitVersion = version;
        AtspiVersion = "2.1";
        InterfaceVersion = AtSpiConstants.ApplicationVersion;
    }

    public string ToolkitName { get; }
    public string Version { get; }
    public string ToolkitVersion { get; }
    public string AtspiVersion { get; }
    public uint InterfaceVersion { get; }
    public int Id { get; set; }

    public ValueTask<string> GetLocaleAsync(uint lctype) =>
        ValueTask.FromResult(AtSpiConstants.ResolveLocale());

    public ValueTask<string> GetApplicationBusAddressAsync() =>
        ValueTask.FromResult(string.Empty);
}
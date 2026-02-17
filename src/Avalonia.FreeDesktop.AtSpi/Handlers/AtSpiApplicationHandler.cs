using System.Threading.Tasks;
using Avalonia.FreeDesktop.AtSpi.DBusXml;
using static Avalonia.FreeDesktop.AtSpi.AtSpiConstants;

namespace Avalonia.FreeDesktop.AtSpi.Handlers
{
    internal sealed class AtSpiApplicationHandler : IOrgA11yAtspiApplication
    {
        public AtSpiApplicationHandler(AtSpiServer server, AtSpiNode node)
        {
            _ = server;
            _ = node;
            var version = ResolveToolkitVersion();
            ToolkitName = "Avalonia";
            Version = version;
            ToolkitVersion = version;
            AtspiVersion = "2.1";
            InterfaceVersion = ApplicationVersion;
        }

        public string ToolkitName { get; }
        public string Version { get; }
        public string ToolkitVersion { get; }
        public string AtspiVersion { get; }
        public uint InterfaceVersion { get; }

        public int Id { get; set; }

        public ValueTask<string> GetLocaleAsync(uint lctype)
        {
            return ValueTask.FromResult(ResolveLocale());
        }

        public ValueTask<string> GetApplicationBusAddressAsync()
        {
            return ValueTask.FromResult(string.Empty);
        }
    }
}

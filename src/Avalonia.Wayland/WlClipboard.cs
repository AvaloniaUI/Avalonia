using System;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Platform;
using NWayland.Protocols.Wayland;

namespace Avalonia.Wayland
{
    public class WlClipboard : IClipboard
    {
        private readonly AvaloniaWaylandPlatform _platform;
        private readonly WlDataDeviceManager _wlDataDeviceManager;
        private readonly WlDataDevice _wlDataDevice;

        public WlClipboard(AvaloniaWaylandPlatform platform)
        {
            _platform = platform;
            _wlDataDeviceManager = platform.WlRegistryHandler.Bind(WlDataDeviceManager.BindFactory, WlDataDeviceManager.InterfaceName, WlDataDeviceManager.InterfaceVersion);
        }

        public Task<string> GetTextAsync()
        {
            throw new NotImplementedException();
        }

        public Task SetTextAsync(string text)
        {
            throw new NotImplementedException();
        }

        public Task ClearAsync()
        {
            throw new NotImplementedException();
        }

        public Task SetDataObjectAsync(IDataObject data)
        {
            var wlDataSource = _wlDataDeviceManager.CreateDataSource();
            foreach (var format in data.GetDataFormats())
                wlDataSource.Offer(format);
            return Task.CompletedTask;
        }

        public Task<string[]> GetFormatsAsync()
        {
            throw new NotImplementedException();
        }

        public Task<object> GetDataAsync(string format)
        {
            throw new NotImplementedException();
        }
    }
}

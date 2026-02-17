using System;
using Avalonia.Automation.Provider;
using Avalonia.FreeDesktop.AtSpi.DBusXml;
using static Avalonia.FreeDesktop.AtSpi.AtSpiConstants;

namespace Avalonia.FreeDesktop.AtSpi.Handlers
{
    internal sealed class AtSpiValueHandler : IOrgA11yAtspiValue
    {
        private readonly AtSpiNode _node;

        public AtSpiValueHandler(AtSpiServer server, AtSpiNode node)
        {
            _ = server;
            _node = node;
        }

        public uint Version => ValueVersion;

        public double MinimumValue =>
            _node.Peer.GetProvider<IRangeValueProvider>() is { } p ? p.Minimum : 0;

        public double MaximumValue =>
            _node.Peer.GetProvider<IRangeValueProvider>() is { } p ? p.Maximum : 0;

        public double MinimumIncrement =>
            _node.Peer.GetProvider<IRangeValueProvider>() is { } p ? p.SmallChange : 0;

        public string Text => string.Empty;

        public double CurrentValue
        {
            get => _node.Peer.GetProvider<IRangeValueProvider>() is { } p ? p.Value : 0;
            set
            {
                if (_node.Peer.GetProvider<IRangeValueProvider>() is { } p)
                {
                    var clamped = Math.Max(p.Minimum, Math.Min(p.Maximum, value));
                    p.SetValue(clamped);
                }
            }
        }
    }
}

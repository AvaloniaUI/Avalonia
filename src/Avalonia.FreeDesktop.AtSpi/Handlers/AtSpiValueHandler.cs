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

        public double MinimumValue => _node.InvokeSync<IRangeValueProvider, double>(p => p.Minimum);

        public double MaximumValue => _node.InvokeSync<IRangeValueProvider, double>(p => p.Maximum);

        public double MinimumIncrement => _node.InvokeSync<IRangeValueProvider, double>(p => p.SmallChange);

        public string Text => string.Empty;

        public double CurrentValue
        {
            get => _node.InvokeSync<IRangeValueProvider, double>(p => p.Value);
            set
            {
                _node.InvokeSync<IRangeValueProvider>(p =>
                {
                    var clamped = Math.Max(p.Minimum, Math.Min(p.Maximum, value));
                    p.SetValue(clamped);
                });
            }
        }
    }
}

using System;
using System.Collections.Generic;
using Avalonia.DBus;
using Avalonia.FreeDesktop.AtSpi.DBusXml;
using static Avalonia.FreeDesktop.AtSpi.AtSpiConstants;

namespace Avalonia.FreeDesktop.AtSpi.Handlers
{
    internal sealed class AtSpiEventObjectHandler(AtSpiServer server, string path) : IOrgA11yAtspiEventObject
    {
        public uint Version => EventObjectVersion;

        public void EmitChildrenChangedSignal(string operation, int indexInParent, DBusVariant child)
        {
            EmitSignal("ChildrenChanged", operation, indexInParent, 0, child, EmptyProperties());
        }

        public void EmitPropertyChangeSignal(string propertyName, DBusVariant value)
        {
            EmitSignal("PropertyChange", propertyName, 0, 0, value, EmptyProperties());
        }

        public void EmitStateChangedSignal(string stateName, int detail1, DBusVariant value)
        {
            EmitSignal("StateChanged", stateName, detail1, 0, value, EmptyProperties());
        }

        public void EmitSelectionChangedSignal()
        {
            EmitSignal("SelectionChanged", string.Empty, 0, 0, new DBusVariant(0), EmptyProperties());
        }

        public void EmitBoundsChangedSignal()
        {
            EmitSignal("BoundsChanged", string.Empty, 0, 0, new DBusVariant(0), EmptyProperties());
        }

        private void EmitSignal(string member, params object[] body)
        {
            if (!server.HasEventListeners)
                return;

            var connection = server.A11yConnection;
            if (connection is null)
                return;

            var message = DBusMessage.CreateSignal(
                (DBusObjectPath)path,
                IfaceEventObject,
                member,
                body);

            _ = connection.SendMessageAsync(message);
        }

        private static Dictionary<string, DBusVariant> EmptyProperties()
        {
            return new Dictionary<string, DBusVariant>(StringComparer.Ordinal);
        }
    }
}

using System;
using System.Collections.Generic;
using Avalonia.DBus;
using Avalonia.FreeDesktop.AtSpi.DBusXml;
using static Avalonia.FreeDesktop.AtSpi.AtSpiConstants;

namespace Avalonia.FreeDesktop.AtSpi.Handlers
{
    /// <summary>
    /// Emits AT-SPI Event.Object signals (children-changed, state-changed, property-change).
    /// </summary>
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

        /// <summary>
        /// Emits object:text-changed with <paramref name="operation"/> "insert" or "delete";
        /// detail1 is the offset, detail2 the length, and the affected text rides in any_data.
        /// </summary>
        public void EmitTextChangedSignal(string operation, int offset, int length, string text)
        {
            EmitSignal("TextChanged", operation, offset, length, new DBusVariant(text), EmptyProperties());
        }

        public void EmitTextCaretMovedSignal(int offset)
        {
            EmitSignal("TextCaretMoved", string.Empty, offset, 0, new DBusVariant(0), EmptyProperties());
        }

        public void EmitTextSelectionChangedSignal()
        {
            EmitSignal("TextSelectionChanged", string.Empty, 0, 0, new DBusVariant(0), EmptyProperties());
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

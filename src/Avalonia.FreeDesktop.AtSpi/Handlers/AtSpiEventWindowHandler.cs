using System;
using System.Collections.Generic;
using Avalonia.DBus;
using Avalonia.FreeDesktop.AtSpi.DBusXml;
using static Avalonia.FreeDesktop.AtSpi.AtSpiConstants;

namespace Avalonia.FreeDesktop.AtSpi.Handlers
{
    /// <summary>
    /// Emits AT-SPI Event.Window signals (activate, deactivate).
    /// </summary>
    internal sealed class AtSpiEventWindowHandler(AtSpiServer server, string path) : IOrgA11yAtspiEventWindow
    {
        public void EmitActivateSignal()
        {
            EmitSignal("Activate", string.Empty, 0, 0, new DBusVariant("0"), EmptyProperties());
        }

        public void EmitDeactivateSignal()
        {
            EmitSignal("Deactivate", string.Empty, 0, 0, new DBusVariant("0"), EmptyProperties());
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
                IfaceEventWindow,
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

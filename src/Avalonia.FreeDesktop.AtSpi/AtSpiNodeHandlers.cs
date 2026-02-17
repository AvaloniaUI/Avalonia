using System;
using System.Collections.Generic;
using System.Threading;
using Avalonia.DBus;
using Avalonia.FreeDesktop.AtSpi.Handlers;

namespace Avalonia.FreeDesktop.AtSpi
{
    internal sealed class AtSpiNodeHandlers
    {
        public AtSpiNodeHandlers(AtSpiNode node)
        {
            Node = node;
        }

        public AtSpiNode Node { get; }
        public AtSpiAccessibleHandler? AccessibleHandler { get; set; }
        public AtSpiApplicationHandler? ApplicationHandler { get; set; }
        public AtSpiComponentHandler? ComponentHandler { get; set; }
        public AtSpiActionHandler? ActionHandler { get; set; }
        public AtSpiValueHandler? ValueHandler { get; set; }
        public AtSpiEventObjectHandler? EventObjectHandler { get; set; }

        public IDisposable Register(
            IDBusConnection connection,
            SynchronizationContext? synchronizationContext = null)
        {
            ArgumentNullException.ThrowIfNull(connection);

            var targets = new List<object>();
            if (AccessibleHandler != null)
                targets.Add(AccessibleHandler);
            if (ApplicationHandler != null)
                targets.Add(ApplicationHandler);
            if (ComponentHandler != null)
                targets.Add(ComponentHandler);
            if (ActionHandler != null)
                targets.Add(ActionHandler);
            if (ValueHandler != null)
                targets.Add(ValueHandler);
            if (EventObjectHandler != null)
                targets.Add(EventObjectHandler);

            if (targets.Count == 0)
                return EmptyRegistration.Instance;

            return connection.RegisterObjects((DBusObjectPath)Node.Path, targets, synchronizationContext);
        }

        private sealed class EmptyRegistration : IDisposable
        {
            public static EmptyRegistration Instance { get; } = new();
            public void Dispose() { }
        }
    }
}

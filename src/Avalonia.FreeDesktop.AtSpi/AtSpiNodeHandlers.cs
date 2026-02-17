using System;
using System.Collections.Generic;
using System.Threading;
using Avalonia.DBus;
using Avalonia.FreeDesktop.AtSpi.Handlers;

namespace Avalonia.FreeDesktop.AtSpi
{
    internal sealed class AtSpiNodeHandlers(AtSpiNode node)
    {
        public AtSpiNode Node { get; } = node;

        /// <summary>
        /// The set of interface names that were used when building these handlers.
        /// Used to detect when the peer's providers have changed and a rebuild is needed.
        /// </summary>
        public HashSet<string>? RegisteredInterfaces { get; set; }

        public AtSpiAccessibleHandler? AccessibleHandler { get; set; }
        public AtSpiApplicationHandler? ApplicationHandler { get; set; }
        public AtSpiComponentHandler? ComponentHandler { get; set; }
        public AtSpiActionHandler? ActionHandler { get; set; }
        public AtSpiValueHandler? ValueHandler { get; set; }
        public AtSpiSelectionHandler? SelectionHandler { get; set; }
        public AtSpiTextHandler? TextHandler { get; set; }
        public AtSpiEditableTextHandler? EditableTextHandler { get; set; }
        public AtSpiImageHandler? ImageHandler { get; set; }
        public AtSpiCollectionHandler? CollectionHandler { get; set; }
        public AtSpiEventObjectHandler? EventObjectHandler { get; set; }
        public AtSpiEventWindowHandler? EventWindowHandler { get; set; }

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
            if (SelectionHandler != null)
                targets.Add(SelectionHandler);
            if (TextHandler != null)
                targets.Add(TextHandler);
            if (EditableTextHandler != null)
                targets.Add(EditableTextHandler);
            if (ImageHandler != null)
                targets.Add(ImageHandler);
            if (CollectionHandler != null)
                targets.Add(CollectionHandler);
            if (EventObjectHandler != null)
                targets.Add(EventObjectHandler);
            if (EventWindowHandler != null)
                targets.Add(EventWindowHandler);

            return targets.Count == 0 ? EmptyRegistration.Instance : 
                connection.RegisterObjects((DBusObjectPath)Node.Path, targets, synchronizationContext);
        }

        private sealed class EmptyRegistration : IDisposable
        {
            public static EmptyRegistration Instance { get; } = new();
            public void Dispose() { }
        }
    }
}

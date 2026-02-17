using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.DBus;
using Avalonia.FreeDesktop.AtSpi.DBusXml;
using static Avalonia.FreeDesktop.AtSpi.AtSpiConstants;

namespace Avalonia.FreeDesktop.AtSpi
{
    /// <summary>
    /// Monitors the AT-SPI registry to determine if any screen reader is listening for events.
    /// </summary>
    internal sealed class AtSpiRegistryEventTracker : IDisposable
    {
        private readonly DBusConnection _connection;
        private readonly HashSet<string> _registeredEvents = new(StringComparer.Ordinal);

        private OrgA11yAtspiRegistryProxy? _registryProxy;
        private IDisposable? _registryRegisteredSubscription;
        private IDisposable? _registryDeregisteredSubscription;
        private IDisposable? _registryOwnerChangedSubscription;
        private string? _registryUniqueName;

        internal AtSpiRegistryEventTracker(DBusConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        /// <summary>
        /// Indicates whether any screen reader is currently listening for object events.
        /// Defaults to true (chatty) until registry tracking confirms otherwise.
        /// </summary>
        internal bool HasEventListeners { get; private set; } = true;

        internal async Task InitializeAsync()
        {
            try
            {
                _registryProxy ??= new OrgA11yAtspiRegistryProxy(
                    _connection, BusNameRegistry, new DBusObjectPath(RegistryPath));

                // Seed from current registrations
                var events = await _registryProxy.GetRegisteredEventsAsync();
                _registeredEvents.Clear();
                foreach (var registered in events)
                    _registeredEvents.Add(registered.EventName);
                UpdateHasEventListeners();

                // Resolve registry unique name and subscribe to signals
                var registryOwner = await _connection.GetNameOwnerAsync(BusNameRegistry);
                await SubscribeToRegistrySignalsAsync(registryOwner);

                // Watch for registry daemon restarts
                _registryOwnerChangedSubscription ??= await _connection.WatchNameOwnerChangedAsync(
                    (name, oldOwner, newOwner) =>
                    {
                        if (!string.Equals(name, BusNameRegistry, StringComparison.Ordinal))
                            return;

                        _ = SubscribeToRegistrySignalsAsync(newOwner);
                    },
                    emitOnCapturedContext: true);
            }
            catch
            {
                // Registry event tracking unavailable - remain chatty.
                HasEventListeners = true;
            }
        }

        public void Dispose()
        {
            _registryOwnerChangedSubscription?.Dispose();
            _registryOwnerChangedSubscription = null;
            _registryRegisteredSubscription?.Dispose();
            _registryRegisteredSubscription = null;
            _registryDeregisteredSubscription?.Dispose();
            _registryDeregisteredSubscription = null;
            _registryProxy = null;
            _registryUniqueName = null;
            _registeredEvents.Clear();
        }

        private async Task SubscribeToRegistrySignalsAsync(string? registryOwner)
        {
            if (string.Equals(_registryUniqueName, registryOwner, StringComparison.Ordinal))
                return;

            // Dispose old subscriptions
            _registryRegisteredSubscription?.Dispose();
            _registryRegisteredSubscription = null;
            _registryDeregisteredSubscription?.Dispose();
            _registryDeregisteredSubscription = null;
            _registryUniqueName = registryOwner;

            var senderFilter = string.IsNullOrWhiteSpace(registryOwner) ? null : registryOwner;

            _registryProxy ??= new OrgA11yAtspiRegistryProxy(
                _connection, BusNameRegistry, new DBusObjectPath(RegistryPath));

            try
            {
                _registryRegisteredSubscription = await _registryProxy.WatchEventListenerRegisteredAsync(
                    OnRegistryEventListenerRegistered,
                    senderFilter,
                    emitOnCapturedContext: true);

                _registryDeregisteredSubscription = await _registryProxy.WatchEventListenerDeregisteredAsync(
                    OnRegistryEventListenerDeregistered,
                    senderFilter,
                    emitOnCapturedContext: true);
            }
            catch
            {
                _registryRegisteredSubscription?.Dispose();
                _registryRegisteredSubscription = null;
                _registryDeregisteredSubscription?.Dispose();
                _registryDeregisteredSubscription = null;
                HasEventListeners = true;
            }
        }

        private void OnRegistryEventListenerRegistered(string bus, string @event, List<string> properties)
        {
            _registeredEvents.Add(@event);
            UpdateHasEventListeners();
        }

        private void OnRegistryEventListenerDeregistered(string bus, string @event)
        {
            _registeredEvents.Remove(@event);
            UpdateHasEventListeners();
        }

        private void UpdateHasEventListeners()
        {
            HasEventListeners = _registeredEvents.Any(IsObjectEventClass);
        }

        private static bool IsObjectEventClass(string eventName)
        {
            if (string.IsNullOrWhiteSpace(eventName))
                return false;

            if (eventName == "*")
                return true;

            return eventName.StartsWith("object:", StringComparison.OrdinalIgnoreCase)
                || eventName.StartsWith("window:", StringComparison.OrdinalIgnoreCase)
                || eventName.StartsWith("focus:", StringComparison.OrdinalIgnoreCase);
        }
    }
}

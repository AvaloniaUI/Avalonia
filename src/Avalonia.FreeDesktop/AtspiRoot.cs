using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reactive.Disposables;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls.Automation.Peers;
using Avalonia.Controls.Platform;
using Avalonia.FreeDesktop.Atspi;
using Avalonia.Logging;
using Avalonia.Platform;
using Avalonia.Threading;
using Tmds.DBus;

#nullable enable

namespace Avalonia.FreeDesktop
{
    public class AtspiRoot : IAccessible, IApplication, IPlatformAutomationPeerFactory
    {
        private const string RootPath = "/org/a11y/atspi/accessible/root";
        private const string AtspiVersion = "2.1";

        // TODO: Not sure where to store this shared instance.
        private static AtspiRoot? _instance;
        private static bool _instanceInitialized;

        private readonly List<Child> _children = new List<Child>();
        private Connection? _connection;
        private string? _localName;
        private AccessibleProperties? _accessibleProperties;
        private ApplicationProperties? _applicationProperties;

        public AtspiRoot(Connection sessionConnection)
        {
            Register(sessionConnection);
            Attributes = new Dictionary<string, string> { { "toolkit", "Avalonia" } };
        }

        public ObjectPath ObjectPath => RootPath;
        public (string, ObjectPath) ApplicationPath => _accessibleProperties!.Parent;
        public IDictionary<string, string> Attributes { get; }
        
        public static AtspiRoot? RegisterRoot(Func<AutomationPeer> peerGetter)
        {
            if (!_instanceInitialized)
            {
                _instance = DBusHelper.Connection != null ? new AtspiRoot(DBusHelper.Connection) : null;
                _instanceInitialized = true;
            }

            _instance?._children.Add(new Child(peerGetter));
            return _instance;
        }

        public IAutomationPeerImpl CreateAutomationPeerImpl(AutomationPeer peer)
        {
            return AtspiContextFactory.Create(this, peer);
        }

        private async void Register(Connection sessionConnection)
        {
            try
            {
                // Get the address of the a11y bus and open a connection to it.
                var bus = sessionConnection.CreateProxy<IBus>("org.a11y.Bus", "/org/a11y/bus");
                var address = await bus.GetAddressAsync();
                var connection = new Connection(address);
                var connectionInfo = await connection.ConnectAsync();

                // Register the org.a11y.atspi.Application and org.a11y.atspi.Accessible interfaces at the well-known
                // object path
                await connection.RegisterObjectAsync(this);
            
                // Register ourselves on the a11y bus.
                var socket = connection.CreateProxy<ISocket>("org.a11y.atspi.Registry", RootPath);
                var plug = (connectionInfo.LocalName, RootPath);

                _accessibleProperties = new AccessibleProperties
                {
                    Name = Application.Current.Name ?? "Unnamed",
                    Locale = CultureInfo.CurrentCulture.Name,
                    ChildCount = _children.Count,
                    AccessibleId = string.Empty,
                };
                
                _applicationProperties = new ApplicationProperties
                {
                    Id  = 0,
                    Version = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly()!.Location).FileVersion,
                    AtspiVersion = AtspiVersion,
                    ToolkitName = "Avalonia",
                };
                
                _accessibleProperties.Parent = await socket.EmbedAsync(plug);
                _localName = connectionInfo.LocalName;
                _connection = connection;
            }
            catch (Exception e)
            {
                Logger.TryGet(LogEventLevel.Error, LogArea.Control)?.Log(this, "Error connecting to AT-SPI bus: {Error}", e);
            }
        }

        async Task<(string, ObjectPath)> IAccessible.GetChildAtIndexAsync(int index)
        {
            var child = _children[index];
            var peer = child.Peer;

            if (peer is null)
            {
                await Dispatcher.UIThread.InvokeAsync(() => child.CreatePeer());
                peer = child.Peer!;
            }

            var context = (AtspiContext?)peer.PlatformImpl;
            
            if (context is null)
                throw new AvaloniaInternalException("AutomationPeer has no platform implementation.");

            return (_localName!, context.ObjectPath);
        }

        Task<(string, ObjectPath)[]> IAccessible.GetChildrenAsync()
        {
            var result = new List<(string, ObjectPath)>();
            
            // foreach (var p in _automationPeers)
            // {
            //     var peer = p();
            //     result.Add((_localName, p.GetHashCode().ToString()));
            // }

            return Task.FromResult(result.ToArray());
        }

        Task<int> IAccessible.GetIndexInParentAsync() => Task.FromResult(-1);

        Task<(uint, (string, ObjectPath)[])[]> IAccessible.GetRelationSetAsync()
        {
            return Task.FromResult(Array.Empty<(uint, (string, ObjectPath)[])>());            
        }

        Task<uint> IAccessible.GetRoleAsync() => Task.FromResult((uint)AtspiRole.ATSPI_ROLE_APPLICATION);
        Task<string> IAccessible.GetRoleNameAsync() => Task.FromResult("application");
        Task<string> IAccessible.GetLocalizedRoleNameAsync() => Task.FromResult("application");
        Task<uint[]> IAccessible.GetStateAsync() => Task.FromResult(new uint[] { 0, 0 });
        Task<(string, ObjectPath)> IAccessible.GetApplicationAsync() => Task.FromResult(ApplicationPath);
        Task<IDictionary<string, string>> IAccessible.GetAttributesAsync() => Task.FromResult(Attributes);

        Task<string> IApplication.GetLocaleAsync(uint lcType) => Task.FromResult(CultureInfo.CurrentCulture.Name);

        Task IApplication.RegisterEventListenerAsync(string Event)
        {
            throw new NotImplementedException();
        }

        Task IApplication.DeregisterEventListenerAsync(string Event)
        {
            throw new NotImplementedException();
        }

        Task<object?> IApplication.GetAsync(string prop)
        {
            return Task.FromResult<object?>(prop switch
            {
                nameof(ApplicationProperties.ToolkitName) => _applicationProperties!.ToolkitName,
                nameof(ApplicationProperties.Version) => _applicationProperties!.Version,
                nameof(ApplicationProperties.AtspiVersion) => _applicationProperties!.AtspiVersion,
                nameof(ApplicationProperties.Id) => _applicationProperties!.Id,
                _ => null,
            });
        }

        Task<ApplicationProperties> IApplication.GetAllAsync() => Task.FromResult(_applicationProperties!);

        Task IApplication.SetAsync(string prop, object val)
        {
            switch (prop)
            {
                case nameof(ApplicationProperties.Id):
                    _applicationProperties!.Id = (int)val;
                    break;
            }
            
            return Task.CompletedTask;
        }

        Task<IDisposable> IApplication.WatchPropertiesAsync(Action<PropertyChanges> handler)
        {
            return Task.FromResult(Disposable.Empty);
        }

        Task<object?> IAccessible.GetAsync(string prop)
        {
            return Task.FromResult<object?>(prop switch
            {
                nameof(AccessibleProperties.Name) => _accessibleProperties!.Name,
                nameof(AccessibleProperties.Description) => _accessibleProperties!.Description,
                nameof(AccessibleProperties.Parent) => _accessibleProperties!.Parent,
                nameof(AccessibleProperties.ChildCount) => _accessibleProperties!.ChildCount,
                nameof(AccessibleProperties.Locale) => _accessibleProperties!.Locale,
                nameof(AccessibleProperties.AccessibleId) => _accessibleProperties!.AccessibleId,
                _ => null,
            });
        }

        Task<AccessibleProperties> IAccessible.GetAllAsync() => Task.FromResult(_accessibleProperties!);

        Task IAccessible.SetAsync(string prop, object val)
        {
            throw new NotImplementedException();
        }

        Task<IDisposable> IAccessible.WatchPropertiesAsync(Action<PropertyChanges> handler)
        {
            return Task.FromResult(Disposable.Empty);
        }

        private class Child
        {
            private readonly Func<AutomationPeer> _peerGetter;
            public Child(Func<AutomationPeer> peerGetter) => _peerGetter = peerGetter;
            public AutomationPeer? Peer  {  get;  private set;  }

            public void CreatePeer()
            {
                Dispatcher.UIThread.VerifyAccess();
                Peer = _peerGetter();
            }
        }
    }
}

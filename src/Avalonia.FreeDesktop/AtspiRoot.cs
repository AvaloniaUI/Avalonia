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
using JetBrains.Annotations;
using Tmds.DBus;

#nullable enable

namespace Avalonia.FreeDesktop
{
    /// <summary>
    /// The root Application in the AT-SPI automation tree.
    /// </summary>
    /// <remarks>
    /// When using AT-SPI there is a single AT-SPI root object for the application. Its children are the application's
    /// open windows.
    /// </remarks>
    public class AtspiRoot : IAccessible, IApplication
    {
        private const string RootPath = "/org/a11y/atspi/accessible/root";
        private const string AtspiVersion = "2.1";

        // TODO: Not sure where to store this shared instance.
        private static AtspiRoot? _instance;
        private static bool _instanceInitialized;

        private readonly List<Child> _children = new List<Child>();
        private string? _applicationBusAddress;
        private Connection? _connection;
        private string? _localName;
        private AtspiCache? _cache;
        private AccessibleProperties? _accessibleProperties;
        private ApplicationProperties? _applicationProperties;

        public AtspiRoot(Connection sessionConnection)
        {
            Register(sessionConnection);
            Attributes = new Dictionary<string, string> { { "toolkit", "Avalonia" } };
        }

        public ObjectPath ObjectPath => RootPath;
        public ObjectReference ApplicationPath => new ObjectReference(LocalName, ObjectPath);
        public IDictionary<string, string> Attributes { get; }
        public string LocalName => _localName ?? throw new AvaloniaInternalException("AT-SPI not initialized.");

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
            if (_connection is null)
                throw new AvaloniaInternalException("AT-SPI not initialized.");
            
            var result = AtspiContextFactory.Create(this, peer);
            var _ = _connection.RegisterObjectAsync(result);
            _cache!.Add(result);
            System.Diagnostics.Debug.WriteLine($"Created {result.ObjectPath} for {peer}");
            return result;
        }

        private async void Register(Connection sessionConnection)
        {
            try
            {
                // Get the address of the a11y bus and open a connection to it.
                var bus = sessionConnection.CreateProxy<IBus>("org.a11y.Bus", "/org/a11y/bus");
                _applicationBusAddress = await bus.GetAddressAsync();

                var connection = new Connection(_applicationBusAddress);
                var connectionInfo = await connection.ConnectAsync();

                // Register the org.a11y.atspi.Application and org.a11y.atspi.Accessible interfaces at the well-known
                // object path.
                await connection.RegisterObjectAsync(this);
            
                // Get the a11y Register object's Socket interface.
                var socket = connection.CreateProxy<ISocket>("org.a11y.atspi.Registry", RootPath);
                
                // Set up our properties now as they can be read when we call Socket.Embed.
                _accessibleProperties = new AccessibleProperties
                {
                    Name = Application.Current.Name ?? "Unnamed",
                    Description = string.Empty,
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
                
                // Store the connection object and local name as the call to Socket.Embed will result in a call
                // to GetChildAtIndexAsync.
                _connection = connection;
                _localName = connectionInfo.LocalName;
                
                // Embed ourselves using the Socket.Embed method. We pass the local name (aka unique name) for the
                // connection along with the root a11y path and get back an object reference to the desktop object
                // (I think?).
                var plug = new ObjectReference(connectionInfo.LocalName, RootPath);
                _accessibleProperties.Parent = await socket.EmbedAsync(plug);
                
                // Create and register the cache.
                _cache = new AtspiCache(this);
                await connection.RegisterObjectAsync(_cache);
                
                System.Diagnostics.Debug.WriteLine($"Set up AtspiRoot on {LocalName}");
            }
            catch (Exception e)
            {
                Logger.TryGet(LogEventLevel.Error, LogArea.Control)?.Log(this, "Error connecting to AT-SPI bus: {Error}", e);
            }
        }

        async Task<ObjectReference> IAccessible.GetChildAtIndexAsync(int index)
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

            return new ObjectReference(LocalName, context.ObjectPath);
        }

        async Task<ObjectReference[]> IAccessible.GetChildrenAsync()
        {
            var result = new ObjectReference[_children.Count];
            
            for (var i = 0; i < _children.Count; ++i)
            {
                result[i] = await ((IAccessible)this).GetChildAtIndexAsync(i);
            }

            return result;
        }

        Task<int> IAccessible.GetIndexInParentAsync() => Task.FromResult(-1);

        Task<(uint, ObjectReference[])[]> IAccessible.GetRelationSetAsync()
        {
            return Task.FromResult(Array.Empty<(uint, ObjectReference[])>());            
        }

        Task<uint> IAccessible.GetRoleAsync() => Task.FromResult((uint)AtspiRole.ATSPI_ROLE_APPLICATION);
        Task<string> IAccessible.GetRoleNameAsync() => Task.FromResult("application");
        Task<string> IAccessible.GetLocalizedRoleNameAsync() => Task.FromResult("application");
        Task<uint[]> IAccessible.GetStateAsync() => Task.FromResult(new uint[] { 0, 0 });
        Task<ObjectReference> IAccessible.GetApplicationAsync() => Task.FromResult(ApplicationPath);
        Task<IDictionary<string, string>> IAccessible.GetAttributesAsync() => Task.FromResult(Attributes);

        Task<string> IApplication.GetApplicationBusAddressAsync() => Task.FromResult(_applicationBusAddress!);
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

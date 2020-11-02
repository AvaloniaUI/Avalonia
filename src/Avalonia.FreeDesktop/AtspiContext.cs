using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Avalonia.Controls.Automation.Peers;
using Avalonia.FreeDesktop.Atspi;
using Avalonia.Platform;
using Tmds.DBus;

#nullable enable

namespace Avalonia.FreeDesktop
{
    /// <summary>
    /// A node in the AT-SPI UI automation tree.
    /// </summary>
    /// <remarks>
    /// This class is the platform implementation for an <see cref="AutomationPeer"/> when using AT-SPI.
    /// </remarks>
    internal class AtspiContext : IAccessible, IAutomationPeerImpl
    {
        private static uint _id;
        private readonly AtspiRoot _root;
        private readonly AutomationPeer _peer;
        private readonly AtspiRole _role;
        
        public AtspiContext(AtspiRoot root, AutomationPeer peer, AtspiRole role)
        {
            _root = root;
            _peer = peer;
            _role = role;
            ObjectPath = new ObjectPath("/org/a11y/atspi/accessible/" + ++_id);
        }
        
        public ObjectPath ObjectPath { get; }

        public CacheItem ToCacheItem()
        {
            return new CacheItem(
                new ObjectReference(_root.LocalName, ObjectPath),
                _root.ApplicationPath,
                _root.ApplicationPath,
                new ObjectReference[0],
                new[] { "org.a11y.atspi.Accessible" },
                string.Empty,
                (uint)_role,
                string.Empty,
                new[] { 0, 0 });
        }
        
        Task<ObjectReference> IAccessible.GetChildAtIndexAsync(int Index)
        {
            throw new NotImplementedException();
        }

        Task<ObjectReference[]> IAccessible.GetChildrenAsync()
        {
            throw new NotImplementedException();
        }

        Task<int> IAccessible.GetIndexInParentAsync()
        {
            throw new NotImplementedException();
        }

        Task<(uint, ObjectReference[])[]> IAccessible.GetRelationSetAsync()
        {
            throw new NotImplementedException();
        }

        Task<uint> IAccessible.GetRoleAsync() => Task.FromResult((uint)_role);
        Task<string> IAccessible.GetRoleNameAsync() => Task.FromResult(_role.ToString()); // TODO
        Task<string> IAccessible.GetLocalizedRoleNameAsync() => Task.FromResult(_role.ToString()); // TODO
        Task<uint[]> IAccessible.GetStateAsync() => Task.FromResult(new uint[] { 0, 0 }); // TODO
        Task<IDictionary<string, string>> IAccessible.GetAttributesAsync() => Task.FromResult(_root.Attributes);
        Task<ObjectReference> IAccessible.GetApplicationAsync() => Task.FromResult(_root.ApplicationPath);

        Task<object?> IAccessible.GetAsync(string prop)
        {
            throw new NotImplementedException();
        }

        Task<AccessibleProperties> IAccessible.GetAllAsync()
        {
            throw new NotImplementedException();
        }

        Task IAccessible.SetAsync(string prop, object val)
        {
            return Task.FromResult(Disposable.Empty);
        }

        Task<IDisposable> IAccessible.WatchPropertiesAsync(Action<PropertyChanges> handler)
        {
            return Task.FromResult(Disposable.Empty);
        }
    }
}

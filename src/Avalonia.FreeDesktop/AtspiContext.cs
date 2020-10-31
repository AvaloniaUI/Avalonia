using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls.Automation.Peers;
using Avalonia.FreeDesktop.Atspi;
using Avalonia.Platform;
using Tmds.DBus;

#nullable enable

namespace Avalonia.FreeDesktop
{
    internal class AtspiContext : IAccessible, IAutomationPeerImpl
    {
        private readonly AtspiRoot _root;
        private readonly AutomationPeer _peer;
        private readonly AtspiRole _role;
        
        public AtspiContext(AtspiRoot root, AutomationPeer peer, AtspiRole role)
        {
            _root = root;
            _peer = peer;
            _role = role;
            ObjectPath = new ObjectPath("/net/avaloniaui/a11y/" + Guid.NewGuid().ToString().Replace("-", ""));
        }
        
        public ObjectPath ObjectPath { get; }

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
            throw new NotImplementedException();
        }

        Task<IDisposable> IAccessible.WatchPropertiesAsync(Action<PropertyChanges> handler)
        {
            throw new NotImplementedException();
        }
    }
}

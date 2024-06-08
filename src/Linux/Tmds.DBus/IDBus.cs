// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System.Threading;
using System.Threading.Tasks;

namespace Tmds.DBus
{
    [DBusInterface(DBusConnection.DBusInterface)]
    internal interface IDBus : IDBusObject
    {
        Task<string[]> ListActivatableNamesAsync();
		Task<bool> NameHasOwnerAsync(string name);
        Task<ServiceStartResult> StartServiceByNameAsync(string name, uint flags);
        Task<string> GetNameOwnerAsync(string name);
        Task<string[]> ListNamesAsync();
    }
}

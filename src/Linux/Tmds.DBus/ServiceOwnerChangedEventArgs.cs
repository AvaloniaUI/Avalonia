// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tmds.DBus
{
    /// <summary>
    /// Event data for the ServiceOwnerChanged event.
    /// </summary>
    public struct ServiceOwnerChangedEventArgs
    {
        /// <summary>
        /// Creates an instance of ServiceOwnerChangedEventArgs.
        /// </summary>
        /// <param name="serviceName">The name of the service.</param>
        /// <param name="oldOwner">The previous owner of the service.</param>
        /// <param name="newOwner">The new owner of the service.</param>
        public ServiceOwnerChangedEventArgs(string serviceName, string oldOwner, string newOwner)
        {
            ServiceName = serviceName;
            OldOwner = oldOwner;
            NewOwner = newOwner;
        }

        /// <summary>
        /// Name of the service.
        /// </summary>
        public string ServiceName { get; }

        /// <summary>
        /// Local name of the previous owner. <c>null</c> when there is no previous owner.
        /// </summary>
        public string OldOwner { get; internal set; }

        /// <summary>
        /// Local name of the new owner. <c>null</c> when there is no new owner.
        /// </summary>
        public string NewOwner { get; }
    }
}

// Copyright 2017 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tmds.DBus
{
    /// <summary>
    /// Options that configure the behavior of a Connection.
    /// </summary>
    public abstract class ConnectionOptions
    {
        internal ConnectionOptions()
        {}

        /// <summary>
        /// SynchronizationContext used for event handlers and callbacks.
        /// </summary>
        public SynchronizationContext SynchronizationContext { get; set; }
    }
}
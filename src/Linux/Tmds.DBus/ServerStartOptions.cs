// Copyright 2017 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Tmds.DBus
{
    /// <summary>
    /// Options that configure the behavior of ServerConnectionOptions.Start.
    /// </summary>
    public class ServerStartOptions : ConnectionOptions
    {
        /// <summary>
        /// Listen address (e.g. 'tcp:host=localhost').
        /// </summary>
        public string Address { get; set; }
    }
}
// Copyright 2017 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Threading;

namespace Tmds.DBus
{
    /// <summary>
    /// Result of ClientConnectionOptions.SetupAsync
    /// </summary>
    public class ClientSetupResult
    {
        /// <summary>
        /// Address of the D-Bus peer.
        /// </summary>
        public string ConnectionAddress { get; set; }

        /// <summary>
        /// Object passed to ConnectionOptions.Teardown.
        /// </summary>
        public object TeardownToken { get; set; }

        /// <summary>
        /// Authentication User ID (Linux UID).
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Indicates whether the connection supports Fd passing.
        /// </summary>
        public bool SupportsFdPassing { get; set; }
    }
}
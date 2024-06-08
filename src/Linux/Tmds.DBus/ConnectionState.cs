// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

namespace Tmds.DBus
{
    /// <summary>
    /// State of the Connection.
    /// </summary>
    public enum ConnectionState
    {
        /// <summary>No connection attempt has been made.</summary>
        Created,
        /// <summary>Connecting to remote peer.</summary>
        Connecting,
        /// <summary>Connection established.</summary>
        Connected,
        /// <summary>Connection is closing.</summary>
        Disconnecting,
        /// <summary>Connection is closed.</summary>
        Disconnected
    }
}

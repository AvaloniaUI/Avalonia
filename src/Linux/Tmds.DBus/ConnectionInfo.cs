// Copyright 2017 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

namespace Tmds.DBus
{
    /// <summary>
    /// Information about established Connection.
    /// </summary>
    public class ConnectionInfo
    {
        /// <summary>
        /// Creates an instance of ConnectionInfo.
        /// </summary>
        /// <param name="localName">Name assigned by the bus to the connection.</param>
        public ConnectionInfo(string localName)
        {
            LocalName = localName;
        }

        /// <summary>
        /// Local name assigned by the bus to the connection.
        /// </summary>
        public string LocalName { get; }

        /// <summary>
        /// Returns whether the remote peer is a bus.
        /// </summary>
        public bool RemoteIsBus => !string.IsNullOrEmpty(LocalName);
    }
}

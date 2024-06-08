// Copyright 2017 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;

namespace Tmds.DBus
{
    /// <summary>
    /// Event data for the Connection StateChanged event.
    /// </summary>
    public struct ConnectionStateChangedEventArgs
    {
        /// <summary>
        /// Creates an instance of ConnectionStateChangedEventArgs.
        /// </summary>
        /// <param name="state">State of the connection.</param>
        /// <param name="disconnectReason">Reason the connection closed.</param>
        /// <param name="connectionInfo">Information about established connection.</param>
        public ConnectionStateChangedEventArgs(ConnectionState state, Exception disconnectReason, ConnectionInfo connectionInfo)
        {
            State = state;
            DisconnectReason = disconnectReason;
            ConnectionInfo = connectionInfo;
        }

        /// <summary>
        /// ConnectionInfo for established connection.
        /// </summary>
        /// <remarks>
        /// This property is set for the Connected event.
        /// </remarks>
        public ConnectionInfo ConnectionInfo { get; }

        /// <summary>
        /// New connection state.
        /// </summary>
        public ConnectionState State { get; }

        /// <summary>
        /// Reason the connection closed.
        /// </summary>
        /// <remarks>
        /// This property is set for the Disconnecting, Disconnected and following Connecting event.
        /// </remarks>
        public Exception DisconnectReason { get; }
    }
}

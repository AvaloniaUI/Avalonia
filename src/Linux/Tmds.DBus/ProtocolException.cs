// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;

namespace Tmds.DBus
{
    /// <summary>
    /// Exception thrown when there is an error in the D-Bus protocol.
    /// </summary>
    public class ProtocolException : Exception
    {
        /// <summary>
        /// Creates an instance of the ProtocolException with the specified message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public ProtocolException(string message) : base(message)
        {}
    }
}

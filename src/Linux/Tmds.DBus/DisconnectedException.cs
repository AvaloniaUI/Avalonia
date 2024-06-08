// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;

namespace Tmds.DBus
{
    /// <summary>
    /// Exception thrown when the D-Bus connection was closed after being succesfully established. When the connection is
    /// closed during the connect operation, ConnectException is thrown instead.
    /// </summary>
    public class DisconnectedException : Exception
    {
        internal DisconnectedException(Exception innerException) : base(innerException.Message, innerException)
        { }
    }
}

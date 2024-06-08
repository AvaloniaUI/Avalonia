// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;

namespace Tmds.DBus
{
    /// <summary>
    /// Exception thrown when the D-Bus connection cannot be succesfully established.
    /// </summary>
    public class ConnectException : Exception
    {
        /// <summary>
        /// Creates an instance of the ConnectException with the specified message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public ConnectException(string message) : base(message)
        { }

        /// <summary>
        /// Creates an instance of the ConnectException with the specified message and innerException.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception..</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        public ConnectException(string message, Exception innerException) : base(message, innerException)
        { }
    }
}
// Copyright 2006 Alp Toker <alp@atoker.com>
// Copyright 2010 Alan McGovern <alan.mcgovern@gmail.com>
// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;

namespace Tmds.DBus
{
    /// <summary>
    /// Represents the D-Bus error message which is used to signal the unsuccesfull invocation of a method.
    /// </summary>
    public class DBusException : Exception
    {
        /// <summary>
        /// Creates a new DBusException with the given name and message.
        /// </summary>
        /// <param name="errorName">Name of the error</param>
        /// <param name="errorMessage">Message of the error</param>
        public DBusException(string errorName, string errorMessage) :
            base($"{errorName}: {errorMessage}")
        {
            ErrorName = errorName;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Name of the error.
        /// </summary>
        public string ErrorName { get; }

        /// <summary>
        /// Message of the error.
        /// </summary>
        public string ErrorMessage { get; }
    }
}
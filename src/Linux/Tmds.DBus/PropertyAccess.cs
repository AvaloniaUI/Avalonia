// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

namespace Tmds.DBus
{
    /// <summary>
    /// The mutability of a property
    /// </summary>
    public enum PropertyAccess
    {
        /// <summary>
        /// Allows the property to be read and written
        /// </summary>
        ReadWrite,
        /// <summary>
        /// Allows the property to only be read
        /// </summary>
        Read,
        /// <summary>
        /// Allows the property to only be written to
        /// </summary>
        Write
    }
}

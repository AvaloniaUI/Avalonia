// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;

namespace Tmds.DBus
{
    /// <summary>
    /// Indicates the method return type or signal type represents a single D-Bus argument.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false, Inherited = true)]
    public sealed class ArgumentAttribute : Attribute
    {
        /// <summary>
        /// Name of the argument.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Creates an instance of the ArgumentAttribute with the specified name.
        /// </summary>
        /// <param name="name">Name of the argument.</param>
        public ArgumentAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Creates an instance of the ArgumentAttribute.
        /// </summary>
        public ArgumentAttribute()
        {
            Name = "value";
        }
    }
}

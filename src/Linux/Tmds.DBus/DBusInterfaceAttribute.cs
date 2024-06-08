// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;

namespace Tmds.DBus
{
    /// <summary>
    /// Provides information for mapping the C# interface to a D-Bus interface.
    /// </summary>    
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class DBusInterfaceAttribute : Attribute
    {
        /// <summary>
        /// Name of the D-Bus interface.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Method name of the property get method. Defaults to <c>GetAsync</c>.
        /// </summary>
        public string GetPropertyMethod { get; set; }

        /// <summary>
        /// Method name of the property get method. Defaults to <c>SetAsync</c>.
        /// </summary>
        public string SetPropertyMethod { get; set; }

        /// <summary>
        /// Method name of the property get all method. Defaults to <c>GetAllAsync</c>.
        /// </summary>
        public string GetAllPropertiesMethod { get; set; }

        /// <summary>
        /// Method name of the property get all method. Defaults to <c>WatchPropertiesAsync</c>.
        /// </summary>
        public string WatchPropertiesMethod { get; set; }

        /// <summary>
        /// Set to a type decorated with the Dictionary attribute used to provide property introspection information. When unset the type returned by the <c>GetAllPropertiesMethod</c> is used.
        /// </summary>
        public Type PropertyType { get; set; }

        /// <summary>
        /// Creates a DBusInterfaceAttribute with the specified D-Bus interface name.
        /// </summary>
        /// <param name="name">D-Bus interface name</param>
        public DBusInterfaceAttribute(string name)
        {
            Name = name;
            GetAllPropertiesMethod = "GetAllAsync";
            SetPropertyMethod = "SetAsync";
            GetPropertyMethod = "GetAsync";
            WatchPropertiesMethod = "WatchPropertiesAsync";
            PropertyType = null;
        }
    }
}

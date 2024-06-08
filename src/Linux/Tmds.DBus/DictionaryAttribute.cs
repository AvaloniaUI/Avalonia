// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;

namespace Tmds.DBus
{
    /// <summary>
    /// Indicates the type must be marshalled as a D-Bus dictionary of <c>a{sv}</c>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class DictionaryAttribute : Attribute
    {}
}

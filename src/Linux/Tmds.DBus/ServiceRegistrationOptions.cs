// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;

namespace Tmds.DBus
{
    /// <summary>
    /// Options for service name registration.
    /// </summary>
    [Flags]
    public enum ServiceRegistrationOptions
    {
        /// <summary>No options.</summary>
        None = 0,
        /// <summary>Replace the existing owner.</summary>
        ReplaceExisting = 1,
        /// <summary>Allow registration to be replaced.</summary>
        AllowReplacement = 2,
        /// <summary>Default (<c>ReplaceExisting | AllowReplacement</c>)</summary>
        Default = ReplaceExisting | AllowReplacement
    }
}

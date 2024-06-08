// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

namespace Tmds.DBus
{
    /// <summary>
    /// Result of the service activation request.
    /// </summary>
    public enum ServiceStartResult : uint
    {
        /// <summary>The service was started.</summary>
        Started = 1,
        /// <summary>The service was already running.</summary>
        AlreadyRunning = 2
    }
}

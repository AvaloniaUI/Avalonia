// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System.Runtime.InteropServices;

namespace Tmds.DBus.Protocol
{
    internal struct UnixFd
    {
        public SafeHandle SafeHandle { get; private set; }
        public int Handle { get; private set; }

        public UnixFd(SafeHandle handle)
        {
            SafeHandle = handle;
            Handle = SafeHandle.DangerousGetHandle().ToInt32();
        }

        public UnixFd(int handle)
        {
            SafeHandle = null;
            Handle = handle;
        }
    }
}
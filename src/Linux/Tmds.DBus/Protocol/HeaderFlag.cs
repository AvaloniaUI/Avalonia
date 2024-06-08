// Copyright 2006 Alp Toker <alp@atoker.com>
// Copyright 2010 Alan McGovern <alan.mcgovern@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;

namespace Tmds.DBus.Protocol
{
    [Flags]
    internal enum HeaderFlag : byte
    {
        None = 0,
        NoReplyExpected = 0x1,
        NoAutoStart = 0x2,
    }
}

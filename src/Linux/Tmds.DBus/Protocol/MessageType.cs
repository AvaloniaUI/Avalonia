// Copyright 2006 Alp Toker <alp@atoker.com>
// Copyright 2010 Alan McGovern <alan.mcgovern@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;

namespace Tmds.DBus.Protocol
{
    internal enum MessageType : byte
    {
        //This is an invalid type.
        Invalid,
        //Method call.
        MethodCall,
        //Method reply with returned data.
        MethodReturn,
        //Error reply. If the first argument exists and is a string, it is an error message.
        Error,
        //Signal emission.
        Signal,
        All
        // Correspond to all types
    }
}

// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

namespace Tmds.DBus
{
    internal enum RequestNameReply : uint
    {
        PrimaryOwner = 1,
        InQueue,
        Exists,
        AlreadyOwner,
    }
}

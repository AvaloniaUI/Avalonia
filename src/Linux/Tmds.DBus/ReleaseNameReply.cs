// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

namespace Tmds.DBus
{
    internal enum ReleaseNameReply : uint
    {
        ReplyReleased = 1,
        NonExistent,
        NotOwner
    }
}

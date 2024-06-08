// Copyright 2006 Alp Toker <alp@atoker.com>
// Copyright 2010 Alan McGovern <alan.mcgovern@gmail.com>
// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

namespace Tmds.DBus.Protocol
{
    internal enum DType : byte
    {
        Invalid = (byte)'\0',

        Byte = (byte)'y',
        Boolean = (byte)'b',
        Int16 = (byte)'n',
        UInt16 = (byte)'q',
        Int32 = (byte)'i',
        UInt32 = (byte)'u',
        Int64 = (byte)'x',
        UInt64 = (byte)'t',
        Single = (byte)'f', //This is not yet supported!
        Double = (byte)'d',
        String = (byte)'s',
        ObjectPath = (byte)'o',
        Signature = (byte)'g',
        UnixFd = (byte)'h',
        Array = (byte)'a',
        Variant = (byte)'v',

        StructBegin = (byte)'(',
        StructEnd = (byte)')',
        DictEntryBegin = (byte)'{',
        DictEntryEnd = (byte)'}',
    }
}

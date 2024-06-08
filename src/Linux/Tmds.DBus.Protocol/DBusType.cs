namespace Tmds.DBus.Protocol;

public enum DBusType : byte
{
    Invalid = 0,
    Byte = (byte)'y',
    Bool = (byte)'b',
    Int16 = (byte)'n',
    UInt16 = (byte)'q',
    Int32 = (byte)'i',
    UInt32 = (byte)'u',
    Int64 = (byte)'x',
    UInt64 = (byte)'t',
    Double = (byte)'d',
    String = (byte)'s',
    ObjectPath = (byte)'o',
    Signature = (byte)'g',
    Array = (byte)'a',
    Struct = (byte)'(',
    Variant = (byte)'v',
    DictEntry = (byte)'{',
    UnixFd = (byte)'h',
}
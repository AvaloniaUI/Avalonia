namespace Tmds.DBus.Protocol;

public enum MessageType : byte
{
    MethodCall = 1,
    MethodReturn = 2,
    Error = 3,
    Signal = 4
}
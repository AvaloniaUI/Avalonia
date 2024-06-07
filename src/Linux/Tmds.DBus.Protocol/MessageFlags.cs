namespace Tmds.DBus.Protocol;

[Flags]
public enum MessageFlags : byte
{
    None = 0,
    NoReplyExpected = 1,
    NoAutoStart = 2,
    AllowInteractiveAuthorization = 4
}
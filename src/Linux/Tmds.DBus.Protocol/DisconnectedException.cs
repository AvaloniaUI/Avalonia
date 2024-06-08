namespace Tmds.DBus.Protocol;
public class DisconnectedException : Exception
{
    internal DisconnectedException(Exception innerException) : base(innerException.Message, innerException)
    { }
}

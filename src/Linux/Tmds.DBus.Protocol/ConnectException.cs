namespace Tmds.DBus.Protocol;

public class ConnectException : Exception
{
    public ConnectException(string message) : base(message)
    { }

    public ConnectException(string message, Exception innerException) : base(message, innerException)
    { }
}
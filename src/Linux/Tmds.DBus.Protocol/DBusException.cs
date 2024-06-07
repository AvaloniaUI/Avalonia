namespace Tmds.DBus.Protocol;

public class DBusException : Exception
{
    public DBusException(string errorName, string errorMessage) :
        base($"{errorName}: {errorMessage}")
    {
        ErrorName = errorName;
        ErrorMessage = errorMessage;
    }

    public string ErrorName { get; }

    public string ErrorMessage { get; }
}

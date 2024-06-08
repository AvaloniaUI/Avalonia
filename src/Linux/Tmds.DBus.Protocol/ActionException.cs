namespace Tmds.DBus.Protocol;

public static class ActionException
{
    // Exception used when the IDisposable returned by AddMatchAsync gets disposed.
    public static bool IsObserverDisposed(Exception exception)
        => object.ReferenceEquals(exception, DBusConnection.ObserverDisposedException);

    // Exception used when the Connection gets disposed.
    public static bool IsConnectionDisposed(Exception exception)
        // note: Connection.DisposedException is only ever used as an InnerException of DisconnectedException,
        //       so we directly check for that.
        => object.ReferenceEquals(exception?.InnerException, Connection.DisposedException);

    public static bool IsDisposed(Exception exception)
        => IsObserverDisposed(exception) || IsConnectionDisposed(exception);
}
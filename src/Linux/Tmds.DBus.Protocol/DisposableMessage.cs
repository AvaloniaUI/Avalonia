namespace Tmds.DBus.Protocol;

public struct DisposableMessage : IDisposable
{
    private Message? _message;

    internal DisposableMessage(Message? message)
        => _message = message;

    public Message Message
        => _message ?? throw new ObjectDisposedException(typeof(Message).FullName);

    public void Dispose()
    {
        _message?.ReturnToPool();
        _message = null;
    }
}

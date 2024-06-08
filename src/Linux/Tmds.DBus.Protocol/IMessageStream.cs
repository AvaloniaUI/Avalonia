namespace Tmds.DBus.Protocol;

interface IMessageStream
{
    public delegate void MessageReceivedHandler<T>(Exception? closeReason, Message message, T state);

    void ReceiveMessages<T>(MessageReceivedHandler<T> handler, T state);

    ValueTask<bool> TrySendMessageAsync(MessageBuffer message);

    void BecomeMonitor();

    void Close(Exception closeReason);
}
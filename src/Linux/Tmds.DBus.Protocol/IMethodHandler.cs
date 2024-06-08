namespace Tmds.DBus.Protocol;

public interface IMethodHandler
{
    // Path that is handled by this method handler.
    string Path { get; }

    // The message argument is only valid during the call. It must not be stored to extend its lifetime.
    ValueTask HandleMethodAsync(MethodContext context);

    // Controls whether to wait for the handler method to finish executing before reading more messages.
    bool RunMethodHandlerSynchronously(Message message);
}

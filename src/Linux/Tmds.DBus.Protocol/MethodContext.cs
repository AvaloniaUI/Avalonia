namespace Tmds.DBus.Protocol;

public class MethodContext
{
    internal MethodContext(Connection connection, Message request, CancellationToken requestAborted)
    {
        Connection = connection;
        Request = request;
        RequestAborted = requestAborted;
    }

    public Message Request { get; }
    public Connection Connection { get; }
    public CancellationToken RequestAborted { get; }

    public bool ReplySent { get; private set; }

    public bool NoReplyExpected => (Request.MessageFlags & MessageFlags.NoReplyExpected) != 0;

    public bool IsDBusIntrospectRequest { get; internal set; }

    internal List<string>? IntrospectChildNameList { get; set; }

    public MessageWriter CreateReplyWriter(string? signature)
    {
        var writer = Connection.GetMessageWriter();
        writer.WriteMethodReturnHeader(
            replySerial: Request.Serial,
            destination: Request.Sender,
            signature: signature
        );
        return writer;
    }

    public void Reply(MessageBuffer message)
    {
        if (ReplySent || NoReplyExpected)
        {
            message.Dispose();
            if (ReplySent)
            {
                throw new InvalidOperationException("A reply has already been sent.");
            }
        }

        ReplySent = true;
        Connection.TrySendMessage(message);
    }

    public void ReplyError(string? errorName = null,
                           string? errorMsg = null)
    {
        using var writer = Connection.GetMessageWriter();
        writer.WriteError(
            replySerial: Request.Serial,
            destination: Request.Sender,
            errorName: errorName,
            errorMsg: errorMsg
        );
        Reply(writer.CreateMessage());
    }

    public void ReplyIntrospectXml(ReadOnlySpan<ReadOnlyMemory<byte>> interfaceXmls)
    {
        if (!IsDBusIntrospectRequest)
        {
            throw new InvalidOperationException($"Can not reply with introspection XML when {nameof(IsDBusIntrospectRequest)} is false.");
        }

        using var writer = Connection.GetMessageWriter();
        writer.WriteMethodReturnHeader(
            replySerial: Request.Serial,
            destination: Request.Sender,
            signature: "s"
        );

        // Add the Peer and Introspectable interfaces.
        // Tools like D-Feet will list the paths separately as soon as there is an interface.
        // We add the base interfaces only for the paths that we want to show up.
        // Those are paths that have other interfaces, paths that are leaves.
        bool includeBaseInterfaces = !interfaceXmls.IsEmpty || IntrospectChildNameList is null || IntrospectChildNameList.Count == 0;
        ReadOnlySpan<ReadOnlyMemory<byte>> baseInterfaceXmls = includeBaseInterfaces ? [ IntrospectionXml.DBusIntrospectable, IntrospectionXml.DBusPeer ] : [ ];

        // Add the child names.
#if NET5_0_OR_GREATER
        ReadOnlySpan<string> childNames = CollectionsMarshal.AsSpan(IntrospectChildNameList);
        IEnumerable<string>? childNamesEnumerable = null;
#else
        ReadOnlySpan<string> childNames = default;
        IEnumerable<string>? childNamesEnumerable = IntrospectChildNameList;
#endif

        writer.WriteIntrospectionXml(interfaceXmls, baseInterfaceXmls, childNames, childNamesEnumerable);

        Reply(writer.CreateMessage());
    }
}
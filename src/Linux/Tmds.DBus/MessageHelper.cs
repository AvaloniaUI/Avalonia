// Copyright 2006 Alp Toker <alp@atoker.com>
// Copyright 2010 Alan McGovern <alan.mcgovern@gmail.com>
// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using Tmds.DBus.Protocol;

namespace Tmds.DBus
{
    internal static class MessageHelper
    {
        public static Message ConstructErrorReply(Message incoming, string errorName, string errorMessage)
        {
            MessageWriter writer = new MessageWriter(incoming.Header.Endianness);
            writer.WriteString(errorMessage);

            Message replyMessage = new Message(
                new Header(MessageType.Error)
                {
                    ErrorName = errorName,
                    ReplySerial = incoming.Header.Serial,
                    Signature = Signature.StringSig,
                    Destination = incoming.Header.Sender
                },
                writer.ToArray(),
                writer.UnixFds
            );

            return replyMessage;
        }

        public static Message ConstructReply(Message msg, params object[] vals)
        {
            Signature inSig = Signature.GetSig(vals);

            MessageWriter writer = null;
            if (vals != null && vals.Length != 0)
            {
                writer = new MessageWriter(Environment.NativeEndianness);

                foreach (object arg in vals)
                    writer.Write(arg.GetType(), arg, isCompileTimeType: false);
            }

            Message replyMsg = new Message(
                new Header(MessageType.MethodReturn)
                {
                    ReplySerial = msg.Header.Serial,
                    Signature = inSig
                },
                writer?.ToArray(),
                writer?.UnixFds
            );

            if (msg.Header.Sender != null)
                replyMsg.Header.Destination = msg.Header.Sender;

            return replyMsg;
        }
    }
}

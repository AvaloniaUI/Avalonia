// Copyright 2006 Alp Toker <alp@atoker.com>
// Copyright 2010 Alan McGovern <alan.mcgovern@gmail.com>
// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;

namespace Tmds.DBus.Protocol
{
    internal class Header
    {
        public Header(MessageType type, EndianFlag endianness)
        {
            MessageType = type;
            Endianness = endianness;
            if (type == MessageType.MethodCall)
                ReplyExpected = true;
            else
                Flags = HeaderFlag.NoReplyExpected | HeaderFlag.NoAutoStart;
            MajorVersion = ProtocolInformation.Version;
        }

        public Header(MessageType type) :
            this(type, Environment.NativeEndianness)
        {}

        public EndianFlag Endianness { get; private set; }
        public MessageType MessageType { get; private set; }
        public HeaderFlag Flags { get; private set; }
        public byte MajorVersion { get; private set; }
        public uint Length { get; set; }

        public bool ReplyExpected
        {
            get
            {
                return (Flags & HeaderFlag.NoReplyExpected) == HeaderFlag.None;
            }
            set
            {
                if (value)
                    Flags &= ~HeaderFlag.NoReplyExpected;
                else
                    Flags |= HeaderFlag.NoReplyExpected;
            }
        }

        public uint Serial { get; set; }
        public ObjectPath2? Path { get; set; }
        public string Interface { get; set; }
        public string Member { get; set; }
        public string ErrorName { get; set; }
        public uint? ReplySerial { get; set; }
        public string Destination { get; set; }
        public string Sender { get; set; }
        public Signature? Signature { get; set; }
        public uint NumberOfFds { get; set; }

        public static Header FromBytes(ArraySegment<byte> data)
        {
            Header header = new Header();
            EndianFlag endianness = (EndianFlag)data.Array[data.Offset + 0];

            header.Endianness = endianness;
            header.MessageType = (MessageType)data.Array[data.Offset + 1];
            header.Flags = (HeaderFlag)data.Array[data.Offset + 2];
            header.MajorVersion = data.Array[data.Offset + 3];

            var reader = new MessageReader(endianness, data);
            reader.Seek(4);
            header.Length = reader.ReadUInt32();
            header.Serial = reader.ReadUInt32();

            FieldCodeEntry[] fields = reader.ReadArray<FieldCodeEntry>();
            foreach (var f in fields)
            {
                var fieldCode = f.Code;
                var value = f.Value;
                switch (fieldCode)
                {
                    case FieldCode.Path:
                        header.Path = (ObjectPath2)value;
                        break;
                    case FieldCode.Interface:
                        header.Interface = (string)value;
                        break;
                    case FieldCode.Member:
                        header.Member = (string)value;
                        break;
                    case FieldCode.ErrorName:
                        header.ErrorName = (string)value;
                        break;
                    case FieldCode.ReplySerial:
                        header.ReplySerial = (uint)value;
                        break;
                    case FieldCode.Destination:
                        header.Destination = (string)value;
                        break;
                    case FieldCode.Sender:
                        header.Sender = (string)value;
                        break;
                    case FieldCode.Signature:
                        header.Signature = (Signature)value;
                        break;
                    case FieldCode.UnixFds:
                        header.NumberOfFds = (uint)value;
                        break;
                }
            }

            return header;
        }

        public byte[] ToArray()
        {
            MessageWriter writer = new MessageWriter(Endianness);
            writer.WriteByte((byte)Endianness);
            writer.WriteByte((byte)MessageType);
            writer.WriteByte((byte)Flags);
            writer.WriteByte(MajorVersion);
            writer.WriteUInt32(Length);
            writer.WriteUInt32(Serial);
            writer.WriteHeaderFields(GetFields());
            writer.CloseWrite();
            return writer.ToArray();
        }

        public IEnumerable<KeyValuePair<FieldCode, object>> GetFields()
        {
            if (Path != null)
            {
                yield return new KeyValuePair<FieldCode, object>(FieldCode.Path, Path.Value);
            }
            if (Interface != null)
            {
                yield return new KeyValuePair<FieldCode, object>(FieldCode.Interface, Interface);
            }
            if (Member != null)
            {
                yield return new KeyValuePair<FieldCode, object>(FieldCode.Member, Member);
            }
            if (ErrorName != null)
            {
                yield return new KeyValuePair<FieldCode, object>(FieldCode.ErrorName, ErrorName);
            }
            if (ReplySerial != null)
            {
                yield return new KeyValuePair<FieldCode, object>(FieldCode.ReplySerial, ReplySerial);
            }
            if (Destination != null)
            {
                yield return new KeyValuePair<FieldCode, object>(FieldCode.Destination, Destination);
            }
            if (Sender != null)
            {
                yield return new KeyValuePair<FieldCode, object>(FieldCode.Sender, Sender);
            }
            if (Signature != null)
            {
                yield return new KeyValuePair<FieldCode, object>(FieldCode.Signature, Signature.Value);
            }
            if (NumberOfFds != 0)
            {
                yield return new KeyValuePair<FieldCode, object>(FieldCode.UnixFds, NumberOfFds);
            }
        }

        private Header()
        { }
#pragma warning disable 0649 // Field is never assigned to, and will always have its default value
        private struct FieldCodeEntry
        {
            public FieldCode Code;
            public object Value;
        }
#pragma warning restore
    }
}

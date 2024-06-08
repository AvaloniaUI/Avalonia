// Copyright 2006 Alp Toker <alp@atoker.com>
// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

#pragma warning disable 0618 // 'Marshal.SizeOf(Type)' is obsolete

using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Linq;
using Tmds.DBus.CodeGen;

namespace Tmds.DBus.Protocol
{
    internal sealed class MessageWriter
    {
        EndianFlag endianness;
        MemoryStream stream;
        List<UnixFd> fds;
        bool  _skipNextStructPadding;

        static readonly Encoding stringEncoding = Encoding.UTF8;

        //a default constructor is a bad idea for now as we want to make sure the header and content-type match
        public MessageWriter () : this (Environment.NativeEndianness) {}

        public MessageWriter (EndianFlag endianness)
        {
            this.endianness = endianness;
            stream = new MemoryStream ();
        }

        public void SetSkipNextStructPadding()
        {
            _skipNextStructPadding = true;
        }

        public byte[] ToArray ()
        {
            return stream.ToArray ();
        }

        public UnixFd[] UnixFds => fds?.ToArray() ?? Array.Empty<UnixFd>();

        public void CloseWrite ()
        {
            WritePad (8);
        }

        public void WriteByte (byte val)
        {
            stream.WriteByte (val);
        }

        public void WriteBoolean (bool val)
        {
            WriteUInt32 ((uint) (val ? 1 : 0));
        }

        // Buffer for integer marshaling
        byte[] dst = new byte[8];
        private unsafe void MarshalUShort (void* dataPtr)
        {
            WritePad (2);

            if (endianness == Environment.NativeEndianness) {
                fixed (byte* p = &dst[0])
                    *((ushort*)p) = *((ushort*)dataPtr);
            } else {
                byte* data = (byte*)dataPtr;
                dst[0] = data[1];
                dst[1] = data[0];
            }

            stream.Write (dst, 0, 2);
        }

        unsafe public void WriteInt16 (short val)
        {
            MarshalUShort (&val);
        }

        unsafe public void WriteUInt16 (ushort val)
        {
            MarshalUShort (&val);
        }

        private unsafe void MarshalUInt (void* dataPtr)
        {
            WritePad (4);

            if (endianness == Environment.NativeEndianness) {
                fixed (byte* p = &dst[0])
                    *((uint*)p) = *((uint*)dataPtr);
            } else {
                byte* data = (byte*)dataPtr;
                dst[0] = data[3];
                dst[1] = data[2];
                dst[2] = data[1];
                dst[3] = data[0];
            }

            stream.Write (dst, 0, 4);
        }

        unsafe public void WriteInt32 (int val)
        {
            MarshalUInt (&val);
        }

        unsafe public void WriteUInt32 (uint val)
        {
            MarshalUInt (&val);
        }

        private unsafe void MarshalULong (void* dataPtr)
        {
            WritePad (8);

            if (endianness == Environment.NativeEndianness) {
                fixed (byte* p = &dst[0])
                    *((ulong*)p) = *((ulong*)dataPtr);
            } else {
                byte* data = (byte*)dataPtr;
                for (int i = 0; i < 8; ++i)
                    dst[i] = data[7 - i];
            }

            stream.Write (dst, 0, 8);
        }

        unsafe public void WriteInt64 (long val)
        {
            MarshalULong (&val);
        }

        unsafe public void WriteUInt64 (ulong val)
        {
            MarshalULong (&val);
        }

        unsafe public void WriteSingle (float val)
        {
            MarshalUInt (&val);
        }

        unsafe public void WriteDouble (double val)
        {
            MarshalULong (&val);
        }

        public void WriteString (string val)
        {
            byte[] utf8_data = stringEncoding.GetBytes (val);
            WriteUInt32 ((uint)utf8_data.Length);
            stream.Write (utf8_data, 0, utf8_data.Length);
            WriteNull ();
        }

        public void WriteObjectPath (ObjectPath2 val)
        {
            WriteString (val.Value);
        }

        public void WriteSignature(Signature val)
        {
            byte[] ascii_data = val.GetBuffer ();

            if (ascii_data.Length > ProtocolInformation.MaxSignatureLength)
                throw new ProtocolException("Signature length " + ascii_data.Length + " exceeds maximum allowed " + ProtocolInformation.MaxSignatureLength + " bytes");

            WriteByte ((byte)ascii_data.Length);
            stream.Write (ascii_data, 0, ascii_data.Length);
            WriteNull ();
        }

        public void WriteSafeHandle(SafeHandle handle)
        {
            if (fds == null)
            {
                fds = new List<UnixFd>();
            }
            fds.Add(new UnixFd(handle));
            WriteInt32(fds.Count - 1);
        }

        public void Write(Type type, object val, bool isCompileTimeType)
        {
            if (type.GetTypeInfo().IsEnum)
            {
                type = Enum.GetUnderlyingType(type);
            }

            if (type == typeof(bool))
            {
                WriteBoolean((bool)val);
                return;
            }
            else if (type == typeof(byte))
            {
                WriteByte((byte)val);
                return;
            }
            else if (type == typeof(double))
            {
                WriteDouble((double)val);
                return;
            }
            else if (type == typeof(short))
            {
                WriteInt16((short)val);
                return;
            }
            else if (type == typeof(int))
            {
                WriteInt32((int)val);
                return;
            }
            else if (type == typeof(long))
            {
                WriteInt64((long)val);
                return;
            }
            else if (type == typeof(ObjectPath2))
            {
                WriteObjectPath((ObjectPath2)val);
                return;
            }
            else if (type == typeof(Signature))
            {
                WriteSignature((Signature)val);
                return;
            }
            else if (type == typeof(string))
            {
                WriteString((string)val);
                return;
            }
            else if (type == typeof(float))
            {
                WriteSingle((float)val);
                return;
            }
            else if (type == typeof(ushort))
            {
                WriteUInt16((ushort)val);
                return;
            }
            else if (type == typeof(uint))
            {
                WriteUInt32((uint)val);
                return;
            }
            else if (type == typeof(ulong))
            {
                WriteUInt64((ulong)val);
                return;
            }
            else if (type == typeof(object))
            {
                WriteVariant(val);
                return;
            }
            else if (type == typeof(IDBusObject))
            {
                WriteBusObject((IDBusObject)val);
                return;
            }

            if (ArgTypeInspector.IsDBusObjectType(type, isCompileTimeType))
            {
                WriteBusObject((IDBusObject)val);
                return;
            }

            if (ArgTypeInspector.IsSafeHandleType(type))
            {
                WriteSafeHandle((SafeHandle)val);
                return;
            }

            MethodInfo method = WriteMethodFactory.CreateWriteMethodForType(type, isCompileTimeType);

            if (method.IsStatic)
            {
                method.Invoke(null, new object[] { this, val });
            }
            else
            {
                method.Invoke(this, new object[] { val });
            }
        }

        private void WriteObject (Type type, object val)
        {
            ObjectPath2 path2;

            DBusObjectProxy bobj = val as DBusObjectProxy;

            if (bobj == null)
                throw new ArgumentException("No object reference to write", nameof(val));

            path2 = bobj.ObjectPath2;

            WriteObjectPath (path2);
        }

        public void WriteBusObject(IDBusObject busObject)
        {
            WriteObjectPath(busObject.ObjectPath2);
        }

        public void WriteVariant (object val)
        {
            if (val == null)
                throw new NotSupportedException ("Cannot send null variant");

            Type type = val.GetType ();

            if (type == typeof(object))
            {
                throw new ArgumentException($"Cannot (de)serialize Type '{type.FullName}'");
            }

            Signature sig = Signature.GetSig(type, isCompileTimeType: false);

            WriteSignature(sig);
            Write(type, val, isCompileTimeType: false);
        }

        public void WriteArray<T> (IEnumerable<T> val)
        {
            Type elemType = typeof (T);

            var byteArray = val as byte[];
            if (byteArray != null) {
                int valLength = val.Count();
                if (byteArray.Length > ProtocolInformation.MaxArrayLength)
                    ThrowArrayLengthException ((uint)byteArray.Length);

                WriteUInt32 ((uint)byteArray.Length);
                stream.Write (byteArray, 0, byteArray.Length);
                return;
            }

            if (elemType.GetTypeInfo().IsEnum)
                elemType = Enum.GetUnderlyingType (elemType);

            Signature sigElem = Signature.GetSig (elemType, isCompileTimeType: true);
            int fixedSize = 0;

            if (endianness == Environment.NativeEndianness && elemType.GetTypeInfo().IsValueType && !sigElem.IsStruct && elemType != typeof(bool) &&
                sigElem.GetFixedSize (ref fixedSize) && val is Array) {
                var array = val as Array;
                int byteLength = fixedSize * array.Length;
                if (byteLength > ProtocolInformation.MaxArrayLength)
                    ThrowArrayLengthException ((uint)byteLength);

                WriteUInt32 ((uint)byteLength);
                WritePad (sigElem.Alignment);

                byte[] data = new byte[byteLength];
                Buffer.BlockCopy (array, 0, data, 0, data.Length);
                stream.Write (data, 0, data.Length);

                return;
            }

            long origPos = stream.Position;
            WriteUInt32 ((uint)0);

            WritePad (sigElem.Alignment);

            long startPos = stream.Position;

            var tWriter = WriteMethodFactory.CreateWriteMethodDelegate<T>();

            foreach (T elem in val)
                tWriter (this, elem);

            long endPos = stream.Position;
            uint ln = (uint)(endPos - startPos);
            stream.Position = origPos;

            if (ln > ProtocolInformation.MaxArrayLength)
                ThrowArrayLengthException (ln);

            WriteUInt32 (ln);
            stream.Position = endPos;
        }

        internal static void ThrowArrayLengthException (uint ln)
        {
            throw new ProtocolException("Array length " + ln.ToString () + " exceeds maximum allowed " + ProtocolInformation.MaxArrayLength + " bytes");
        }

        public void WriteStructure<T> (T value)
        {
            
            if (!_skipNextStructPadding)
            {
                WritePad (8);
            }
            _skipNextStructPadding = false;

            FieldInfo[] fis = ArgTypeInspector.GetStructFields(typeof(T), isValueTuple: false);
            if (fis.Length == 0)
                return;

            object boxed = value;

            if (MessageReader.IsEligibleStruct (typeof (T), fis)) {
                byte[] buffer = new byte[Marshal.SizeOf (fis[0].FieldType) * fis.Length];

                unsafe {
                    GCHandle valueHandle = GCHandle.Alloc (boxed, GCHandleType.Pinned);
                    Marshal.Copy (valueHandle.AddrOfPinnedObject (), buffer, 0, buffer.Length);
                    valueHandle.Free ();
                }
                stream.Write (buffer, 0, buffer.Length);
                return;
            }

            foreach (var fi in fis)
                Write (fi.FieldType, fi.GetValue (boxed), isCompileTimeType: true);
        }

        public void WriteValueTupleStructure<T> (T value)
        {
            if (!_skipNextStructPadding)
            {
                WritePad (8);
            }
            _skipNextStructPadding = false;
            FieldInfo[] fis = ArgTypeInspector.GetStructFields(typeof(T), isValueTuple: true);
            if (fis.Length == 0)
                return;

            object boxed = value;
            for (int i = 0; i < fis.Length;)
            {
                var fi = fis[i];
                if (i == 7)
                {
                    boxed = fi.GetValue (boxed);
                    fis = ArgTypeInspector.GetStructFields(fi.FieldType, isValueTuple: true);
                    i = 0;
                }
                else
                {
                    Write (fi.FieldType, fi.GetValue (boxed), isCompileTimeType: true);
                    i++;
                }
            }
        }

        public void WriteFromDict<TKey,TValue> (IEnumerable<KeyValuePair<TKey,TValue>> val)
        {
            long origPos = stream.Position;
            // Pre-write array length field, we overwrite it at the end with the correct value
            WriteUInt32 ((uint)0);
            WritePad (8);
            long startPos = stream.Position;

            var keyWriter = WriteMethodFactory.CreateWriteMethodDelegate<TKey>();
            var valueWriter = WriteMethodFactory.CreateWriteMethodDelegate<TValue>();

            foreach (KeyValuePair<TKey,TValue> entry in val) {
                WritePad (8);
                keyWriter (this, entry.Key);
                valueWriter (this, entry.Value);
            }

            long endPos = stream.Position;
            uint ln = (uint)(endPos - startPos);
            stream.Position = origPos;

            if (ln > ProtocolInformation.MaxArrayLength)
                throw new ProtocolException("Dict length " + ln + " exceeds maximum allowed " + ProtocolInformation.MaxArrayLength + " bytes");

            WriteUInt32 (ln);
            stream.Position = endPos;
        }

        public void WriteDictionaryObject<T>(T val)
        {
            var type = typeof(T);
            FieldInfo[] fis = type.GetFields (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            long origPos = stream.Position;
            // Pre-write array length field, we overwrite it at the end with the correct value
            WriteUInt32 ((uint)0);
            WritePad (8);
            long startPos = stream.Position;

            foreach (var fi in fis)
            {
                object fieldVal = fi.GetValue(val);
                if (fieldVal == null)
                {
                    continue;
                }

                Type fieldType;
                string fieldName;
                PropertyTypeInspector.InspectField(fi, out fieldName, out fieldType);
                Signature sig = Signature.GetSig(fieldType, isCompileTimeType: true);

                WritePad (8);
                WriteString(fieldName);
                WriteSignature(sig);
                Write(fieldType, fieldVal, isCompileTimeType: true);
            }

            long endPos = stream.Position;
            uint ln = (uint)(endPos - startPos);
            stream.Position = origPos;

            if (ln > ProtocolInformation.MaxArrayLength)
                throw new ProtocolException("Dict length " + ln + " exceeds maximum allowed " + ProtocolInformation.MaxArrayLength + " bytes");

            WriteUInt32 (ln);
            stream.Position = endPos;
        }

        public void WriteHeader(Header header)
        {
            WriteByte((byte)header.Endianness);
            WriteByte((byte)header.MessageType);
            WriteByte((byte)header.Flags);
            WriteByte(header.MajorVersion);
            WriteUInt32(header.Length);
            WriteUInt32(header.Serial);
            WriteHeaderFields(header.GetFields());
            CloseWrite();
        }

        internal void WriteHeaderFields(IEnumerable<KeyValuePair<FieldCode, object>> val)
        {
            long origPos = stream.Position;
            WriteUInt32 ((uint)0);

            WritePad (8);

            long startPos = stream.Position;

            foreach (KeyValuePair<FieldCode, object> entry in val) {
                WritePad (8);
                WriteByte ((byte)entry.Key);
                switch (entry.Key) {
                    case FieldCode.Destination:
                    case FieldCode.ErrorName:
                    case FieldCode.Interface:
                    case FieldCode.Member:
                    case FieldCode.Sender:
                        WriteSignature (Signature.StringSig);
                        WriteString((string)entry.Value);
                        break;
                    case FieldCode.Path:
                        WriteSignature(Signature.ObjectPathSig);
                        WriteObjectPath((ObjectPath2)entry.Value);
                        break;
                    case FieldCode.ReplySerial:
                        WriteSignature(Signature.UInt32Sig);
                        WriteUInt32((uint)entry.Value);
                        break;
                    case FieldCode.Signature:
                        WriteSignature(Signature.SignatureSig);
                        Signature sig = (Signature)entry.Value;
                        WriteSignature((Signature)entry.Value);
                        break;
                    default:
                        WriteVariant (entry.Value);
                        break;
                }
            }

            long endPos = stream.Position;
            uint ln = (uint)(endPos - startPos);
            stream.Position = origPos;

            if (ln > ProtocolInformation.MaxArrayLength)
                throw new ProtocolException("Dict length " + ln + " exceeds maximum allowed " + ProtocolInformation.MaxArrayLength + " bytes");

            WriteUInt32 (ln);
            stream.Position = endPos;
        }

        private void WriteNull ()
        {
            stream.WriteByte (0);
        }

        // Source buffer for zero-padding
        static readonly byte[] nullBytes = new byte[8];
        private void WritePad (int alignment)
        {
            int needed = ProtocolInformation.PadNeeded ((int)stream.Position, alignment);
            if (needed == 0)
                return;
            stream.Write (nullBytes, 0, needed);
        }
    }
}

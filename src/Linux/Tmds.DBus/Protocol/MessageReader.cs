// Copyright 2006 Alp Toker <alp@atoker.com>
// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

#pragma warning disable 0618 // 'Marshal.SizeOf(Type)' is obsolete

using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Tmds.DBus.CodeGen;

namespace Tmds.DBus.Protocol
{
    internal class MessageReader
    {
        private readonly EndianFlag _endianness;
        private readonly ArraySegment<byte> _data;
        private readonly Message _message;
        private readonly IProxyFactory _proxyFactory;

        private int _pos = 0;
        private bool _skipNextStructPadding = false;

        static Dictionary<Type, bool> s_isPrimitiveStruct = new Dictionary<Type, bool> ();

        public MessageReader(EndianFlag endianness, ArraySegment<byte> data)
        {
            _endianness = endianness;
            _data = data;
        }

        public MessageReader (Message message, IProxyFactory proxyFactory) :
            this(message.Header.Endianness, new ArraySegment<byte>(message.Body ?? Array.Empty<byte>()))
        {
            _message = message;
            _proxyFactory = proxyFactory;
        }

        public void SetSkipNextStructPadding()
        {
            _skipNextStructPadding = true;
        }

        public object Read(Type type)
        {
            if (type.GetTypeInfo().IsEnum)
            {
                var value = Read(Enum.GetUnderlyingType(type));
                return Enum.ToObject(type, value);
            }

            if (type == typeof(bool))
            {
                return ReadBoolean();
            }
            else if (type == typeof(byte))
            {
                return ReadByte();
            }
            else if (type == typeof(double))
            {
                return ReadDouble();
            }
            else if (type == typeof(short))
            {
                return ReadInt16();
            }
            else if (type == typeof(int))
            {
                return ReadInt32();
            }
            else if (type == typeof(long))
            {
                return ReadInt64();
            }
            else if (type == typeof(ObjectPath2))
            {
                return ReadObjectPath();
            }
            else if (type == typeof(Signature))
            {
                return ReadSignature();
            }
            else if (type == typeof(string))
            {
                return ReadString();
            }
            else if (type == typeof(float))
            {
                return ReadSingle();
            }
            else if (type == typeof(ushort))
            {
                return ReadUInt16();
            }
            else if (type == typeof(uint))
            {
                return ReadUInt32();
            }
            else if (type == typeof(ulong))
            {
                return ReadUInt64();
            }
            else if (type == typeof(object))
            {
                return ReadVariant();
            }
            else if (type == typeof(IDBusObject))
            {
                return ReadBusObject();
            }

            var method = ReadMethodFactory.CreateReadMethodForType(type);
            if (method.IsStatic)
            {
                return method.Invoke(null, new object[] { this });
            }
            else
            {
                return method.Invoke(this, null);
            }
        }

        public void Seek (int stride)
        {
            var check = _pos + stride;
            if (check < 0 || check > _data.Count)
                throw new ArgumentOutOfRangeException ("stride");
            _pos = check;
        }

        public T ReadDBusInterface<T>()
        {
            ObjectPath2 path2 = ReadObjectPath();
            return _proxyFactory.CreateProxy<T>(_message.Header.Sender, path2);
        }

        public T ReadEnum<T>()
        {
            Type type = typeof(T);
            var value = Read(Enum.GetUnderlyingType(type));
            return (T)Enum.ToObject(type, value);
        }

        public byte ReadByte ()
        {
            return _data.Array[_data.Offset + _pos++];
        }

        public bool ReadBoolean ()
        {
            uint intval = ReadUInt32 ();

            switch (intval) {
                case 0:
                    return false;
                case 1:
                    return true;
                default:
                    throw new ProtocolException("Read value " + intval + " at position " + _pos + " while expecting boolean (0/1)");
            }
        }

        unsafe protected void MarshalUShort (void* dstPtr)
        {
            ReadPad (2);

            if (_data.Count < _pos + 2)
                throw new ProtocolException("Cannot read beyond end of data");

            if (_endianness == Environment.NativeEndianness) {
                fixed (byte* p = &_data.Array[_data.Offset + _pos])
                    *((ushort*)dstPtr) = *((ushort*)p);
            } else {
                byte* dst = (byte*)dstPtr;
                dst[0] = _data.Array[_data.Offset + _pos + 1];
                dst[1] = _data.Array[_data.Offset + _pos + 0];
            }

            _pos += 2;
        }

        unsafe public short ReadInt16 ()
        {
            short val;

            MarshalUShort (&val);

            return val;
        }

        unsafe public ushort ReadUInt16 ()
        {
            ushort val;

            MarshalUShort (&val);

            return val;
        }

        unsafe protected void MarshalUInt (void* dstPtr)
        {
            ReadPad (4);

            if (_data.Count < _pos + 4)
                throw new ProtocolException("Cannot read beyond end of data");

            if (_endianness == Environment.NativeEndianness) {
                fixed (byte* p = &_data.Array[_data.Offset + _pos])
                    *((uint*)dstPtr) = *((uint*)p);
            } else {
                byte* dst = (byte*)dstPtr;
                dst[0] = _data.Array[_data.Offset + _pos + 3];
                dst[1] = _data.Array[_data.Offset + _pos + 2];
                dst[2] = _data.Array[_data.Offset + _pos + 1];
                dst[3] = _data.Array[_data.Offset + _pos + 0];
            }

            _pos += 4;
        }

        unsafe public int ReadInt32 ()
        {
            int val;

            MarshalUInt (&val);

            return val;
        }

        unsafe public uint ReadUInt32 ()
        {
            uint val;

            MarshalUInt (&val);

            return val;
        }

        unsafe protected void MarshalULong (void* dstPtr)
        {
            ReadPad (8);

            if (_data.Count < _pos + 8)
                throw new ProtocolException("Cannot read beyond end of data");

            if (_endianness == Environment.NativeEndianness) {
                fixed (byte* p = &_data.Array[_data.Offset + _pos])
                    *((ulong*)dstPtr) = *((ulong*)p);
            } else {
                byte* dst = (byte*)dstPtr;
                for (int i = 0; i < 8; ++i)
                    dst[i] = _data.Array[_data.Offset + _pos + (7 - i)];
            }

            _pos += 8;
        }

        unsafe public long ReadInt64 ()
        {
            long val;

            MarshalULong (&val);

            return val;
        }

        unsafe public ulong ReadUInt64 ()
        {
            ulong val;

            MarshalULong (&val);

            return val;
        }

        unsafe public float ReadSingle ()
        {
            float val;

            MarshalUInt (&val);

            return val;
        }

        unsafe public double ReadDouble ()
        {
            double val;

            MarshalULong (&val);

            return val;
        }

        public string ReadString ()
        {
            uint ln = ReadUInt32 ();

            string val = Encoding.UTF8.GetString (_data.Array, _data.Offset + _pos, (int)ln);
            _pos += (int)ln;
            ReadNull ();

            return val;
        }

        public void SkipString()
        {
            ReadString();
        }

        public ObjectPath2 ReadObjectPath ()
        {
            //exactly the same as string
            return new ObjectPath2 (ReadString ());
        }

        public IDBusObject ReadBusObject()
        {
            return new BusObject(ReadObjectPath());
        }

        public Signature ReadSignature ()
        {
            byte ln = ReadByte ();

            // Avoid an array allocation for small signatures
            if (ln == 1) {
                DType dtype = (DType)ReadByte ();
                ReadNull ();
                return new Signature (dtype);
            }

            if (ln > ProtocolInformation.MaxSignatureLength)
                throw new ProtocolException("Signature length " + ln + " exceeds maximum allowed " + ProtocolInformation.MaxSignatureLength + " bytes");

            byte[] sigData = new byte[ln];
            Array.Copy (_data.Array, _data.Offset + _pos, sigData, 0, (int)ln);
            _pos += (int)ln;
            ReadNull ();

            return Signature.Take (sigData);
        }

        public T ReadSafeHandle<T>()
        {
            var idx = ReadInt32();
            var fds = _message.UnixFds;
            int fd = -1;
            if (fds != null && idx < fds.Length)
            {
                fd = fds[idx].Handle;
            }
            return (T)Activator.CreateInstance(typeof(T), new object[] { (IntPtr)fd , true });
        }

        public object ReadVariant ()
        {
            var sig = ReadSignature ();
            if (!sig.IsSingleCompleteType)
                throw new InvalidOperationException (string.Format ("ReadVariant need a single complete type signature, {0} was given", sig.ToString ()));
            return Read(sig.ToType());
        }

        public T ReadVariantAsType<T>()
        {
            var sig = ReadSignature ();
            if (!sig.IsSingleCompleteType)
                throw new InvalidOperationException (string.Format ("ReadVariant need a single complete type signature, {0} was given", sig.ToString ()));
            var type = typeof(T);
            if (sig != Signature.GetSig(type, isCompileTimeType: true))
                throw new InvalidCastException($"Cannot convert dbus type '{sig.ToString()}' to '{type.FullName}'");
            return (T)Read(type);
        }

        public T ReadDictionaryObject<T>()
        {
            var type = typeof(T);
            FieldInfo[] fis = type.GetFields (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            object val = Activator.CreateInstance (type);

            uint ln = ReadUInt32 ();
            if (ln > ProtocolInformation.MaxArrayLength)
                throw new ProtocolException("Dict length " + ln + " exceeds maximum allowed " + ProtocolInformation.MaxArrayLength + " bytes");

            ReadPad (8);

            int endPos = _pos + (int)ln;

            while (_pos < endPos) {
                ReadPad (8);

                var key = ReadString();
                var sig = ReadSignature();

                if (!sig.IsSingleCompleteType)
                    throw new InvalidOperationException (string.Format ("ReadVariant need a single complete type signature, {0} was given", sig.ToString ()));

                // if the key contains a '-' which is an invalid identifier character,
                // we try and replace it with '_' and see if we find a match.
                // The name may be prefixed with '_'.
                var field = fis.Where(f =>   ((f.Name.Length == key.Length) || (f.Name.Length == key.Length + 1 && f.Name[0] == '_'))
                                          && (f.Name.EndsWith(key, StringComparison.Ordinal) || (key.Contains("-") && f.Name.Replace('_', '-').EndsWith(key, StringComparison.Ordinal)))).SingleOrDefault();

                if (field == null)
                {
                    var value = Read(sig.ToType());
                }
                else
                {
                    Type fieldType;
                    string propertyName;
                    PropertyTypeInspector.InspectField(field, out propertyName, out fieldType);

                    if (sig != Signature.GetSig(fieldType, isCompileTimeType: true))
                    {
                        throw new ArgumentException($"Dictionary '{type.FullName}' field '{field.Name}' with type '{fieldType.FullName}' cannot be read from D-Bus type '{sig}'");
                    }

                    var readValue = Read(fieldType);

                    field.SetValue (val, readValue);
                }
            }

            if (_pos != endPos)
                throw new ProtocolException("Read pos " + _pos + " != ep " + endPos);

            return (T)val;
        }

        public Dictionary<TKey, TValue> ReadDictionary<TKey, TValue> ()
        {
            uint ln = ReadUInt32 ();

            if (ln > ProtocolInformation.MaxArrayLength)
                throw new ProtocolException("Dict length " + ln + " exceeds maximum allowed " + ProtocolInformation.MaxArrayLength + " bytes");

            var val = new Dictionary<TKey, TValue> ((int)(ln / 8));
            ReadPad (8);

            int endPos = _pos + (int)ln;

            var keyReader = ReadMethodFactory.CreateReadMethodDelegate<TKey>();
            var valueReader = ReadMethodFactory.CreateReadMethodDelegate<TValue>();

            while (_pos < endPos) {
                ReadPad (8);
                TKey k = keyReader(this);
                TValue v = valueReader(this);
                val.Add (k, v);
            }

            if (_pos != endPos)
                throw new ProtocolException("Read pos " + _pos + " != ep " + endPos);

            return val;
        }

        public T[] ReadArray<T> ()
        {
            uint ln = ReadUInt32 ();
            Type elemType = typeof (T);

            if (ln > ProtocolInformation.MaxArrayLength)
                throw new ProtocolException("Array length " + ln + " exceeds maximum allowed " + ProtocolInformation.MaxArrayLength + " bytes");

            //advance to the alignment of the element
            ReadPad (ProtocolInformation.GetAlignment (elemType));

            if (elemType.GetTypeInfo().IsPrimitive) {
                // Fast path for primitive types (except bool which isn't blittable and take another path)
                if (elemType != typeof (bool))
                    return MarshalArray<T> (ln);
                else
                    return (T[])(Array)MarshalBoolArray (ln);
            }

            var list = new List<T> ();
            int endPos = _pos + (int)ln;

            var elementReader = ReadMethodFactory.CreateReadMethodDelegate<T>();

            while (_pos < endPos)
                list.Add (elementReader(this));

            if (_pos != endPos)
                throw new ProtocolException("Read pos " + _pos + " != ep " + endPos);

            return list.ToArray ();
        }

        TArray[] MarshalArray<TArray> (uint length)
        {
            int sof = Marshal.SizeOf<TArray>();
            TArray[] array = new TArray[(int)(length / sof)];

            if (_endianness == Environment.NativeEndianness) {
                Buffer.BlockCopy (_data.Array, _data.Offset + _pos, array, 0, (int)length);
                _pos += (int)length;
            } else {
                GCHandle handle = GCHandle.Alloc (array, GCHandleType.Pinned);
                DirectCopy (sof, array.Length, handle);
                handle.Free ();
            }

            return array;
        }

        void DirectCopy (int sof, int length, GCHandle handle)
        {
            DirectCopy (sof, length, handle.AddrOfPinnedObject ());
        }

        unsafe void DirectCopy (int sof, int length, IntPtr handle)
        {
            int byteLength = length * sof;
            if (_endianness == Environment.NativeEndianness) {
                Marshal.Copy (_data.Array, _data.Offset + _pos, handle, byteLength);
            } else {
                byte* ptr = (byte*)(void*)handle;
                for (int i = _pos; i < _pos + byteLength; i += sof)
                    for (int j = i; j < i + sof; j++)
                        ptr[2 * i - _pos + (sof - 1) - j] = _data.Array[_data.Offset + j];
            }

            _pos += byteLength;
        }

        bool[] MarshalBoolArray (uint length)
        {
            bool[] array = new bool [length];
            for (int i = 0; i < length; i++)
                array[i] = ReadBoolean ();

            return array;
        }

        public T ReadStruct<T> ()
        {
            if (!_skipNextStructPadding)
            {
                ReadPad (8);
            }
            _skipNextStructPadding = false;

            FieldInfo[] fis = ArgTypeInspector.GetStructFields(typeof(T), isValueTuple: false);

            // Empty struct? No need for processing
            if (fis.Length == 0)
                return default (T);

            if (IsEligibleStruct (typeof(T), fis))
                return (T)MarshalStruct (typeof(T), fis);

            object val = Activator.CreateInstance<T> ();

            foreach (System.Reflection.FieldInfo fi in fis)
                fi.SetValue (val, Read (fi.FieldType));

            return (T)val;
        }

        public T ReadValueTupleStruct<T> ()
        {
            if (!_skipNextStructPadding)
            {
                ReadPad (8);
            }
            _skipNextStructPadding = false;

            bool isValueTuple = true;
            FieldInfo[] fis = ArgTypeInspector.GetStructFields(typeof(T), isValueTuple);

            // Empty struct? No need for processing
            if (fis.Length == 0)
                return default (T);

            object val = Activator.CreateInstance<T> ();

            for (int i = 0; i < fis.Length; i++)
            {
                var fi = fis[i];
                if (i == 7 && isValueTuple)
                {
                    _skipNextStructPadding = true;
                }
                fi.SetValue (val, Read(fi.FieldType));
            }

            return (T)val;
        }

        object MarshalStruct (Type structType, FieldInfo[] fis)
        {
            object strct = Activator.CreateInstance (structType);
            int sof = Marshal.SizeOf (fis[0].FieldType);
            GCHandle handle = GCHandle.Alloc (strct, GCHandleType.Pinned);
            DirectCopy (sof, fis.Length, handle);
            handle.Free ();

            return strct;
        }

        public void ReadNull ()
        {
            if (_data.Array[_data.Offset + _pos] != 0)
                throw new ProtocolException("Read non-zero byte at position " + _pos + " while expecting null terminator");
            _pos++;
        }

        public void ReadPad (int alignment)
        {
            for (int endPos = ProtocolInformation.Padded (_pos, alignment) ; _pos != endPos ; _pos++)
                if (_data.Array[_data.Offset + _pos] != 0)
                    throw new ProtocolException("Read non-zero byte at position " + _pos + " while expecting padding. Value given: " + _data.Array[_data.Offset + _pos]);
        }

        // If a struct is only composed of primitive type fields (i.e. blittable types)
        // then this method return true. Result is cached in isPrimitiveStruct dictionary.
        internal static bool IsEligibleStruct (Type structType, FieldInfo[] fields)
        {
            lock (s_isPrimitiveStruct)
            {
                bool result;
                if (s_isPrimitiveStruct.TryGetValue(structType, out result))
                    return result;

                var typeInfo = structType.GetTypeInfo();
                if (!typeInfo.IsLayoutSequential)
                    return false;

                if (!(s_isPrimitiveStruct[structType] = fields.All((f) => f.FieldType.GetTypeInfo().IsPrimitive && f.FieldType != typeof(bool))))
                    return false;

                int alignement = ProtocolInformation.GetAlignment(fields[0].FieldType);

                return s_isPrimitiveStruct[structType] = !fields.Any((f) => ProtocolInformation.GetAlignment(f.FieldType) != alignement);
            }
        }

        private class BusObject : IDBusObject
        {
            public BusObject(ObjectPath2 path2)
            {
                ObjectPath2 = path2;
            }

            public ObjectPath2 ObjectPath2 { get; }
        }
    }
}

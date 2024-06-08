// Copyright 2006 Alp Toker <alp@atoker.com>
// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Tmds.DBus.CodeGen;
using Tmds.DBus.Protocol;

namespace Tmds.DBus
{
    /// <summary>
    /// D-Bus type signature.
    /// </summary>
    public struct Signature
    {
        internal static readonly Signature Empty = new Signature (String.Empty);
        internal static readonly Signature ArraySig = Allocate (DType.Array);
        internal static readonly Signature ByteSig = Allocate (DType.Byte);
        internal static readonly Signature DictEntryBegin = Allocate (DType.DictEntryBegin);
        internal static readonly Signature DictEntryEnd = Allocate (DType.DictEntryEnd);
        internal static readonly Signature Int32Sig = Allocate (DType.Int32);
        internal static readonly Signature UInt16Sig = Allocate (DType.UInt16);
        internal static readonly Signature UInt32Sig = Allocate (DType.UInt32);
        internal static readonly Signature StringSig = Allocate (DType.String);
        internal static readonly Signature StructBegin = Allocate (DType.StructBegin);
        internal static readonly Signature StructEnd = Allocate (DType.StructEnd);
        internal static readonly Signature ObjectPathSig = Allocate (DType.ObjectPath);
        internal static readonly Signature SignatureSig = Allocate (DType.Signature);
        internal static readonly Signature SignatureUnixFd = Allocate (DType.UnixFd);
        internal static readonly Signature VariantSig = Allocate (DType.Variant);
        internal static readonly Signature BoolSig = Allocate(DType.Boolean);
        internal static readonly Signature DoubleSig = Allocate(DType.Double);
        internal static readonly Signature Int16Sig = Allocate(DType.Int16);
        internal static readonly Signature Int64Sig = Allocate(DType.Int64);
        internal static readonly Signature SingleSig = Allocate(DType.Single);
        internal static readonly Signature UInt64Sig = Allocate(DType.UInt64);
        internal static readonly Signature StructBeginSig = Allocate(DType.StructBegin);
        internal static readonly Signature StructEndSig = Allocate(DType.StructEnd);
        internal static readonly Signature DictEntryBeginSig = Allocate(DType.DictEntryBegin);
        internal static readonly Signature DictEntryEndSig = Allocate(DType.DictEntryEnd);

        private byte[] _data;

        /// <summary>
        /// Determines whether two specified Signatures have the same value.
        /// </summary>
        public static bool operator== (Signature a, Signature b)
        {
            if (a._data == b._data)
                return true;

            if (a._data == null)
                return false;

            if (b._data == null)
                return false;

            if (a._data.Length != b._data.Length)
                return false;

            for (int i = 0 ; i != a._data.Length ; i++)
                if (a._data[i] != b._data[i])
                    return false;

            return true;
        }

        /// <summary>
        /// Determines whether two specified Signatures have different values.
        /// </summary>
        public static bool operator!=(Signature a, Signature b)
        {
            return !(a == b);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        public override bool Equals (object o)
        {
            if (o == null)
                return false;

            if (!(o is Signature))
                return false;

            return this == (Signature)o;
        }

        /// <summary>
        /// Returns the hash code for this Signature.
        /// </summary>
        public override int GetHashCode()
        {
            if (_data == null)
            {
                return 0;
            }
            int hash = 17;
            for(int i = 0; i < _data.Length; i++)
            {
                hash = hash * 31 + _data[i].GetHashCode();
            }
            return hash;
        }

        internal static Signature Concat (Signature s1, Signature s2)
        {
            if (s1._data == null && s2._data == null)
                return Signature.Empty;

            if (s1._data == null)
                return s2;

            if (s2._data == null)
                return s1;

            if (s1.Length + s2.Length == 0)
                return Signature.Empty;

            byte[] data = new byte[s1._data.Length + s2._data.Length];
            s1._data.CopyTo (data, 0);
            s2._data.CopyTo (data, s1._data.Length);
            return Signature.Take (data);
        }

        /// <summary>
        /// Creates a new Signature.
        /// </summary>
        /// <param name="value">signature.</param>
        public Signature(string value)
        {
            if (value == null)
                throw new ArgumentNullException ("value");
            if (!IsValid (value))
                throw new ArgumentException (string.Format ("'{0}' is not a valid signature", value), "value");

            foreach (var c in value)
                if (!Enum.IsDefined (typeof (DType), (byte) c))
                    throw new ArgumentException (string.Format ("{0} is not a valid dbus type", c));

            if (value.Length == 0) {
                _data = Array.Empty<byte>();
            } else if (value.Length == 1) {
                _data = DataForDType ((DType)value[0]);
            } else {
                _data = Encoding.ASCII.GetBytes (value);
            }
        }

        /// <summary>
        /// Creates a new Signature.
        /// </summary>
        /// <param name="value">signature.</param>
        public static implicit operator Signature(string value)
        {
            return new Signature(value);
        }

        // Basic validity is to check that every "opening" DType has a corresponding closing DType
        internal static bool IsValid (string strSig)
        {
            int structCount = 0;
            int dictCount = 0;

            foreach (char c in strSig) {
                switch ((DType)c) {
                case DType.StructBegin:
                    structCount++;
                    break;
                case DType.StructEnd:
                    structCount--;
                    break;
                case DType.DictEntryBegin:
                    dictCount++;
                    break;
                case DType.DictEntryEnd:
                    dictCount--;
                    break;
                }
            }

            return structCount == 0 && dictCount == 0;
        }

        internal static Signature Take (byte[] value)
        {
            Signature sig;

            if (value.Length == 0) {
                sig._data = Empty._data;
                return sig;
            }

            if (value.Length == 1) {
                sig._data = DataForDType ((DType)value[0]);
                return sig;
            }

            sig._data = value;
            return sig;
        }

        internal static byte[] DataForDType (DType value)
        {
            switch (value) {
                case DType.Byte: return ByteSig._data;
                case DType.Boolean: return BoolSig._data;
                case DType.Int16: return Int16Sig._data;
                case DType.UInt16: return UInt16Sig._data;
                case DType.Int32: return Int32Sig._data;
                case DType.UInt32: return UInt32Sig._data;
                case DType.Int64: return Int64Sig._data;
                case DType.UInt64: return UInt64Sig._data;
                case DType.Single: return SingleSig._data;
                case DType.Double: return DoubleSig._data;
                case DType.String: return StringSig._data;
                case DType.ObjectPath: return ObjectPathSig._data;
                case DType.Signature: return SignatureSig._data;
                case DType.Array: return ArraySig._data;
                case DType.Variant: return VariantSig._data;
                case DType.StructBegin: return StructBeginSig._data;
                case DType.StructEnd: return StructEndSig._data;
                case DType.DictEntryBegin: return DictEntryBeginSig._data;
                case DType.DictEntryEnd: return DictEntryEndSig._data;
                default:
                    return new byte[] {(byte)value};
            }
        }

        private static Signature Allocate (DType value)
        {
            Signature sig;
            sig._data = new byte[] {(byte)value};
            return sig;
        }

        internal Signature (DType value)
        {
            this._data = DataForDType (value);
        }

        internal byte[] GetBuffer ()
        {
            return _data;
        }

        internal DType this[int index]
        {
            get {
                return (DType)_data[index];
            }
        }

        /// <summary>
        /// Length of the Signature.
        /// </summary>
        public int Length
        {
            get {
                return _data != null ? _data.Length : 0;
            }
        }

        internal string Value
        {
            get {
                if (_data == null)
                    return String.Empty;

                return Encoding.ASCII.GetString (_data);
            }
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        public override string ToString ()
        {
            return Value;
        }

        internal static Signature MakeArray (Signature signature)
        {
            if (!signature.IsSingleCompleteType)
                throw new ArgumentException ("The type of an array must be a single complete type", "signature");
            return Concat(Signature.ArraySig, signature);
        }

        internal static Signature MakeStruct (Signature signature)
        {
            if (signature == Signature.Empty)
                throw new ArgumentException ("Cannot create a struct with no fields", "signature");

            return Concat(Concat(Signature.StructBegin, signature), Signature.StructEnd);
        }

        internal static Signature MakeDictEntry (Signature keyType, Signature valueType)
        {
            if (!keyType.IsSingleCompleteType)
                throw new ArgumentException ("Signature must be a single complete type", "keyType");
            if (!valueType.IsSingleCompleteType)
                throw new ArgumentException ("Signature must be a single complete type", "valueType");

            return Concat(Concat(Concat(Signature.DictEntryBegin, keyType), valueType), Signature.DictEntryEnd);
        }

        internal static Signature MakeDict (Signature keyType, Signature valueType)
        {
            return MakeArray (MakeDictEntry (keyType, valueType));
        }

        internal int Alignment
        {
            get {
                if (_data.Length == 0)
                    return 0;

                return ProtocolInformation.GetAlignment (this[0]);
            }
        }

        internal int GetSize (DType dtype)
        {
            switch (dtype) {
                case DType.Byte:
                    return 1;
                case DType.Boolean:
                    return 4;
                case DType.Int16:
                case DType.UInt16:
                    return 2;
                case DType.Int32:
                case DType.UInt32:
                case DType.UnixFd:
                    return 4;
                case DType.Int64:
                case DType.UInt64:
                    return 8;
                case DType.Single:
                    return 4;
                case DType.Double:
                    return 8;
                case DType.String:
                case DType.ObjectPath:
                case DType.Signature:
                case DType.Array:
                case DType.StructBegin:
                case DType.Variant:
                case DType.DictEntryBegin:
                    return -1;
                case DType.Invalid:
                default:
                    throw new ProtocolException("Cannot determine size of unknown D-Bus type: " + dtype);
            }
        }

        internal bool GetFixedSize (ref int size)
        {
            if (size < 0)
                return false;

            if (_data.Length == 0)
                return true;

            // Sensible?
            size = ProtocolInformation.Padded (size, Alignment);

            if (_data.Length == 1) {
                int valueSize = GetSize (this[0]);

                if (valueSize == -1)
                    return false;

                size += valueSize;
                return true;
            }

            if (IsStructlike) {
                foreach (Signature sig in GetParts ())
                        if (!sig.GetFixedSize (ref size))
                            return false;
                return true;
            }

            if (IsArray || IsDict)
                return false;

            if (IsStruct) {
                foreach (Signature sig in GetFieldSignatures ())
                        if (!sig.GetFixedSize (ref size))
                            return false;
                return true;
            }

            // Any other cases?
            throw new Exception ();
        }

        internal bool IsSingleCompleteType
        {
            get {
                if (_data.Length == 0)
                    return true;
                var checker = new SignatureChecker (_data);
                return checker.CheckSignature ();
            }
        }

        internal bool IsStruct
        {
            get {
                if (Length < 2)
                    return false;

                if (this[0] != DType.StructBegin)
                    return false;

                // FIXME: Incorrect! What if this is in fact a Structlike starting and finishing with structs?
                if (this[Length - 1] != DType.StructEnd)
                    return false;

                return true;
            }
        }

        internal bool IsStructlike
        {
            get {
                if (Length < 2)
                    return false;

                if (IsArray)
                    return false;

                if (IsDict)
                    return false;

                if (IsStruct)
                    return false;

                return true;
            }
        }

        internal bool IsDict
        {
            get {
                if (Length < 3)
                    return false;

                if (!IsArray)
                    return false;

                // 0 is 'a'
                if (this[1] != DType.DictEntryBegin)
                    return false;

                return true;
            }
        }

        internal bool IsArray
        {
            get {
                if (Length < 2)
                    return false;

                if (this[0] != DType.Array)
                    return false;

                return true;
            }
        }

        internal Type ToType ()
        {
            int pos = 0;
            Type ret = ToType (ref pos);
            if (pos != _data.Length)
                throw new ProtocolException("Signature '" + Value + "' is not a single complete type");
            return ret;
        }

        internal IEnumerable<Signature> GetFieldSignatures ()
        {
            if (this == Signature.Empty || this[0] != DType.StructBegin)
                throw new ProtocolException("Not a struct");

            for (int pos = 1 ; pos < _data.Length - 1 ;)
                yield return GetNextSignature (ref pos);
        }

        internal void GetDictEntrySignatures (out Signature sigKey, out Signature sigValue)
        {
            if (this == Signature.Empty || this[0] != DType.DictEntryBegin)
                throw new ProtocolException("Not a DictEntry");

            int pos = 1;
            sigKey = GetNextSignature (ref pos);
            sigValue = GetNextSignature (ref pos);
        }

        internal IEnumerable<Signature> GetParts ()
        {
            if (_data == null)
                yield break;
            for (int pos = 0 ; pos < _data.Length ;) {
                yield return GetNextSignature (ref pos);
            }
        }

        internal Signature GetNextSignature (ref int pos)
        {
            if (_data == null)
                return Signature.Empty;

            DType dtype = (DType)_data[pos++];

            switch (dtype) {
                //case DType.Invalid:
                //    return typeof (void);
                case DType.Array:
                    //peek to see if this is in fact a dictionary
                    if ((DType)_data[pos] == DType.DictEntryBegin) {
                        //skip over the {
                        pos++;
                        Signature keyType = GetNextSignature (ref pos);
                        Signature valueType = GetNextSignature (ref pos);
                        //skip over the }
                        pos++;
                        return Signature.MakeDict (keyType, valueType);
                    } else {
                        Signature elementType = GetNextSignature (ref pos);
                        return MakeArray (elementType);
                    }
                //case DType.DictEntryBegin: // FIXME: DictEntries should be handled separately.
                case DType.StructBegin:
                    //List<Signature> fieldTypes = new List<Signature> ();
                    Signature fieldsSig = Signature.Empty;
                    while ((DType)_data[pos] != DType.StructEnd)
                    {
                        fieldsSig = Concat(fieldsSig, GetNextSignature (ref pos));
                    }
                    //skip over the )
                    pos++;
                    return Signature.MakeStruct (fieldsSig);
                    //return fieldsSig;
                case DType.DictEntryBegin:
                    Signature sigKey = GetNextSignature (ref pos);
                    Signature sigValue = GetNextSignature (ref pos);
                    //skip over the }
                    pos++;
                    return Signature.MakeDictEntry (sigKey, sigValue);
                default:
                    return new Signature (dtype);
            }
        }

        internal Type ToType (ref int pos)
        {
            if (_data == null)
                return typeof (void);

            DType dtype = (DType)_data[pos++];

            switch (dtype) {
            case DType.Invalid:
                return typeof (void);
            case DType.Byte:
                return typeof (byte);
            case DType.Boolean:
                return typeof (bool);
            case DType.Int16:
                return typeof (short);
            case DType.UInt16:
                return typeof (ushort);
            case DType.Int32:
                return typeof (int);
            case DType.UInt32:
                return typeof (uint);
            case DType.Int64:
                return typeof (long);
            case DType.UInt64:
                return typeof (ulong);
            case DType.Single: ////not supported by libdbus at time of writing
                return typeof (float);
            case DType.Double:
                return typeof (double);
            case DType.String:
                return typeof (string);
            case DType.ObjectPath:
                return typeof (ObjectPath2);
            case DType.Signature:
                return typeof (Signature);
            case DType.UnixFd:
                return typeof (CloseSafeHandle);
            case DType.Array:
                //peek to see if this is in fact a dictionary
                if ((DType)_data[pos] == DType.DictEntryBegin) {
                    //skip over the {
                    pos++;
                    Type keyType = ToType (ref pos);
                    Type valueType = ToType (ref pos);
                    //skip over the }
                    pos++;
                    return typeof(IDictionary<,>).MakeGenericType (new [] { keyType, valueType});
                } else {
                    return ToType (ref pos).MakeArrayType ();
                }
            case DType.StructBegin:
                List<Type> innerTypes = new List<Type> ();
                while (((DType)_data[pos]) != DType.StructEnd)
                    innerTypes.Add (ToType (ref pos));
                // go over the struct end
                pos++;
                return TypeOfValueTupleOf(innerTypes.ToArray ());
            case DType.DictEntryBegin:
                return typeof (System.Collections.Generic.KeyValuePair<,>);
            case DType.Variant:
                return typeof (object);
            default:
                throw new NotSupportedException ("Parsing or converting this signature is not yet supported (signature was '" + Value + "'), at DType." + dtype);
            }
        }

        internal static Signature GetSig (object[] objs)
        {
            return GetSig (objs.Select(o => o.GetType()).ToArray(), isCompileTimeType: true);
        }

        internal static Signature GetSig (Type[] types, bool isCompileTimeType)
        {
            if (types == null)
                throw new ArgumentNullException ("types");

            Signature sig = Signature.Empty;

            foreach (Type type in types)
            {
                sig = Concat(sig, GetSig (type, isCompileTimeType));
            }

            return sig;
        }

        internal static Signature GetSig (Type type, bool isCompileTimeType)
        {
            if (type == null)
                throw new ArgumentNullException ("type");

            if (type.GetTypeInfo().IsEnum)
            {
                type = Enum.GetUnderlyingType(type);
            }

            if (type == typeof(bool))
            {
                return BoolSig;
            }
            else if (type == typeof(byte))
            {
                return ByteSig;
            }
            else if (type == typeof(double))
            {
                return DoubleSig;
            }
            else if (type == typeof(short))
            {
                return Int16Sig;
            }
            else if (type == typeof(int))
            {
                return Int32Sig;
            }
            else if (type == typeof(long))
            {
                return Int64Sig;
            }
            else if (type == typeof(ObjectPath2))
            {
                return ObjectPathSig;
            }
            else if (type == typeof(Signature))
            {
                return SignatureSig;
            }
            else if (type == typeof(string))
            {
                return StringSig;
            }
            else if (type == typeof(float))
            {
                return SingleSig;
            }
            else if (type == typeof(ushort))
            {
                return UInt16Sig;
            }
            else if (type == typeof(uint))
            {
                return UInt32Sig;
            }
            else if (type == typeof(ulong))
            {
                return UInt64Sig;
            }
            else if (type == typeof(object))
            {
                return VariantSig;
            }
            else if (type == typeof(IDBusObject))
            {
                return ObjectPathSig;
            }

            if (ArgTypeInspector.IsDBusObjectType(type, isCompileTimeType))
            {
                return ObjectPathSig;
            }

            Type elementType;
            var enumerableType = ArgTypeInspector.InspectEnumerableType(type, out elementType, isCompileTimeType);
            if (enumerableType != ArgTypeInspector.EnumerableType.NotEnumerable)
            {
                if ((enumerableType == ArgTypeInspector.EnumerableType.EnumerableKeyValuePair) ||
                    (enumerableType == ArgTypeInspector.EnumerableType.GenericDictionary) ||
                    (enumerableType == ArgTypeInspector.EnumerableType.AttributeDictionary))
                {
                    Type keyType = elementType.GenericTypeArguments[0];
                    Type valueType = elementType.GenericTypeArguments[1];
                    return Signature.MakeDict(GetSig(keyType, isCompileTimeType: true), GetSig(valueType, isCompileTimeType: true));
                }
                else // Enumerable
                {
                    return MakeArray(GetSig(elementType, isCompileTimeType: true));
                }
            }

            bool isValueTuple;
            if (ArgTypeInspector.IsStructType(type, out isValueTuple))
            {
                Signature sig = Signature.Empty;
                var fields = ArgTypeInspector.GetStructFields(type, isValueTuple);
                for (int i = 0; i < fields.Length;)
                {
                    var fi = fields[i];
                    if (i == 7 && isValueTuple)
                    {
                        fields = ArgTypeInspector.GetStructFields(fi.FieldType, isValueTuple);
                        i = 0;
                    }
                    else
                    {
                        sig = Concat(sig, GetSig(fi.FieldType, isCompileTimeType: true));
                        i++;
                    }
                }

                return Signature.MakeStruct(sig);
            }

            if (ArgTypeInspector.IsSafeHandleType(type))
            {
                return Signature.SignatureUnixFd;
            }

            throw new ArgumentException($"Cannot (de)serialize Type '{type.FullName}'");
        }

        private static Type TypeOfValueTupleOf(Type[] innerTypes)
        {
            if (innerTypes == null || innerTypes.Length == 0)
                throw new NotSupportedException($"ValueTuple of length {innerTypes?.Length} is not supported");
            if (innerTypes.Length > 7)
            {
                innerTypes = new [] { innerTypes[0], innerTypes[1], innerTypes[2], innerTypes[3], innerTypes[4], innerTypes[5], innerTypes[6], TypeOfValueTupleOf(innerTypes.Skip(7).ToArray()) };
            }

            Type structType = null;
            switch (innerTypes.Length) {
            case 1:
                structType = typeof(ValueTuple<>);
                break;
            case 2:
                structType = typeof(ValueTuple<,>);
                break;
            case 3:
                structType = typeof(ValueTuple<,,>);
                break;
            case 4:
                structType = typeof(ValueTuple<,,,>);
                break;
            case 5:
                structType = typeof(ValueTuple<,,,,>);
                break;
            case 6:
                structType = typeof(ValueTuple<,,,,,>);
                break;
            case 7:
                structType = typeof(ValueTuple<,,,,,,>);
                break;
            case 8:
                structType = typeof(ValueTuple<,,,,,,,>);
                break;
            }
            return structType.MakeGenericType(innerTypes);
        }

        class SignatureChecker
        {
            byte[] data;
            int pos;

            internal SignatureChecker (byte[] data)
            {
                this.data = data;
            }

            internal bool CheckSignature ()
            {
                return SingleType () ? pos == data.Length : false;
            }

            bool SingleType ()
            {
                if (pos >= data.Length)
                    return false;

                //Console.WriteLine ((DType)data[pos]);

                switch ((DType)data[pos]) {
                // Simple Type
                case DType.Byte:
                case DType.Boolean:
                case DType.Int16:
                case DType.UInt16:
                case DType.Int32:
                case DType.UInt32:
                case DType.Int64:
                case DType.UInt64:
                case DType.Single:
                case DType.Double:
                case DType.String:
                case DType.ObjectPath:
                case DType.Signature:
                case DType.Variant:
                    pos += 1;
                    return true;
                case DType.Array:
                    pos += 1;
                    return ArrayType ();
                case DType.StructBegin:
                    pos += 1;
                    return StructType ();
                case DType.DictEntryBegin:
                    pos += 1;
                    return DictType ();
                }

                return false;
            }

            bool ArrayType ()
            {
                return SingleType ();
            }

            bool DictType ()
            {
                bool result = SingleType () && SingleType () && ((DType)data[pos]) == DType.DictEntryEnd;
                if (result)
                    pos += 1;
                return result;
            }

            bool StructType ()
            {
                if (pos >= data.Length)
                    return false;
                while (((DType)data[pos]) != DType.StructEnd) {
                    if (!SingleType ())
                        return false;
                    if (pos >= data.Length)
                        return false;
                }
                pos += 1;

                return true;
            }
        }
    }
}

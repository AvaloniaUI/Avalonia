// Copyright 2006 Alp Toker <alp@atoker.com>
// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.Reflection;
using Tmds.DBus.CodeGen;

namespace Tmds.DBus.Protocol
{
    internal static class ProtocolInformation
    {
        //protocol versions
        public const byte Version = 1;

        public const uint MaxMessageLength = 134217728; //2 to the 27th power
        public const uint MaxArrayLength = 67108864; //2 to the 26th power
        public const uint MaxSignatureLength = 255;
        public const uint MaxArrayDepth = 32;
        public const uint MaxStructDepth = 32;

        //this is not strictly related to Protocol since names are passed around as strings
        internal const uint MaxNameLength = 255;
        internal const uint MaxMatchRuleLength = 1024;
        internal const uint MaxMatchRuleArgs = 64;

        public static int PadNeeded (int pos, int alignment)
        {
            int pad = pos % alignment;
            return pad == 0 ? 0 : alignment - pad;
        }

        public static int Padded (int pos, int alignment)
        {
            int pad = pos % alignment;
            if (pad != 0)
                pos += alignment - pad;

            return pos;
        }

        public static int GetAlignment(Type type)
        {
            if (type.GetTypeInfo().IsEnum)
            {
                type = Enum.GetUnderlyingType(type);
            }

            if (type == typeof(bool))
            {
                return GetAlignment(DType.Boolean);
            }
            else if (type == typeof(byte))
            {
                return GetAlignment(DType.Byte);
            }
            else if (type == typeof(double))
            {
                return GetAlignment(DType.Double);
            }
            else if (type == typeof(short))
            {
                return GetAlignment(DType.Int16);
            }
            else if (type == typeof(int))
            {
                return GetAlignment(DType.Int32);
            }
            else if (type == typeof(long))
            {
                return GetAlignment(DType.Int64);
            }
            else if (type == typeof(ObjectPath2))
            {
                return GetAlignment(DType.ObjectPath);
            }
            else if (type == typeof(Signature))
            {
                return GetAlignment(DType.Signature);
            }
            else if (type == typeof(string))
            {
                return GetAlignment(DType.String);
            }
            else if (type == typeof(float))
            {
                return GetAlignment(DType.Single);
            }
            else if (type == typeof(ushort))
            {
                return GetAlignment(DType.UInt16);
            }
            else if (type == typeof(uint))
            {
                return GetAlignment(DType.UInt32);
            }
            else if (type == typeof(ulong))
            {
                return GetAlignment(DType.UInt64);
            }
            else if (type == typeof(object))
            {
                return GetAlignment(DType.Variant);
            }
            else if (type == typeof(IDBusObject))
            {
                return GetAlignment(DType.Variant);
            }

            if (ArgTypeInspector.IsDBusObjectType(type, isCompileTimeType: true))
            {
                return GetAlignment(DType.Variant);
            }

            Type elementType;
            if (ArgTypeInspector.InspectEnumerableType(type, out elementType, isCompileTimeType: true)
                != ArgTypeInspector.EnumerableType.NotEnumerable)
            {
                return GetAlignment(DType.Array);
            }

            if (ArgTypeInspector.IsStructType(type))
            {
                return GetAlignment(DType.StructBegin);
            }

            if (ArgTypeInspector.IsSafeHandleType(type))
            {
                return GetAlignment(DType.UnixFd);
            }

            throw new ArgumentException($"Cannot (de)serialize Type '{type.FullName}'");
        }

        public static int GetAlignment (DType dtype)
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
                    return 4;
                case DType.Int64:
                case DType.UInt64:
                    return 8;
                case DType.Single: //Not yet supported!
                    return 4;
                case DType.Double:
                    return 8;
                case DType.String:
                    return 4;
                case DType.ObjectPath:
                    return 4;
                case DType.Signature:
                    return 1;
                case DType.Array:
                    return 4;
                case DType.StructBegin:
                    return 8;
                case DType.Variant:
                    return 1;
                case DType.DictEntryBegin:
                    return 8;
                case DType.Invalid:
                default:
                    throw new ProtocolException("Cannot determine alignment of " + dtype);
            }
        }
    }
}

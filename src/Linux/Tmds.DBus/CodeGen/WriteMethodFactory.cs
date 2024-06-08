// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Reflection;
using Tmds.DBus.Protocol;

namespace Tmds.DBus.CodeGen
{
    internal delegate void WriteMethodDelegate<in T>(MessageWriter writer, T value);

    internal class WriteMethodFactory
    {
        public static WriteMethodDelegate<T> CreateWriteMethodDelegate<T>()
        {
            var type = typeof(T);
            var writeMethod = CreateWriteMethodForType(type, true);
            return (WriteMethodDelegate<T>)writeMethod.CreateDelegate(typeof(WriteMethodDelegate<T>));
        }

        public static MethodInfo CreateWriteMethodForType(Type type, bool isCompileTimeType) // void Write(MessageWriter, T)
        {
            if (type.GetTypeInfo().IsEnum)
            {
                type = Enum.GetUnderlyingType(type);
            }

            if (type == typeof(bool))
            {
                return s_messageWriterWriteBoolean;
            }
            else if (type == typeof(byte))
            {
                return s_messageWriterWriteByte;
            }
            else if (type == typeof(double))
            {
                return s_messageWriterWriteDouble;
            }
            else if (type == typeof(short))
            {
                return s_messageWriterWriteInt16;
            }
            else if (type == typeof(int))
            {
                return s_messageWriterWriteInt32;
            }
            else if (type == typeof(long))
            {
                return s_messageWriterWriteInt64;
            }
            else if (type == typeof(ObjectPath2))
            {
                return s_messageWriterWriteObjectPath;
            }
            else if (type == typeof(Signature))
            {
                return s_messageWriterWriteSignature;
            }
            else if (type == typeof(string))
            {
                return s_messageWriterWriteString;
            }
            else if (type == typeof(float))
            {
                return s_messageWriterWriteSingle;
            }
            else if (type == typeof(ushort))
            {
                return s_messageWriterWriteUInt16;
            }
            else if (type == typeof(uint))
            {
                return s_messageWriterWriteUInt32;
            }
            else if (type == typeof(ulong))
            {
                return s_messageWriterWriteUInt64;
            }
            else if (type == typeof(object))
            {
                return s_messageWriterWriteVariant;
            }
            else if (type == typeof(IDBusObject))
            {
                return s_messageWriterWriteBusObject;
            }

            if (ArgTypeInspector.IsDBusObjectType(type, isCompileTimeType))
            {
                return s_messageWriterWriteBusObject;
            }

            Type elementType;
            var enumerableType = ArgTypeInspector.InspectEnumerableType(type, out elementType, isCompileTimeType);
            if (enumerableType != ArgTypeInspector.EnumerableType.NotEnumerable)
            {
                if ((enumerableType == ArgTypeInspector.EnumerableType.EnumerableKeyValuePair) ||
                    (enumerableType == ArgTypeInspector.EnumerableType.GenericDictionary))
                {
                    return s_messageWriterWriteDict.MakeGenericMethod(elementType.GenericTypeArguments);
                }
                else if (enumerableType == ArgTypeInspector.EnumerableType.AttributeDictionary)
                {
                    return s_messageWriterWriteDictionaryObject.MakeGenericMethod(type);
                }
                else // Enumerable
                {
                    return s_messageWriterWriteArray.MakeGenericMethod(new[] { elementType });
                }
            }

            bool isValueTuple;
            if (ArgTypeInspector.IsStructType(type, out isValueTuple))
            {
                if (isValueTuple)
                {
                    return s_messageWriterWriteValueTupleStruct.MakeGenericMethod(type);
                }
                else
                {
                    return s_messageWriterWriteStruct.MakeGenericMethod(type);
                }
            }

            if (ArgTypeInspector.IsSafeHandleType(type))
            {
                return s_messageWriterWriteSafeHandle;
            }

            throw new ArgumentException($"Cannot (de)serialize Type '{type.FullName}'");
        }

        private static readonly MethodInfo s_messageWriterWriteBoolean = typeof(MessageWriter).GetMethod(nameof(MessageWriter.WriteBoolean));
        private static readonly MethodInfo s_messageWriterWriteByte = typeof(MessageWriter).GetMethod(nameof(MessageWriter.WriteByte));
        private static readonly MethodInfo s_messageWriterWriteDouble = typeof(MessageWriter).GetMethod(nameof(MessageWriter.WriteDouble));
        private static readonly MethodInfo s_messageWriterWriteInt16 = typeof(MessageWriter).GetMethod(nameof(MessageWriter.WriteInt16));
        private static readonly MethodInfo s_messageWriterWriteInt32 = typeof(MessageWriter).GetMethod(nameof(MessageWriter.WriteInt32));
        private static readonly MethodInfo s_messageWriterWriteInt64 = typeof(MessageWriter).GetMethod(nameof(MessageWriter.WriteInt64));
        private static readonly MethodInfo s_messageWriterWriteObjectPath = typeof(MessageWriter).GetMethod(nameof(MessageWriter.WriteObjectPath));
        private static readonly MethodInfo s_messageWriterWriteSignature = typeof(MessageWriter).GetMethod(nameof(MessageWriter.WriteSignature));
        private static readonly MethodInfo s_messageWriterWriteSafeHandle = typeof(MessageWriter).GetMethod(nameof(MessageWriter.WriteSafeHandle));
        private static readonly MethodInfo s_messageWriterWriteSingle = typeof(MessageWriter).GetMethod(nameof(MessageWriter.WriteSingle));
        private static readonly MethodInfo s_messageWriterWriteString = typeof(MessageWriter).GetMethod(nameof(MessageWriter.WriteString));
        private static readonly MethodInfo s_messageWriterWriteUInt16 = typeof(MessageWriter).GetMethod(nameof(MessageWriter.WriteUInt16));
        private static readonly MethodInfo s_messageWriterWriteUInt32 = typeof(MessageWriter).GetMethod(nameof(MessageWriter.WriteUInt32));
        private static readonly MethodInfo s_messageWriterWriteUInt64 = typeof(MessageWriter).GetMethod(nameof(MessageWriter.WriteUInt64));
        private static readonly MethodInfo s_messageWriterWriteVariant = typeof(MessageWriter).GetMethod(nameof(MessageWriter.WriteVariant));
        private static readonly MethodInfo s_messageWriterWriteBusObject = typeof(MessageWriter).GetMethod(nameof(MessageWriter.WriteBusObject));
        private static readonly MethodInfo s_messageWriterWriteArray = typeof(MessageWriter).GetMethod(nameof(MessageWriter.WriteArray));
        private static readonly MethodInfo s_messageWriterWriteDict = typeof(MessageWriter).GetMethod(nameof(MessageWriter.WriteFromDict));
        private static readonly MethodInfo s_messageWriterWriteDictionaryObject = typeof(MessageWriter).GetMethod(nameof(MessageWriter.WriteDictionaryObject));
        private static readonly MethodInfo s_messageWriterWriteStruct = typeof(MessageWriter).GetMethod(nameof(MessageWriter.WriteStructure));
        private static readonly MethodInfo s_messageWriterWriteValueTupleStruct = typeof(MessageWriter).GetMethod(nameof(MessageWriter.WriteValueTupleStructure));
    }
}

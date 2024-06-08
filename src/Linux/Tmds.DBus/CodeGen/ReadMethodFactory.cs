// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.Reflection;
using Tmds.DBus.Protocol;

namespace Tmds.DBus.CodeGen
{
    internal delegate T ReadMethodDelegate<out T>(MessageReader reader);

    internal class ReadMethodFactory
    {
        public static ReadMethodDelegate<T> CreateReadMethodDelegate<T>()
        {
            var type = typeof(T);
            var readMethod = CreateReadMethodForType(type);
            return (ReadMethodDelegate<T>)readMethod.CreateDelegate(typeof(ReadMethodDelegate<T>));
        }

        public static MethodInfo CreateReadMethodForType(Type type) // Type Read(MessageReader)
        {
            if (type.GetTypeInfo().IsEnum)
            {
                return s_messageReaderReadEnum.MakeGenericMethod(new[] { type });
            }

            if (type == typeof(bool))
            {
                return s_messageReaderReadBoolean;
            }
            else if (type == typeof(byte))
            {
                return s_messageReaderReadByte;
            }
            else if (type == typeof(double))
            {
                return s_messageReaderReadDouble;
            }
            else if (type == typeof(short))
            {
                return s_messageReaderReadInt16;
            }
            else if (type == typeof(int))
            {
                return s_messageReaderReadInt32;
            }
            else if (type == typeof(long))
            {
                return s_messageReaderReadInt64;
            }
            else if (type == typeof(ObjectPath2))
            {
                return s_messageReaderReadObjectPath;
            }
            else if (type == typeof(Signature))
            {
                return s_messageReaderReadSignature;
            }
            else if (type == typeof(string))
            {
                return s_messageReaderReadString;
            }
            else if (type == typeof(float))
            {
                return s_messageReaderReadSingle;
            }
            else if (type == typeof(ushort))
            {
                return s_messageReaderReadUInt16;
            }
            else if (type == typeof(uint))
            {
                return s_messageReaderReadUInt32;
            }
            else if (type == typeof(ulong))
            {
                return s_messageReaderReadUInt64;
            }
            else if (type == typeof(object))
            {
                return s_messageReaderReadVariant;
            }
            else if (type == typeof(IDBusObject))
            {
                return s_messageReaderReadBusObject;
            }

            if (ArgTypeInspector.IsDBusObjectType(type, isCompileTimeType: true))
            {
                return s_messageReaderReadDBusInterface.MakeGenericMethod(new[] { type });
            }

            Type elementType;
            var enumerableType = ArgTypeInspector.InspectEnumerableType(type, out elementType, isCompileTimeType: true);
            if (enumerableType != ArgTypeInspector.EnumerableType.NotEnumerable)
            {
                if (enumerableType == ArgTypeInspector.EnumerableType.GenericDictionary)
                {
                    TypeInfo elementTypeInfo = elementType.GetTypeInfo();
                    Type keyType = elementTypeInfo.GenericTypeArguments[0];
                    Type valueType = elementTypeInfo.GenericTypeArguments[1];
                    return s_messageReaderReadDictionary.MakeGenericMethod(new[] { keyType, valueType });
                }
                else if (enumerableType == ArgTypeInspector.EnumerableType.AttributeDictionary)
                {
                    return s_messageReaderReadDictionaryObject.MakeGenericMethod(new[] { type });
                }
                else // Enumerable, EnumerableKeyValuePair
                {
                    return s_messageReaderReadArray.MakeGenericMethod(new[] { elementType });
                }
            }

            bool isValueTuple;
            if (ArgTypeInspector.IsStructType(type, out isValueTuple))
            {
                if (isValueTuple)
                {
                    return s_messageReaderReadValueTupleStruct.MakeGenericMethod(type);
                }
                else
                {
                    return s_messageReaderReadStruct.MakeGenericMethod(type);
                }
            }

            if (ArgTypeInspector.IsSafeHandleType(type))
            {
                return s_messageReaderReadSafeHandle.MakeGenericMethod(type);
            }

            throw new ArgumentException($"Cannot (de)serialize Type '{type.FullName}'");
        }

        private static readonly MethodInfo s_messageReaderReadEnum = typeof(MessageReader).GetMethod(nameof(MessageReader.ReadEnum));
        private static readonly MethodInfo s_messageReaderReadBoolean = typeof(MessageReader).GetMethod(nameof(MessageReader.ReadBoolean));
        private static readonly MethodInfo s_messageReaderReadByte = typeof(MessageReader).GetMethod(nameof(MessageReader.ReadByte));
        private static readonly MethodInfo s_messageReaderReadDouble = typeof(MessageReader).GetMethod(nameof(MessageReader.ReadDouble));
        private static readonly MethodInfo s_messageReaderReadInt16 = typeof(MessageReader).GetMethod(nameof(MessageReader.ReadInt16));
        private static readonly MethodInfo s_messageReaderReadInt32 = typeof(MessageReader).GetMethod(nameof(MessageReader.ReadInt32));
        private static readonly MethodInfo s_messageReaderReadInt64 = typeof(MessageReader).GetMethod(nameof(MessageReader.ReadInt64));
        private static readonly MethodInfo s_messageReaderReadObjectPath = typeof(MessageReader).GetMethod(nameof(MessageReader.ReadObjectPath));
        private static readonly MethodInfo s_messageReaderReadSignature = typeof(MessageReader).GetMethod(nameof(MessageReader.ReadSignature));
        private static readonly MethodInfo s_messageReaderReadSafeHandle = typeof(MessageReader).GetMethod(nameof(MessageReader.ReadSafeHandle));
        private static readonly MethodInfo s_messageReaderReadSingle = typeof(MessageReader).GetMethod(nameof(MessageReader.ReadSingle));
        private static readonly MethodInfo s_messageReaderReadString = typeof(MessageReader).GetMethod(nameof(MessageReader.ReadString));
        private static readonly MethodInfo s_messageReaderReadUInt16 = typeof(MessageReader).GetMethod(nameof(MessageReader.ReadUInt16));
        private static readonly MethodInfo s_messageReaderReadUInt32 = typeof(MessageReader).GetMethod(nameof(MessageReader.ReadUInt32));
        private static readonly MethodInfo s_messageReaderReadUInt64 = typeof(MessageReader).GetMethod(nameof(MessageReader.ReadUInt64));
        private static readonly MethodInfo s_messageReaderReadVariant = typeof(MessageReader).GetMethod(nameof(MessageReader.ReadVariant));
        private static readonly MethodInfo s_messageReaderReadBusObject = typeof(MessageReader).GetMethod(nameof(MessageReader.ReadBusObject));
        private static readonly MethodInfo s_messageReaderReadDictionary = typeof(MessageReader).GetMethod(nameof(MessageReader.ReadDictionary), Type.EmptyTypes);
        private static readonly MethodInfo s_messageReaderReadDictionaryObject = typeof(MessageReader).GetMethod(nameof(MessageReader.ReadDictionaryObject), Type.EmptyTypes);
        private static readonly MethodInfo s_messageReaderReadArray = typeof(MessageReader).GetMethod(nameof(MessageReader.ReadArray), Type.EmptyTypes);
        private static readonly MethodInfo s_messageReaderReadStruct = typeof(MessageReader).GetMethod(nameof(MessageReader.ReadStruct), Type.EmptyTypes);
        private static readonly MethodInfo s_messageReaderReadValueTupleStruct = typeof(MessageReader).GetMethod(nameof(MessageReader.ReadValueTupleStruct), Type.EmptyTypes);
        private static readonly MethodInfo s_messageReaderReadDBusInterface = typeof(MessageReader).GetMethod(nameof(MessageReader.ReadDBusInterface));
    }
}

// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Tmds.DBus.CodeGen
{
    static internal class ArgTypeInspector
    {
        private static readonly Type s_idbusObjectType = typeof(IDBusObject);
        private static readonly Type s_idictionaryGenericType = typeof(IDictionary<,>);
        private static readonly Type s_keyValueGenericPairType = typeof(KeyValuePair<,>);
        private static readonly Type s_ienumerableGenricType = typeof(IEnumerable<>);
        private static readonly Type s_ienumerableType = typeof(System.Collections.IEnumerable);
        private static readonly Type s_objectType = typeof(Object);
        private static readonly Type s_valueType = typeof(ValueType);
        private static readonly Type s_ilistGenricType = typeof(IList<>);
        private static readonly Type s_icollectionGenricType = typeof(ICollection<>);
        private static readonly Type s_stringObjectKeyValuePairType = typeof(KeyValuePair<string, object>);
        private static readonly Type[] s_valueTupleTypes = new [] {
            typeof(ValueTuple<>),
            typeof(ValueTuple<,>),
            typeof(ValueTuple<,,>),
            typeof(ValueTuple<,,,>),
            typeof(ValueTuple<,,,,>),
            typeof(ValueTuple<,,,,,>),
            typeof(ValueTuple<,,,,,,>),
            typeof(ValueTuple<,,,,,,,>)
        };
        private static readonly IComparer<FieldInfo> s_valueTupleFieldComparer = new StructFieldInfoComparer(true);
        private static readonly IComparer<FieldInfo> s_otherFieldComparer = new StructFieldInfoComparer(false);

        public static bool IsDBusObjectType(Type type, bool isCompileTimeType)
        {
            if (type == s_idbusObjectType)
            {
                return true;
            }
            var typeInfo = type.GetTypeInfo();
            if (isCompileTimeType)
            {
                return typeInfo.IsInterface && typeInfo.ImplementedInterfaces.Contains(s_idbusObjectType);
            }
            else
            {
                return typeInfo.ImplementedInterfaces.Contains(s_idbusObjectType);
            }
        }

        public static bool IsSafeHandleType(Type type)
        {
            return type.GetTypeInfo().IsSubclassOf(typeof(SafeHandle));
        }

        public enum EnumerableType
        {
            NotEnumerable,
            Enumerable,             // IEnumerable
            EnumerableKeyValuePair, // IEnumerable<KeyValuePair>
            GenericDictionary,      // IDictionary
            AttributeDictionary     // AttributeDictionary
        }

        public static EnumerableType InspectEnumerableType(Type type, out Type elementType, bool isCompileTimeType)
        {
            elementType = null;
            var typeInfo = type.GetTypeInfo();

            if (isCompileTimeType)
            {
                if (typeInfo.IsArray)
                {
                    elementType = typeInfo.GetElementType();
                    return InspectElementType(elementType);
                }
                if (typeInfo.IsInterface && typeInfo.IsGenericType)
                {
                    var genericTypeDefinition = typeInfo.GetGenericTypeDefinition();
                    if (genericTypeDefinition == s_idictionaryGenericType)
                    {
                        elementType = s_keyValueGenericPairType.MakeGenericType(typeInfo.GenericTypeArguments);
                        return EnumerableType.GenericDictionary;
                    }
                    else if (genericTypeDefinition == s_ienumerableGenricType ||
                             genericTypeDefinition == s_ilistGenricType ||
                             genericTypeDefinition == s_icollectionGenricType)
                    {
                        elementType = typeInfo.GenericTypeArguments[0];
                        return InspectElementType(elementType);
                    }
                    else
                    {
                        return EnumerableType.NotEnumerable;
                    }
                }
                var dictionaryAttribute = typeInfo.GetCustomAttribute<DictionaryAttribute>(false);
                if (dictionaryAttribute != null)
                {
                    elementType = s_stringObjectKeyValuePairType;
                    return EnumerableType.AttributeDictionary;
                }
                return EnumerableType.NotEnumerable;
            }
            else
            {
                if (typeInfo.ImplementedInterfaces.Contains(s_idbusObjectType))
                {
                    return EnumerableType.NotEnumerable;
                }

                var dictionaryAttribute = typeInfo.GetCustomAttribute<DictionaryAttribute>(false);
                if (dictionaryAttribute != null)
                {
                    elementType = s_stringObjectKeyValuePairType;
                    return EnumerableType.AttributeDictionary;
                }

                if (!typeInfo.ImplementedInterfaces.Contains(s_ienumerableType))
                {
                    return EnumerableType.NotEnumerable;
                }

                var enumerableTypes = from interf in typeInfo.ImplementedInterfaces
                                    let interfTypeinfo = interf.GetTypeInfo()
                                    where interfTypeinfo.IsGenericType && interfTypeinfo.GetGenericTypeDefinition() == s_ienumerableGenricType
                                    select interfTypeinfo;

                var enumerableCount = enumerableTypes.Count();

                if (enumerableCount == 1)
                {
                    elementType = enumerableTypes.First().GenericTypeArguments[0];
                    return InspectElementType(elementType);
                }
                else
                {
                    throw new ArgumentException($"Cannot determine element type of enumerable type '$type.FullName'");
                }
            }
        }

        public static bool IsStructType(Type type)
        {
            bool isValueTuple;
            return IsStructType(type, out isValueTuple);
        }

        public static bool IsStructType(Type type, out bool isValueTuple)
        {
            isValueTuple = false;
            var typeInfo = type.GetTypeInfo();
            if (typeInfo.IsPointer ||
                typeInfo.IsInterface ||
                typeInfo.IsArray ||
                typeInfo.IsPrimitive ||
                typeInfo.IsAbstract)
            {
                return false;
            }
            if (IsValueTuple(typeInfo))
            {
                isValueTuple = true;
                return true;
            }
            else if (!typeInfo.IsLayoutSequential)
            {
                return false;
            }

            if (typeInfo.ImplementedInterfaces.Contains(s_idbusObjectType))
            {
                return false;
            }

            if (typeInfo.ImplementedInterfaces.Contains(s_ienumerableType))
            {
                return false;
            }

            if (typeInfo.BaseType != s_objectType &&
                typeInfo.BaseType != s_valueType)
            {
                return false;
            }

            var fields = type.GetFields (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (!fields.Any())
            {
                return false;
            }

            return true;
        }

        public static FieldInfo[] GetStructFields(Type structType, bool isValueTuple)
        {
            var fields = structType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Array.Sort<FieldInfo, object>(fields, null, isValueTuple ? s_valueTupleFieldComparer : s_otherFieldComparer);
            return fields;
        }

        private static EnumerableType InspectElementType(Type elementType)
        {
            var typeInfo = elementType.GetTypeInfo();
            bool isKeyValuePair = typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == s_keyValueGenericPairType;
            if (isKeyValuePair)
            {
                return EnumerableType.EnumerableKeyValuePair;
            }
            else
            {
                return EnumerableType.Enumerable;
            }
        }

        private static bool IsValueTuple(TypeInfo typeInfo)
        {
            if (typeInfo.IsGenericType)
            {
                var genericTypeDefinition = typeInfo.GetGenericTypeDefinition();
                return s_valueTupleTypes.Contains(genericTypeDefinition);
            }
            else
            {
                return false;
            }
        }

        private class StructFieldInfoComparer : IComparer<FieldInfo>
        {
            private bool _isValueTypleComparer;
            public StructFieldInfoComparer(bool isValueTypleComparer)
            {
                _isValueTypleComparer = isValueTypleComparer;
            }

            public int Compare(FieldInfo x, FieldInfo y)
            {
                if (_isValueTypleComparer)
                {
                    // ValueTuples are not layout sequentially
                    // The fields are named Item[1-7], Rest
                    if (x.Name.Length == 4)
                    {
                        return 1;
                    }
                    return x.Name[x.Name.Length - 1].CompareTo(y.Name[y.Name.Length - 1]);
                }
                else
                {
                    return x.MetadataToken.CompareTo(y.MetadataToken);
                }
            }
        }
    }
}
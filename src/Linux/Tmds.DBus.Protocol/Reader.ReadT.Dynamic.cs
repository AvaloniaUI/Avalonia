namespace Tmds.DBus.Protocol;

// Code in this file is not trimmer friendly.
#pragma warning disable IL3050
#pragma warning disable IL2055
#pragma warning disable IL2091
#pragma warning disable IL2026
// Using obsolete generic read members
#pragma warning disable CS0618

public ref partial struct Reader
{
    interface ITypeReader
    { }

    interface ITypeReader<T> : ITypeReader
    {
        T Read(ref Reader reader);
    }

    private T ReadDynamic<T>()
    {
        Type type = typeof(T);

        if (type == typeof(object))
        {
            Utf8Span signature = ReadSignature();
            type = DetermineVariantType(signature);

            if (type == typeof(byte))
            {
                return (T)(object)ReadByte();
            }
            else if (type == typeof(bool))
            {
                return (T)(object)ReadBool();
            }
            else if (type == typeof(short))
            {
                return (T)(object)ReadInt16();
            }
            else if (type == typeof(ushort))
            {
                return (T)(object)ReadUInt16();
            }
            else if (type == typeof(int))
            {
                return (T)(object)ReadInt32();
            }
            else if (type == typeof(uint))
            {
                return (T)(object)ReadUInt32();
            }
            else if (type == typeof(long))
            {
                return (T)(object)ReadInt64();
            }
            else if (type == typeof(ulong))
            {
                return (T)(object)ReadUInt64();
            }
            else if (type == typeof(double))
            {
                return (T)(object)ReadDouble();
            }
            else if (type == typeof(string))
            {
                return (T)(object)ReadString();
            }
            else if (type == typeof(ObjectPath))
            {
                return (T)(object)ReadObjectPath();
            }
            else if (type == typeof(Signature))
            {
                return (T)(object)ReadSignatureAsSignature();
            }
            else if (type == typeof(SafeHandle))
            {
                return (T)(object)ReadHandle<CloseSafeHandle>()!;
            }
            else if (type == typeof(VariantValue))
            {
                return (T)(object)ReadVariantValue();
            }
        }

        var typeReader = (ITypeReader<T>)TypeReaders.GetTypeReader(type);
        return typeReader.Read(ref this);
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL3050")]
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2055")]
    private static Type DetermineVariantType(Utf8Span signature)
    {
        Func<DBusType, Type[], Type> map = (dbusType, innerTypes) =>
        {
            switch (dbusType)
            {
                case DBusType.Byte: return typeof(byte);
                case DBusType.Bool: return typeof(bool);
                case DBusType.Int16: return typeof(short);
                case DBusType.UInt16: return typeof(ushort);
                case DBusType.Int32: return typeof(int);
                case DBusType.UInt32: return typeof(uint);
                case DBusType.Int64: return typeof(long);
                case DBusType.UInt64: return typeof(ulong);
                case DBusType.Double: return typeof(double);
                case DBusType.String: return typeof(string);
                case DBusType.ObjectPath: return typeof(ObjectPath);
                case DBusType.Signature: return typeof(Signature);
                case DBusType.UnixFd: return typeof(SafeHandle);
                case DBusType.Array: return innerTypes[0].MakeArrayType();
                case DBusType.DictEntry: return typeof(Dictionary<,>).MakeGenericType(innerTypes);
                case DBusType.Struct:
                    switch (innerTypes.Length)
                    {
                        case 1: return typeof(ValueTuple<>).MakeGenericType(innerTypes);
                        case 2: return typeof(ValueTuple<,>).MakeGenericType(innerTypes);
                        case 3: return typeof(ValueTuple<,,>).MakeGenericType(innerTypes);
                        case 4: return typeof(ValueTuple<,,,>).MakeGenericType(innerTypes);
                        case 5: return typeof(ValueTuple<,,,,>).MakeGenericType(innerTypes);
                        case 6: return typeof(ValueTuple<,,,,,>).MakeGenericType(innerTypes);
                        case 7: return typeof(ValueTuple<,,,,,,>).MakeGenericType(innerTypes);
                        case 8:
                        case 9:
                        case 10:
                            var types = new Type[8];
                            innerTypes.AsSpan(0, 7).CopyTo(types);
                            types[7] = innerTypes.Length switch
                            {
                                8 => typeof(ValueTuple<>).MakeGenericType(new[] { innerTypes[7] }),
                                9 => typeof(ValueTuple<,>).MakeGenericType(new[] { innerTypes[7], innerTypes[8] }),
                                10 => typeof(ValueTuple<,,>).MakeGenericType(new[] { innerTypes[7], innerTypes[8], innerTypes[9] }),
                                _ => null!
                            };
                            return typeof(ValueTuple<,,,,,,,>).MakeGenericType(types);
                    }
                    break;
            }
            return typeof(object);
        };

        // TODO (perf): add caching.
        return SignatureReader.Transform(signature, map);
    }

    [Obsolete(Strings.AddTypeReaderMethodObsolete)]
    public static void AddArrayTypeReader<T>()
        where T : notnull
    { }

    [Obsolete(Strings.AddTypeReaderMethodObsolete)]
    public static void AddKeyValueArrayTypeReader<TKey, TValue>()
        where TKey : notnull
        where TValue : notnull
    { }

    [Obsolete(Strings.AddTypeReaderMethodObsolete)]
    public static void AddDictionaryTypeReader<TKey, TValue>()
        where TKey : notnull
        where TValue : notnull
    { }

    [Obsolete(Strings.AddTypeReaderMethodObsolete)]
    public static void AddValueTupleTypeReader<T1>()
        where T1 : notnull
    { }

    [Obsolete(Strings.AddTypeReaderMethodObsolete)]
    public static void AddTupleTypeReader<T1>()
        where T1 : notnull
    { }

    [Obsolete(Strings.AddTypeReaderMethodObsolete)]
    public static void AddValueTupleTypeReader<T1, T2>()
        where T1 : notnull
        where T2 : notnull
    { }

    [Obsolete(Strings.AddTypeReaderMethodObsolete)]
    public static void AddTupleTypeReader<T1, T2>()
        where T1 : notnull
        where T2 : notnull
    { }

    [Obsolete(Strings.AddTypeReaderMethodObsolete)]
    public static void AddValueTupleTypeReader<T1, T2, T3>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
    { }

    [Obsolete(Strings.AddTypeReaderMethodObsolete)]
    public static void AddTupleTypeReader<T1, T2, T3>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
    { }

    [Obsolete(Strings.AddTypeReaderMethodObsolete)]
    public static void AddValueTupleTypeReader<T1, T2, T3, T4>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
    { }

    [Obsolete(Strings.AddTypeReaderMethodObsolete)]
    public static void AddTupleTypeReader<T1, T2, T3, T4>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
    { }

    [Obsolete(Strings.AddTypeReaderMethodObsolete)]
    public static void AddValueTupleTypeReader<T1, T2, T3, T4, T5>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
    { }

    [Obsolete(Strings.AddTypeReaderMethodObsolete)]
    public static void AddTupleTypeReader<T1, T2, T3, T4, T5>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
    { }

    [Obsolete(Strings.AddTypeReaderMethodObsolete)]
    public static void AddValueTupleTypeReader<T1, T2, T3, T4, T5, T6>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
    { }

    [Obsolete(Strings.AddTypeReaderMethodObsolete)]
    public static void AddTupleTypeReader<T1, T2, T3, T4, T5, T6>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
    { }

    [Obsolete(Strings.AddTypeReaderMethodObsolete)]
    public static void AddValueTupleTypeReader<T1, T2, T3, T4, T5, T6, T7>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
    { }

    [Obsolete(Strings.AddTypeReaderMethodObsolete)]
    public static void AddTupleTypeReader<T1, T2, T3, T4, T5, T6, T7>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
    { }

    [Obsolete(Strings.AddTypeReaderMethodObsolete)]
    public static void AddValueTupleTypeReader<T1, T2, T3, T4, T5, T6, T7, T8>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
        where T8 : notnull
    { }

    [Obsolete(Strings.AddTypeReaderMethodObsolete)]
    public static void AddTupleTypeReader<T1, T2, T3, T4, T5, T6, T7, T8>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
        where T8 : notnull
    { }

    [Obsolete(Strings.AddTypeReaderMethodObsolete)]
    public static void AddValueTupleTypeReader<T1, T2, T3, T4, T5, T6, T7, T8, T9>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
        where T8 : notnull
        where T9 : notnull
    { }

    [Obsolete(Strings.AddTypeReaderMethodObsolete)]
    public static void AddTupleTypeReader<T1, T2, T3, T4, T5, T6, T7, T8, T9>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
        where T8 : notnull
        where T9 : notnull
    { }

    [Obsolete(Strings.AddTypeReaderMethodObsolete)]
    public static void AddValueTupleTypeReader<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
        where T8 : notnull
        where T9 : notnull
        where T10 : notnull
    { }

    [Obsolete(Strings.AddTypeReaderMethodObsolete)]
    public static void AddTupleTypeReader<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
        where T8 : notnull
        where T9 : notnull
        where T10 : notnull
    { }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL3050")]
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2091")]
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026")]
    static class TypeReaders
    {
        private static readonly Dictionary<Type, ITypeReader> _typeReaders = new();

        public static ITypeReader GetTypeReader(Type type)
        {
            lock (_typeReaders)
            {
                if (_typeReaders.TryGetValue(type, out ITypeReader? reader))
                {
                    return reader;
                }
                reader = CreateReaderForType(type);
                _typeReaders.Add(type, reader);
                return reader;
            }
        }

        private static ITypeReader CreateReaderForType(Type type)
        {
            // Array
            if (type.IsArray)
            {
                return CreateArrayTypeReader(type.GetElementType()!);
            }

            // Dictionary<.>
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                Type keyType = type.GenericTypeArguments[0];
                Type valueType = type.GenericTypeArguments[1];
                return CreateDictionaryTypeReader(keyType, valueType);
            }

            // Struct (ValueTuple)
            if (type.IsGenericType && type.FullName!.StartsWith("System.ValueTuple"))
            {
                switch (type.GenericTypeArguments.Length)
                {
                    case 1: return CreateValueTupleTypeReader(type.GenericTypeArguments[0]);
                    case 2:
                        return CreateValueTupleTypeReader(type.GenericTypeArguments[0],
                                                          type.GenericTypeArguments[1]);
                    case 3:
                        return CreateValueTupleTypeReader(type.GenericTypeArguments[0],
                                                          type.GenericTypeArguments[1],
                                                          type.GenericTypeArguments[2]);
                    case 4:
                        return CreateValueTupleTypeReader(type.GenericTypeArguments[0],
                                                          type.GenericTypeArguments[1],
                                                          type.GenericTypeArguments[2],
                                                          type.GenericTypeArguments[3]);
                    case 5:
                        return CreateValueTupleTypeReader(type.GenericTypeArguments[0],
                                                          type.GenericTypeArguments[1],
                                                          type.GenericTypeArguments[2],
                                                          type.GenericTypeArguments[3],
                                                          type.GenericTypeArguments[4]);

                    case 6:
                        return CreateValueTupleTypeReader(type.GenericTypeArguments[0],
                                                     type.GenericTypeArguments[1],
                                                     type.GenericTypeArguments[2],
                                                     type.GenericTypeArguments[3],
                                                     type.GenericTypeArguments[4],
                                                     type.GenericTypeArguments[5]);
                    case 7:
                        return CreateValueTupleTypeReader(type.GenericTypeArguments[0],
                                                     type.GenericTypeArguments[1],
                                                     type.GenericTypeArguments[2],
                                                     type.GenericTypeArguments[3],
                                                     type.GenericTypeArguments[4],
                                                     type.GenericTypeArguments[5],
                                                     type.GenericTypeArguments[6]);
                    case 8:
                        switch (type.GenericTypeArguments[7].GenericTypeArguments.Length)
                        {
                            case 1:
                                return CreateValueTupleTypeReader(type.GenericTypeArguments[0],
                                                            type.GenericTypeArguments[1],
                                                            type.GenericTypeArguments[2],
                                                            type.GenericTypeArguments[3],
                                                            type.GenericTypeArguments[4],
                                                            type.GenericTypeArguments[5],
                                                            type.GenericTypeArguments[6],
                                                            type.GenericTypeArguments[7].GenericTypeArguments[0]);
                            case 2:
                                return CreateValueTupleTypeReader(type.GenericTypeArguments[0],
                                                            type.GenericTypeArguments[1],
                                                            type.GenericTypeArguments[2],
                                                            type.GenericTypeArguments[3],
                                                            type.GenericTypeArguments[4],
                                                            type.GenericTypeArguments[5],
                                                            type.GenericTypeArguments[6],
                                                            type.GenericTypeArguments[7].GenericTypeArguments[0],
                                                            type.GenericTypeArguments[7].GenericTypeArguments[1]);
                            case 3:
                                return CreateValueTupleTypeReader(type.GenericTypeArguments[0],
                                                            type.GenericTypeArguments[1],
                                                            type.GenericTypeArguments[2],
                                                            type.GenericTypeArguments[3],
                                                            type.GenericTypeArguments[4],
                                                            type.GenericTypeArguments[5],
                                                            type.GenericTypeArguments[6],
                                                            type.GenericTypeArguments[7].GenericTypeArguments[0],
                                                            type.GenericTypeArguments[7].GenericTypeArguments[1],
                                                            type.GenericTypeArguments[7].GenericTypeArguments[2]);
                        }
                        break;
                }
            }
            // Struct (ValueTuple)
            if (type.IsGenericType && type.FullName!.StartsWith("System.Tuple"))
            {
                switch (type.GenericTypeArguments.Length)
                {
                    case 1: return CreateTupleTypeReader(type.GenericTypeArguments[0]);
                    case 2:
                        return CreateTupleTypeReader(type.GenericTypeArguments[0],
                                                     type.GenericTypeArguments[1]);
                    case 3:
                        return CreateTupleTypeReader(type.GenericTypeArguments[0],
                                                     type.GenericTypeArguments[1],
                                                     type.GenericTypeArguments[2]);
                    case 4:
                        return CreateTupleTypeReader(type.GenericTypeArguments[0],
                                                     type.GenericTypeArguments[1],
                                                     type.GenericTypeArguments[2],
                                                     type.GenericTypeArguments[3]);
                    case 5:
                        return CreateTupleTypeReader(type.GenericTypeArguments[0],
                                                     type.GenericTypeArguments[1],
                                                     type.GenericTypeArguments[2],
                                                     type.GenericTypeArguments[3],
                                                     type.GenericTypeArguments[4]);
                    case 6:
                        return CreateTupleTypeReader(type.GenericTypeArguments[0],
                                                     type.GenericTypeArguments[1],
                                                     type.GenericTypeArguments[2],
                                                     type.GenericTypeArguments[3],
                                                     type.GenericTypeArguments[4],
                                                     type.GenericTypeArguments[5]);
                    case 7:
                        return CreateTupleTypeReader(type.GenericTypeArguments[0],
                                                     type.GenericTypeArguments[1],
                                                     type.GenericTypeArguments[2],
                                                     type.GenericTypeArguments[3],
                                                     type.GenericTypeArguments[4],
                                                     type.GenericTypeArguments[5],
                                                     type.GenericTypeArguments[6]);
                    case 8:
                        switch (type.GenericTypeArguments[7].GenericTypeArguments.Length)
                        {
                            case 1:
                                return CreateTupleTypeReader(type.GenericTypeArguments[0],
                                                            type.GenericTypeArguments[1],
                                                            type.GenericTypeArguments[2],
                                                            type.GenericTypeArguments[3],
                                                            type.GenericTypeArguments[4],
                                                            type.GenericTypeArguments[5],
                                                            type.GenericTypeArguments[6],
                                                            type.GenericTypeArguments[7].GenericTypeArguments[0]);
                            case 2:
                                return CreateTupleTypeReader(type.GenericTypeArguments[0],
                                                            type.GenericTypeArguments[1],
                                                            type.GenericTypeArguments[2],
                                                            type.GenericTypeArguments[3],
                                                            type.GenericTypeArguments[4],
                                                            type.GenericTypeArguments[5],
                                                            type.GenericTypeArguments[6],
                                                            type.GenericTypeArguments[7].GenericTypeArguments[0],
                                                            type.GenericTypeArguments[7].GenericTypeArguments[1]);
                            case 3:
                                return CreateTupleTypeReader(type.GenericTypeArguments[0],
                                                            type.GenericTypeArguments[1],
                                                            type.GenericTypeArguments[2],
                                                            type.GenericTypeArguments[3],
                                                            type.GenericTypeArguments[4],
                                                            type.GenericTypeArguments[5],
                                                            type.GenericTypeArguments[6],
                                                            type.GenericTypeArguments[7].GenericTypeArguments[0],
                                                            type.GenericTypeArguments[7].GenericTypeArguments[1],
                                                            type.GenericTypeArguments[7].GenericTypeArguments[2]);
                        }
                        break;
                }
            }

            ThrowNotSupportedType(type);
            return default!;
        }

        sealed class KeyValueArrayTypeReader<TKey, TValue> : ITypeReader<KeyValuePair<TKey, TValue>[]>, ITypeReader<object>
            where TKey : notnull
            where TValue : notnull
        {
            object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

            public KeyValuePair<TKey, TValue>[] Read(ref Reader reader)
            {
                List<KeyValuePair<TKey, TValue>> items = new();
                ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
                while (reader.HasNext(arrayEnd))
                {
                    TKey key = reader.Read<TKey>();
                    TValue value = reader.Read<TValue>();
                    items.Add(new KeyValuePair<TKey, TValue>(key, value));
                }
                return items.ToArray();
            }
        }

        sealed class ArrayTypeReader<T> : ITypeReader<T[]>, ITypeReader<object>
            where T : notnull
        {
            object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

            public T[] Read(ref Reader reader)
            {
                return reader.ReadArray<T>();
            }
        }

        private static ITypeReader CreateArrayTypeReader(Type elementType)
        {
            Type readerType;
            if (elementType.IsGenericType && elementType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                Type keyType = elementType.GenericTypeArguments[0];
                Type valueType = elementType.GenericTypeArguments[1];
                readerType = typeof(KeyValueArrayTypeReader<,>).MakeGenericType(new[] { keyType, valueType });
            }
            else
            {
                readerType = typeof(ArrayTypeReader<>).MakeGenericType(new[] { elementType });
            }
            return (ITypeReader)Activator.CreateInstance(readerType)!;
        }

        sealed class DictionaryTypeReader<TKey, TValue> : ITypeReader<Dictionary<TKey, TValue>>, ITypeReader<object>
            where TKey : notnull
            where TValue : notnull
        {
            object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

            public Dictionary<TKey, TValue> Read(ref Reader reader)
            {
                return reader.ReadDictionary<TKey, TValue>();
            }
        }

        private static ITypeReader CreateDictionaryTypeReader(Type keyType, Type valueType)
        {
            Type readerType = typeof(DictionaryTypeReader<,>).MakeGenericType(new[] { keyType, valueType });
            return (ITypeReader)Activator.CreateInstance(readerType)!;
        }

        sealed class ValueTupleTypeReader<T1> : ITypeReader<ValueTuple<T1>>, ITypeReader<object>
            where T1 : notnull
        {
            object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

            public ValueTuple<T1> Read(ref Reader reader)
            {
                return reader.ReadStruct<T1>();
            }
        }

        sealed class TupleTypeReader<T1> : ITypeReader<Tuple<T1>>, ITypeReader<object>
            where T1 : notnull
        {
            object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

            public Tuple<T1> Read(ref Reader reader)
            {
                return reader.ReadStructAsTuple<T1>();
            }
        }

        private static ITypeReader CreateValueTupleTypeReader(Type type1)
        {
            Type readerType = typeof(ValueTupleTypeReader<>).MakeGenericType(new[] { type1 });
            return (ITypeReader)Activator.CreateInstance(readerType)!;
        }

        private static ITypeReader CreateTupleTypeReader(Type type1)
        {
            Type readerType = typeof(TupleTypeReader<>).MakeGenericType(new[] { type1 });
            return (ITypeReader)Activator.CreateInstance(readerType)!;
        }

        sealed class ValueTupleTypeReader<T1, T2> : ITypeReader<ValueTuple<T1, T2>>, ITypeReader<object>
            where T1 : notnull
            where T2 : notnull
        {
            object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

            public ValueTuple<T1, T2> Read(ref Reader reader)
            {
                return reader.ReadStruct<T1, T2>();
            }
        }

        sealed class TupleTypeReader<T1, T2> : ITypeReader<Tuple<T1, T2>>, ITypeReader<object>
            where T1 : notnull
            where T2 : notnull
        {
            object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

            public Tuple<T1, T2> Read(ref Reader reader)
            {
                return reader.ReadStructAsTuple<T1, T2>();
            }
        }

        private static ITypeReader CreateValueTupleTypeReader(Type type1, Type type2)
        {
            Type readerType = typeof(ValueTupleTypeReader<,>).MakeGenericType(new[] { type1, type2 });
            return (ITypeReader)Activator.CreateInstance(readerType)!;
        }

        private static ITypeReader CreateTupleTypeReader(Type type1, Type type2)
        {
            Type readerType = typeof(TupleTypeReader<,>).MakeGenericType(new[] { type1, type2 });
            return (ITypeReader)Activator.CreateInstance(readerType)!;
        }

        sealed class ValueTupleTypeReader<T1, T2, T3> : ITypeReader<ValueTuple<T1, T2, T3>>, ITypeReader<object>
            where T1 : notnull
            where T2 : notnull
            where T3 : notnull
        {
            object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

            public ValueTuple<T1, T2, T3> Read(ref Reader reader)
            {
                return reader.ReadStruct<T1, T2, T3>();
            }
        }

        sealed class TupleTypeReader<T1, T2, T3> : ITypeReader<Tuple<T1, T2, T3>>, ITypeReader<object>
            where T1 : notnull
            where T2 : notnull
            where T3 : notnull
        {
            object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

            public Tuple<T1, T2, T3> Read(ref Reader reader)
            {
                return reader.ReadStructAsTuple<T1, T2, T3>();
            }
        }

        private static ITypeReader CreateValueTupleTypeReader(Type type1, Type type2, Type type3)
        {
            Type readerType = typeof(ValueTupleTypeReader<,,>).MakeGenericType(new[] { type1, type2, type3 });
            return (ITypeReader)Activator.CreateInstance(readerType)!;
        }

        private static ITypeReader CreateTupleTypeReader(Type type1, Type type2, Type type3)
        {
            Type readerType = typeof(TupleTypeReader<,,>).MakeGenericType(new[] { type1, type2, type3 });
            return (ITypeReader)Activator.CreateInstance(readerType)!;
        }

        sealed class ValueTupleTypeReader<T1, T2, T3, T4> : ITypeReader<ValueTuple<T1, T2, T3, T4>>, ITypeReader<object>
            where T1 : notnull
            where T2 : notnull
            where T3 : notnull
            where T4 : notnull
        {
            object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

            public ValueTuple<T1, T2, T3, T4> Read(ref Reader reader)
            {
                return reader.ReadStruct<T1, T2, T3, T4>();
            }
        }

        sealed class TupleTypeReader<T1, T2, T3, T4> : ITypeReader<Tuple<T1, T2, T3, T4>>, ITypeReader<object>
            where T1 : notnull
            where T2 : notnull
            where T3 : notnull
            where T4 : notnull
        {
            object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

            public Tuple<T1, T2, T3, T4> Read(ref Reader reader)
            {
                return reader.ReadStructAsTuple<T1, T2, T3, T4>();
            }
        }

        private static ITypeReader CreateValueTupleTypeReader(Type type1, Type type2, Type type3, Type type4)
        {
            Type readerType = typeof(ValueTupleTypeReader<,,,>).MakeGenericType(new[] { type1, type2, type3, type4 });
            return (ITypeReader)Activator.CreateInstance(readerType)!;
        }

        private static ITypeReader CreateTupleTypeReader(Type type1, Type type2, Type type3, Type type4)
        {
            Type readerType = typeof(TupleTypeReader<,,,>).MakeGenericType(new[] { type1, type2, type3, type4 });
            return (ITypeReader)Activator.CreateInstance(readerType)!;
        }

        sealed class ValueTupleTypeReader<T1, T2, T3, T4, T5> : ITypeReader<ValueTuple<T1, T2, T3, T4, T5>>, ITypeReader<object>
            where T1 : notnull
            where T2 : notnull
            where T3 : notnull
            where T4 : notnull
            where T5 : notnull
        {
            object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

            public ValueTuple<T1, T2, T3, T4, T5> Read(ref Reader reader)
            {
                return reader.ReadStruct<T1, T2, T3, T4, T5>();
            }
        }

        sealed class TupleTypeReader<T1, T2, T3, T4, T5> : ITypeReader<Tuple<T1, T2, T3, T4, T5>>, ITypeReader<object>
            where T1 : notnull
            where T2 : notnull
            where T3 : notnull
            where T4 : notnull
            where T5 : notnull
        {
            object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

            public Tuple<T1, T2, T3, T4, T5> Read(ref Reader reader)
            {
                return reader.ReadStructAsTuple<T1, T2, T3, T4, T5>();
            }
        }

        private static ITypeReader CreateValueTupleTypeReader(Type type1, Type type2, Type type3, Type type4, Type type5)
        {
            Type readerType = typeof(ValueTupleTypeReader<,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5 });
            return (ITypeReader)Activator.CreateInstance(readerType)!;
        }

        private static ITypeReader CreateTupleTypeReader(Type type1, Type type2, Type type3, Type type4, Type type5)
        {
            Type readerType = typeof(TupleTypeReader<,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type5 });
            return (ITypeReader)Activator.CreateInstance(readerType)!;
        }

        sealed class ValueTupleTypeReader<T1, T2, T3, T4, T5, T6> : ITypeReader<ValueTuple<T1, T2, T3, T4, T5, T6>>, ITypeReader<object>
            where T1 : notnull
            where T2 : notnull
            where T3 : notnull
            where T4 : notnull
            where T5 : notnull
            where T6 : notnull
        {
            object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

            public ValueTuple<T1, T2, T3, T4, T5, T6> Read(ref Reader reader)
            {
                return reader.ReadStruct<T1, T2, T3, T4, T5, T6>();
            }
        }

        sealed class TupleTypeReader<T1, T2, T3, T4, T5, T6> : ITypeReader<Tuple<T1, T2, T3, T4, T5, T6>>, ITypeReader<object>
            where T1 : notnull
            where T2 : notnull
            where T3 : notnull
            where T4 : notnull
            where T5 : notnull
            where T6 : notnull
        {
            object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

            public Tuple<T1, T2, T3, T4, T5, T6> Read(ref Reader reader)
            {
                return reader.ReadStructAsTuple<T1, T2, T3, T4, T5, T6>();
            }
        }

        private static ITypeReader CreateValueTupleTypeReader(Type type1, Type type2, Type type3, Type type4, Type type5, Type type6)
        {
            Type readerType = typeof(ValueTupleTypeReader<,,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type6 });
            return (ITypeReader)Activator.CreateInstance(readerType)!;
        }

        private static ITypeReader CreateTupleTypeReader(Type type1, Type type2, Type type3, Type type4, Type type5, Type type6)
        {
            Type readerType = typeof(TupleTypeReader<,,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type6 });
            return (ITypeReader)Activator.CreateInstance(readerType)!;
        }

        sealed class ValueTupleTypeReader<T1, T2, T3, T4, T5, T6, T7> : ITypeReader<ValueTuple<T1, T2, T3, T4, T5, T6, T7>>, ITypeReader<object>
            where T1 : notnull
            where T2 : notnull
            where T3 : notnull
            where T4 : notnull
            where T5 : notnull
            where T6 : notnull
            where T7 : notnull
        {
            object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

            public ValueTuple<T1, T2, T3, T4, T5, T6, T7> Read(ref Reader reader)
            {
                return reader.ReadStruct<T1, T2, T3, T4, T5, T6, T7>();
            }
        }

        sealed class TupleTypeReader<T1, T2, T3, T4, T5, T6, T7> : ITypeReader<Tuple<T1, T2, T3, T4, T5, T6, T7>>, ITypeReader<object>
            where T1 : notnull
            where T2 : notnull
            where T3 : notnull
            where T4 : notnull
            where T5 : notnull
            where T6 : notnull
            where T7 : notnull
        {
            object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

            public Tuple<T1, T2, T3, T4, T5, T6, T7> Read(ref Reader reader)
            {
                return reader.ReadStructAsTuple<T1, T2, T3, T4, T5, T6, T7>();
            }
        }

        private static ITypeReader CreateValueTupleTypeReader(Type type1, Type type2, Type type3, Type type4, Type type5, Type type6, Type type7)
        {
            Type readerType = typeof(ValueTupleTypeReader<,,,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type6, type7 });
            return (ITypeReader)Activator.CreateInstance(readerType)!;
        }

        private static ITypeReader CreateTupleTypeReader(Type type1, Type type2, Type type3, Type type4, Type type5, Type type6, Type type7)
        {
            Type readerType = typeof(TupleTypeReader<,,,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type6, type7 });
            return (ITypeReader)Activator.CreateInstance(readerType)!;
        }

        sealed class ValueTupleTypeReader<T1, T2, T3, T4, T5, T6, T7, T8> : ITypeReader<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8>>>, ITypeReader<object>
            where T1 : notnull
            where T2 : notnull
            where T3 : notnull
            where T4 : notnull
            where T5 : notnull
            where T6 : notnull
            where T7 : notnull
            where T8 : notnull
        {
            object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

            public ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8>> Read(ref Reader reader)
            {
                return reader.ReadStruct<T1, T2, T3, T4, T5, T6, T7, T8>();
            }
        }

        sealed class TupleTypeReader<T1, T2, T3, T4, T5, T6, T7, T8> : ITypeReader<Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8>>>, ITypeReader<object>
            where T1 : notnull
            where T2 : notnull
            where T3 : notnull
            where T4 : notnull
            where T5 : notnull
            where T6 : notnull
            where T7 : notnull
            where T8 : notnull
        {
            object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

            public Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8>> Read(ref Reader reader)
            {
                return reader.ReadStructAsTuple<T1, T2, T3, T4, T5, T6, T7, T8>();
            }
        }

        private static ITypeReader CreateValueTupleTypeReader(Type type1, Type type2, Type type3, Type type4, Type type5, Type type6, Type type7, Type type8)
        {
            Type readerType = typeof(ValueTupleTypeReader<,,,,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type6, type7, type8 });
            return (ITypeReader)Activator.CreateInstance(readerType)!;
        }

        private static ITypeReader CreateTupleTypeReader(Type type1, Type type2, Type type3, Type type4, Type type5, Type type6, Type type7, Type type8)
        {
            Type readerType = typeof(TupleTypeReader<,,,,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type6, type7, type8 });
            return (ITypeReader)Activator.CreateInstance(readerType)!;
        }

        sealed class ValueTupleTypeReader<T1, T2, T3, T4, T5, T6, T7, T8, T9> : ITypeReader<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9>>>, ITypeReader<object>
            where T1 : notnull
            where T2 : notnull
            where T3 : notnull
            where T4 : notnull
            where T5 : notnull
            where T6 : notnull
            where T7 : notnull
            where T8 : notnull
            where T9 : notnull
        {
            object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

            public ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9>> Read(ref Reader reader)
            {
                return reader.ReadStruct<T1, T2, T3, T4, T5, T6, T7, T8, T9>();
            }
        }

        sealed class TupleTypeReader<T1, T2, T3, T4, T5, T6, T7, T8, T9> : ITypeReader<Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9>>>, ITypeReader<object>
            where T1 : notnull
            where T2 : notnull
            where T3 : notnull
            where T4 : notnull
            where T5 : notnull
            where T6 : notnull
            where T7 : notnull
            where T8 : notnull
            where T9 : notnull
        {
            object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

            public Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9>> Read(ref Reader reader)
            {
                return reader.ReadStructAsTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9>();
            }
        }

        private static ITypeReader CreateValueTupleTypeReader(Type type1, Type type2, Type type3, Type type4, Type type5, Type type6, Type type7, Type type8, Type type9)
        {
            Type readerType = typeof(ValueTupleTypeReader<,,,,,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type6, type7, type8, type9 });
            return (ITypeReader)Activator.CreateInstance(readerType)!;
        }

        private static ITypeReader CreateTupleTypeReader(Type type1, Type type2, Type type3, Type type4, Type type5, Type type6, Type type7, Type type8, Type type9)
        {
            Type readerType = typeof(TupleTypeReader<,,,,,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type6, type7, type8, type9 });
            return (ITypeReader)Activator.CreateInstance(readerType)!;
        }

        sealed class ValueTupleTypeReader<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : ITypeReader<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10>>>, ITypeReader<object>
            where T1 : notnull
            where T2 : notnull
            where T3 : notnull
            where T4 : notnull
            where T5 : notnull
            where T6 : notnull
            where T7 : notnull
            where T8 : notnull
            where T9 : notnull
            where T10 : notnull
        {
            object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

            public ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10>> Read(ref Reader reader)
            {
                return reader.ReadStruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>();
            }
        }

        sealed class TupleTypeReader<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : ITypeReader<Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10>>>, ITypeReader<object>
            where T1 : notnull
            where T2 : notnull
            where T3 : notnull
            where T4 : notnull
            where T5 : notnull
            where T6 : notnull
            where T7 : notnull
            where T8 : notnull
            where T9 : notnull
            where T10 : notnull
        {
            object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

            public Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10>> Read(ref Reader reader)
            {
                return reader.ReadStructAsTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>();
            }
        }

        private static ITypeReader CreateValueTupleTypeReader(Type type1, Type type2, Type type3, Type type4, Type type5, Type type6, Type type7, Type type8, Type type9, Type type10)
        {
            Type readerType = typeof(ValueTupleTypeReader<,,,,,,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type6, type7, type8, type9, type10 });
            return (ITypeReader)Activator.CreateInstance(readerType)!;
        }

        private static ITypeReader CreateTupleTypeReader(Type type1, Type type2, Type type3, Type type4, Type type5, Type type6, Type type7, Type type8, Type type9, Type type10)
        {
            Type readerType = typeof(TupleTypeReader<,,,,,,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type6, type7, type8, type9, type10 });
            return (ITypeReader)Activator.CreateInstance(readerType)!;
        }
    }
}

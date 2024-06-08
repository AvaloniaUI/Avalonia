namespace Tmds.DBus.Protocol;

// Code in this file is not trimmer friendly.
#pragma warning disable IL3050
#pragma warning disable IL2026
// Using obsolete generic write members
#pragma warning disable CS0618

public ref partial struct MessageWriter
{
    interface ITypeWriter
    {
        void WriteVariant(ref MessageWriter writer, object value);
    }

    interface ITypeWriter<in T> : ITypeWriter
    {
        void Write(ref MessageWriter writer, T value);
    }

    private void WriteDynamic<T>(T value) where T : notnull
    {
        if (typeof(T) == typeof(object))
        {
            WriteVariant((object)value);
            return;
        }

        var typeWriter = (ITypeWriter<T>)TypeWriters.GetTypeWriter(typeof(T));
        typeWriter.Write(ref this, value);
    }

    [Obsolete(Strings.AddTypeWriterMethodObsolete)]
    public static void AddArrayTypeWriter<T>()
        where T : notnull
    { }

    [Obsolete(Strings.AddTypeWriterMethodObsolete)]
    public static void AddDictionaryTypeWriter<TKey, TValue>()
        where TKey : notnull
        where TValue : notnull
    { }

    [Obsolete(Strings.AddTypeWriterMethodObsolete)]
    public static void AddValueTupleTypeWriter<T1>()
        where T1 : notnull
    { }

    [Obsolete(Strings.AddTypeWriterMethodObsolete)]
    public static void AddTupleTypeWriter<T1>()
        where T1 : notnull
    { }

    [Obsolete(Strings.AddTypeWriterMethodObsolete)]
    public static void AddValueTupleTypeWriter<T1, T2>()
        where T1 : notnull
        where T2 : notnull
    { }

    [Obsolete(Strings.AddTypeWriterMethodObsolete)]
    public static void AddTupleTypeWriter<T1, T2>()
        where T1 : notnull
        where T2 : notnull
    { }

    [Obsolete(Strings.AddTypeWriterMethodObsolete)]
    public static void AddValueTupleTypeWriter<T1, T2, T3>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
    { }

    [Obsolete(Strings.AddTypeWriterMethodObsolete)]
    public static void AddTupleTypeWriter<T1, T2, T3>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
    { }

    [Obsolete(Strings.AddTypeWriterMethodObsolete)]
    public static void AddValueTupleTypeWriter<T1, T2, T3, T4>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
    { }

    [Obsolete(Strings.AddTypeWriterMethodObsolete)]
    public static void AddTupleTypeWriter<T1, T2, T3, T4>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
    { }

    [Obsolete(Strings.AddTypeWriterMethodObsolete)]
    public static void AddValueTupleTypeWriter<T1, T2, T3, T4, T5>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
    { }

    [Obsolete(Strings.AddTypeWriterMethodObsolete)]
    public static void AddTupleTypeWriter<T1, T2, T3, T4, T5>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
    { }

    [Obsolete(Strings.AddTypeWriterMethodObsolete)]
    public static void AddValueTupleTypeWriter<T1, T2, T3, T4, T5, T6>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
    { }

    [Obsolete(Strings.AddTypeWriterMethodObsolete)]
    public static void AddTupleTypeWriter<T1, T2, T3, T4, T5, T6>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
    { }

    [Obsolete(Strings.AddTypeWriterMethodObsolete)]
    public static void AddValueTupleTypeWriter<T1, T2, T3, T4, T5, T6, T7>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
    { }

    [Obsolete(Strings.AddTypeWriterMethodObsolete)]
    public static void AddTupleTypeWriter<T1, T2, T3, T4, T5, T6, T7>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
    { }

    [Obsolete(Strings.AddTypeWriterMethodObsolete)]
    public static void AddValueTupleTypeWriter<T1, T2, T3, T4, T5, T6, T7, T8>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
        where T8 : notnull
    { }

    [Obsolete(Strings.AddTypeWriterMethodObsolete)]
    public static void AddTupleTypeWriter<T1, T2, T3, T4, T5, T6, T7, T8>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
        where T8 : notnull
    { }

    [Obsolete(Strings.AddTypeWriterMethodObsolete)]
    public static void AddValueTupleTypeWriter<T1, T2, T3, T4, T5, T6, T7, T8, T9>()
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

    [Obsolete(Strings.AddTypeWriterMethodObsolete)]
    public static void AddTupleTypeWriter<T1, T2, T3, T4, T5, T6, T7, T8, T9>()
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

    [Obsolete(Strings.AddTypeWriterMethodObsolete)]
    public static void AddValueTupleTypeWriter<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>()
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

    [Obsolete(Strings.AddTypeWriterMethodObsolete)]
    public static void AddTupleTypeWriter<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>()
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

    static class TypeWriters
    {
        private static readonly Dictionary<Type, ITypeWriter> _typeWriters = new();

        public static ITypeWriter GetTypeWriter(Type type)
        {
            lock (_typeWriters)
            {
                if (_typeWriters.TryGetValue(type, out ITypeWriter? writer))
                {
                    return writer;
                }
                writer = CreateWriterForType(type);
                _typeWriters.Add(type, writer);
                return writer;
            }
        }

        private static ITypeWriter CreateWriterForType(Type type)
        {
            // Struct (ValueTuple)
            if (type.IsGenericType && type.FullName!.StartsWith("System.ValueTuple"))
            {
                switch (type.GenericTypeArguments.Length)
                {
                    case 1: return CreateValueTupleTypeWriter(type.GenericTypeArguments[0]);
                    case 2:
                        return CreateValueTupleTypeWriter(type.GenericTypeArguments[0],
                                                          type.GenericTypeArguments[1]);
                    case 3:
                        return CreateValueTupleTypeWriter(type.GenericTypeArguments[0],
                                                          type.GenericTypeArguments[1],
                                                          type.GenericTypeArguments[2]);
                    case 4:
                        return CreateValueTupleTypeWriter(type.GenericTypeArguments[0],
                                                          type.GenericTypeArguments[1],
                                                          type.GenericTypeArguments[2],
                                                          type.GenericTypeArguments[3]);
                    case 5:
                        return CreateValueTupleTypeWriter(type.GenericTypeArguments[0],
                                                          type.GenericTypeArguments[1],
                                                          type.GenericTypeArguments[2],
                                                          type.GenericTypeArguments[3],
                                                          type.GenericTypeArguments[4]);

                    case 6:
                        return CreateValueTupleTypeWriter(type.GenericTypeArguments[0],
                                                     type.GenericTypeArguments[1],
                                                     type.GenericTypeArguments[2],
                                                     type.GenericTypeArguments[3],
                                                     type.GenericTypeArguments[4],
                                                     type.GenericTypeArguments[5]);
                    case 7:
                        return CreateValueTupleTypeWriter(type.GenericTypeArguments[0],
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
                                return CreateValueTupleTypeWriter(type.GenericTypeArguments[0],
                                                             type.GenericTypeArguments[1],
                                                             type.GenericTypeArguments[2],
                                                             type.GenericTypeArguments[3],
                                                             type.GenericTypeArguments[4],
                                                             type.GenericTypeArguments[5],
                                                             type.GenericTypeArguments[6],
                                                             type.GenericTypeArguments[7].GenericTypeArguments[0]);
                            case 2:
                                return CreateValueTupleTypeWriter(type.GenericTypeArguments[0],
                                                             type.GenericTypeArguments[1],
                                                             type.GenericTypeArguments[2],
                                                             type.GenericTypeArguments[3],
                                                             type.GenericTypeArguments[4],
                                                             type.GenericTypeArguments[5],
                                                             type.GenericTypeArguments[6],
                                                             type.GenericTypeArguments[7].GenericTypeArguments[0],
                                                             type.GenericTypeArguments[7].GenericTypeArguments[1]);
                            case 3:
                                return CreateValueTupleTypeWriter(type.GenericTypeArguments[0],
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
                    case 1: return CreateTupleTypeWriter(type.GenericTypeArguments[0]);
                    case 2:
                        return CreateTupleTypeWriter(type.GenericTypeArguments[0],
                                                     type.GenericTypeArguments[1]);
                    case 3:
                        return CreateTupleTypeWriter(type.GenericTypeArguments[0],
                                                     type.GenericTypeArguments[1],
                                                     type.GenericTypeArguments[2]);
                    case 4:
                        return CreateTupleTypeWriter(type.GenericTypeArguments[0],
                                                     type.GenericTypeArguments[1],
                                                     type.GenericTypeArguments[2],
                                                     type.GenericTypeArguments[3]);
                    case 5:
                        return CreateTupleTypeWriter(type.GenericTypeArguments[0],
                                                     type.GenericTypeArguments[1],
                                                     type.GenericTypeArguments[2],
                                                     type.GenericTypeArguments[3],
                                                     type.GenericTypeArguments[4]);
                    case 6:
                        return CreateTupleTypeWriter(type.GenericTypeArguments[0],
                                                     type.GenericTypeArguments[1],
                                                     type.GenericTypeArguments[2],
                                                     type.GenericTypeArguments[3],
                                                     type.GenericTypeArguments[4],
                                                     type.GenericTypeArguments[5]);
                    case 7:
                        return CreateTupleTypeWriter(type.GenericTypeArguments[0],
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
                                return CreateTupleTypeWriter(type.GenericTypeArguments[0],
                                                             type.GenericTypeArguments[1],
                                                             type.GenericTypeArguments[2],
                                                             type.GenericTypeArguments[3],
                                                             type.GenericTypeArguments[4],
                                                             type.GenericTypeArguments[5],
                                                             type.GenericTypeArguments[6],
                                                             type.GenericTypeArguments[7].GenericTypeArguments[0]);
                            case 2:
                                return CreateTupleTypeWriter(type.GenericTypeArguments[0],
                                                             type.GenericTypeArguments[1],
                                                             type.GenericTypeArguments[2],
                                                             type.GenericTypeArguments[3],
                                                             type.GenericTypeArguments[4],
                                                             type.GenericTypeArguments[5],
                                                             type.GenericTypeArguments[6],
                                                             type.GenericTypeArguments[7].GenericTypeArguments[0],
                                                             type.GenericTypeArguments[7].GenericTypeArguments[1]);
                            case 3:
                                return CreateTupleTypeWriter(type.GenericTypeArguments[0],
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

            // Array/Dictionary type (IEnumerable<>/IEnumerable<KeyValuePair<,>>)
            Type? extractedType = TypeModel.ExtractGenericInterface(type, typeof(IEnumerable<>));
            if (extractedType != null)
            {
                if (_typeWriters.TryGetValue(extractedType, out ITypeWriter? writer))
                {
                    return writer;
                }

                Type elementType = extractedType.GenericTypeArguments[0];
                if (elementType.IsGenericType && elementType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                {
                    Type keyType = elementType.GenericTypeArguments[0];
                    Type valueType = elementType.GenericTypeArguments[1];
                    writer = CreateDictionaryTypeWriter(keyType, valueType);
                }
                else
                {
                    writer = CreateArrayTypeWriter(elementType);
                }

                if (type != extractedType)
                {
                    _typeWriters.Add(extractedType, writer);
                }

                return writer;
            }

            ThrowNotSupportedType(type);
            return default!;
        }

        sealed class ArrayTypeWriter<T> : ITypeWriter<IEnumerable<T>>
            where T : notnull
        {
            public void Write(ref MessageWriter writer, IEnumerable<T> value)
            {
                writer.WriteArray(value);
            }

            public void WriteVariant(ref MessageWriter writer, object value)
            {
                WriteArraySignature<T>(ref writer);
                writer.WriteArray((IEnumerable<T>)value);
            }
        }

        private static ITypeWriter CreateArrayTypeWriter(Type elementType)
        {
            Type writerType = typeof(ArrayTypeWriter<>).MakeGenericType(new[] { elementType });
            return (ITypeWriter)Activator.CreateInstance(writerType)!;
        }

        sealed class DictionaryTypeWriter<TKey, TValue> : ITypeWriter<IEnumerable<KeyValuePair<TKey, TValue>>>
            where TKey : notnull
            where TValue : notnull
        {
            public void Write(ref MessageWriter writer, IEnumerable<KeyValuePair<TKey, TValue>> value)
            {
                writer.WriteDictionary(value);
            }

            public void WriteVariant(ref MessageWriter writer, object value)
            {
                WriteDictionarySignature<TKey, TValue>(ref writer);
                writer.WriteDictionary((IEnumerable<KeyValuePair<TKey, TValue>>)value);
            }
        }

        private static ITypeWriter CreateDictionaryTypeWriter(Type keyType, Type valueType)
        {
            Type writerType = typeof(DictionaryTypeWriter<,>).MakeGenericType(new[] { keyType, valueType });
            return (ITypeWriter)Activator.CreateInstance(writerType)!;
        }

        sealed class ValueTupleTypeWriter<T1> : ITypeWriter<ValueTuple<T1>>
            where T1 : notnull
        {
            public void Write(ref MessageWriter writer, ValueTuple<T1> value)
            {
                writer.WriteStruct<T1>(new ValueTuple<T1>(value.Item1));
            }

            public void WriteVariant(ref MessageWriter writer, object value)
            {
                WriteStructSignature<T1>(ref writer);
                Write(ref writer, (ValueTuple<T1>)value);
            }
        }

        sealed class TupleTypeWriter<T1> : ITypeWriter<Tuple<T1>>
            where T1 : notnull
        {
            public void Write(ref MessageWriter writer, Tuple<T1> value)
            {
                writer.WriteStruct<T1>(new ValueTuple<T1>(value.Item1));
            }

            public void WriteVariant(ref MessageWriter writer, object value)
            {
                WriteStructSignature<T1>(ref writer);
                Write(ref writer, (Tuple<T1>)value);
            }
        }

        private static ITypeWriter CreateValueTupleTypeWriter(Type type1)
        {
            Type writerType = typeof(ValueTupleTypeWriter<>).MakeGenericType(new[] { type1 });
            return (ITypeWriter)Activator.CreateInstance(writerType)!;
        }

        private static ITypeWriter CreateTupleTypeWriter(Type type1)
        {
            Type writerType = typeof(TupleTypeWriter<>).MakeGenericType(new[] { type1 });
            return (ITypeWriter)Activator.CreateInstance(writerType)!;
        }

        sealed class ValueTupleTypeWriter<T1, T2> : ITypeWriter<ValueTuple<T1, T2>>
            where T1 : notnull
            where T2 : notnull
        {
            public void Write(ref MessageWriter writer, ValueTuple<T1, T2> value)
            {
                writer.WriteStruct<T1, T2>(value);
            }

            public void WriteVariant(ref MessageWriter writer, object value)
            {
                WriteStructSignature<T1, T2>(ref writer);
                Write(ref writer, (ValueTuple<T1, T2>)value);
            }
        }

        sealed class TupleTypeWriter<T1, T2> : ITypeWriter<Tuple<T1, T2>>
            where T1 : notnull
            where T2 : notnull
        {
            public void Write(ref MessageWriter writer, Tuple<T1, T2> value)
            {
                writer.WriteStruct<T1, T2>((value.Item1, value.Item2));
            }

            public void WriteVariant(ref MessageWriter writer, object value)
            {
                WriteStructSignature<T1, T2>(ref writer);
                Write(ref writer, (Tuple<T1, T2>)value);
            }
        }

        private static ITypeWriter CreateValueTupleTypeWriter(Type type1, Type type2)
        {
            Type writerType = typeof(ValueTupleTypeWriter<,>).MakeGenericType(new[] { type1, type2 });
            return (ITypeWriter)Activator.CreateInstance(writerType)!;
        }

        private static ITypeWriter CreateTupleTypeWriter(Type type1, Type type2)
        {
            Type writerType = typeof(TupleTypeWriter<,>).MakeGenericType(new[] { type1, type2 });
            return (ITypeWriter)Activator.CreateInstance(writerType)!;
        }

        sealed class ValueTupleTypeWriter<T1, T2, T3> : ITypeWriter<ValueTuple<T1, T2, T3>>
            where T1 : notnull
            where T2 : notnull
            where T3 : notnull
        {
            public void Write(ref MessageWriter writer, ValueTuple<T1, T2, T3> value)
            {
                writer.WriteStruct<T1, T2, T3>(value);
            }

            public void WriteVariant(ref MessageWriter writer, object value)
            {
                WriteStructSignature<T1, T2, T3>(ref writer);
                Write(ref writer, (ValueTuple<T1, T2, T3>)value);
            }
        }

        sealed class TupleTypeWriter<T1, T2, T3> : ITypeWriter<Tuple<T1, T2, T3>>
            where T1 : notnull
            where T2 : notnull
            where T3 : notnull
        {
            public void Write(ref MessageWriter writer, Tuple<T1, T2, T3> value)
            {
                writer.WriteStruct<T1, T2, T3>((value.Item1, value.Item2, value.Item3));
            }

            public void WriteVariant(ref MessageWriter writer, object value)
            {
                WriteStructSignature<T1, T2, T3>(ref writer);
                Write(ref writer, (Tuple<T1, T2, T3>)value);
            }
        }

        private static ITypeWriter CreateValueTupleTypeWriter(Type type1, Type type2, Type type3)
        {
            Type writerType = typeof(ValueTupleTypeWriter<,,>).MakeGenericType(new[] { type1, type2, type3 });
            return (ITypeWriter)Activator.CreateInstance(writerType)!;
        }

        private static ITypeWriter CreateTupleTypeWriter(Type type1, Type type2, Type type3)
        {
            Type writerType = typeof(TupleTypeWriter<,,>).MakeGenericType(new[] { type1, type2, type3 });
            return (ITypeWriter)Activator.CreateInstance(writerType)!;
        }

        sealed class ValueTupleTypeWriter<T1, T2, T3, T4> : ITypeWriter<ValueTuple<T1, T2, T3, T4>>
            where T1 : notnull
            where T2 : notnull
            where T3 : notnull
            where T4 : notnull
        {
            public void Write(ref MessageWriter writer, ValueTuple<T1, T2, T3, T4> value)
            {
                writer.WriteStruct<T1, T2, T3, T4>(value);
            }

            public void WriteVariant(ref MessageWriter writer, object value)
            {
                WriteStructSignature<T1, T2, T3, T4>(ref writer);
                Write(ref writer, (ValueTuple<T1, T2, T3, T4>)value);
            }
        }

        sealed class TupleTypeWriter<T1, T2, T3, T4> : ITypeWriter<Tuple<T1, T2, T3, T4>>
            where T1 : notnull
            where T2 : notnull
            where T3 : notnull
            where T4 : notnull
        {
            public void Write(ref MessageWriter writer, Tuple<T1, T2, T3, T4> value)
            {
                writer.WriteStruct<T1, T2, T3, T4>((value.Item1, value.Item2, value.Item3, value.Item4));
            }

            public void WriteVariant(ref MessageWriter writer, object value)
            {
                WriteStructSignature<T1, T2, T3, T4>(ref writer);
                Write(ref writer, (Tuple<T1, T2, T3, T4>)value);
            }
        }

        private static ITypeWriter CreateValueTupleTypeWriter(Type type1, Type type2, Type type3, Type type4)
        {
            Type writerType = typeof(ValueTupleTypeWriter<,,,>).MakeGenericType(new[] { type1, type2, type3, type4 });
            return (ITypeWriter)Activator.CreateInstance(writerType)!;
        }

        private static ITypeWriter CreateTupleTypeWriter(Type type1, Type type2, Type type3, Type type4)
        {
            Type writerType = typeof(TupleTypeWriter<,,,>).MakeGenericType(new[] { type1, type2, type3, type4 });
            return (ITypeWriter)Activator.CreateInstance(writerType)!;
        }

        sealed class ValueTupleTypeWriter<T1, T2, T3, T4, T5> : ITypeWriter<ValueTuple<T1, T2, T3, T4, T5>>
            where T1 : notnull
            where T2 : notnull
            where T3 : notnull
            where T4 : notnull
            where T5 : notnull
        {
            public void Write(ref MessageWriter writer, ValueTuple<T1, T2, T3, T4, T5> value)
            {
                writer.WriteStruct<T1, T2, T3, T4, T5>(value);
            }

            public void WriteVariant(ref MessageWriter writer, object value)
            {
                WriteStructSignature<T1, T2, T3, T4, T5>(ref writer);
                Write(ref writer, (ValueTuple<T1, T2, T3, T4, T5>)value);
            }
        }

        sealed class TupleTypeWriter<T1, T2, T3, T4, T5> : ITypeWriter<Tuple<T1, T2, T3, T4, T5>>
            where T1 : notnull
            where T2 : notnull
            where T3 : notnull
            where T4 : notnull
            where T5 : notnull
        {
            public void Write(ref MessageWriter writer, Tuple<T1, T2, T3, T4, T5> value)
            {
                writer.WriteStruct<T1, T2, T3, T4, T5>((value.Item1, value.Item2, value.Item3, value.Item4, value.Item5));
            }

            public void WriteVariant(ref MessageWriter writer, object value)
            {
                WriteStructSignature<T1, T2, T3, T4, T5>(ref writer);
                Write(ref writer, (Tuple<T1, T2, T3, T4, T5>)value);
            }
        }

        private static ITypeWriter CreateValueTupleTypeWriter(Type type1, Type type2, Type type3, Type type4, Type type5)
        {
            Type writerType = typeof(ValueTupleTypeWriter<,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5 });
            return (ITypeWriter)Activator.CreateInstance(writerType)!;
        }

        private static ITypeWriter CreateTupleTypeWriter(Type type1, Type type2, Type type3, Type type4, Type type5)
        {
            Type writerType = typeof(TupleTypeWriter<,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5 });
            return (ITypeWriter)Activator.CreateInstance(writerType)!;
        }

        sealed class ValueTupleTypeWriter<T1, T2, T3, T4, T5, T6> : ITypeWriter<ValueTuple<T1, T2, T3, T4, T5, T6>>
            where T1 : notnull
            where T2 : notnull
            where T3 : notnull
            where T4 : notnull
            where T5 : notnull
            where T6 : notnull
        {
            public void Write(ref MessageWriter writer, ValueTuple<T1, T2, T3, T4, T5, T6> value)
            {
                writer.WriteStruct<T1, T2, T3, T4, T5, T6>(value);
            }

            public void WriteVariant(ref MessageWriter writer, object value)
            {
                WriteStructSignature<T1, T2, T3, T4, T5, T6>(ref writer);
                Write(ref writer, (ValueTuple<T1, T2, T3, T4, T5, T6>)value);
            }
        }

        sealed class TupleTypeWriter<T1, T2, T3, T4, T5, T6> : ITypeWriter<Tuple<T1, T2, T3, T4, T5, T6>>
            where T1 : notnull
            where T2 : notnull
            where T3 : notnull
            where T4 : notnull
            where T5 : notnull
            where T6 : notnull
        {
            public void Write(ref MessageWriter writer, Tuple<T1, T2, T3, T4, T5, T6> value)
            {
                writer.WriteStruct<T1, T2, T3, T4, T5, T6>((value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6));
            }

            public void WriteVariant(ref MessageWriter writer, object value)
            {
                WriteStructSignature<T1, T2, T3, T4, T5, T6>(ref writer);
                Write(ref writer, (Tuple<T1, T2, T3, T4, T5, T6>)value);
            }
        }

        private static ITypeWriter CreateValueTupleTypeWriter(Type type1, Type type2, Type type3, Type type4, Type type5, Type type6)
        {
            Type writerType = typeof(ValueTupleTypeWriter<,,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type6 });
            return (ITypeWriter)Activator.CreateInstance(writerType)!;
        }

        private static ITypeWriter CreateTupleTypeWriter(Type type1, Type type2, Type type3, Type type4, Type type5, Type type6)
        {
            Type writerType = typeof(TupleTypeWriter<,,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type6 });
            return (ITypeWriter)Activator.CreateInstance(writerType)!;
        }

        sealed class ValueTupleTypeWriter<T1, T2, T3, T4, T5, T6, T7> : ITypeWriter<ValueTuple<T1, T2, T3, T4, T5, T6, T7>>
            where T1 : notnull
            where T2 : notnull
            where T3 : notnull
            where T4 : notnull
            where T5 : notnull
            where T6 : notnull
            where T7 : notnull
        {
            public void Write(ref MessageWriter writer, ValueTuple<T1, T2, T3, T4, T5, T6, T7> value)
            {
                writer.WriteStruct<T1, T2, T3, T4, T5, T6, T7>(value);
            }

            public void WriteVariant(ref MessageWriter writer, object value)
            {
                WriteStructSignature<T1, T2, T3, T4, T5, T6, T7>(ref writer);
                Write(ref writer, (ValueTuple<T1, T2, T3, T4, T5, T6, T7>)value);
            }
        }

        sealed class TupleTypeWriter<T1, T2, T3, T4, T5, T6, T7> : ITypeWriter<Tuple<T1, T2, T3, T4, T5, T6, T7>>
            where T1 : notnull
            where T2 : notnull
            where T3 : notnull
            where T4 : notnull
            where T5 : notnull
            where T6 : notnull
            where T7 : notnull
        {
            public void Write(ref MessageWriter writer, Tuple<T1, T2, T3, T4, T5, T6, T7> value)
            {
                writer.WriteStruct<T1, T2, T3, T4, T5, T6, T7>((value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7));
            }

            public void WriteVariant(ref MessageWriter writer, object value)
            {
                WriteStructSignature<T1, T2, T3, T4, T5, T6, T7>(ref writer);
                Write(ref writer, (Tuple<T1, T2, T3, T4, T5, T6, T7>)value);
            }
        }

        private static ITypeWriter CreateValueTupleTypeWriter(Type type1, Type type2, Type type3, Type type4, Type type5, Type type6, Type type7)
        {
            Type writerType = typeof(ValueTupleTypeWriter<,,,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type6, type7 });
            return (ITypeWriter)Activator.CreateInstance(writerType)!;
        }

        private static ITypeWriter CreateTupleTypeWriter(Type type1, Type type2, Type type3, Type type4, Type type5, Type type6, Type type7)
        {
            Type writerType = typeof(TupleTypeWriter<,,,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type6, type7 });
            return (ITypeWriter)Activator.CreateInstance(writerType)!;
        }

        sealed class ValueTupleTypeWriter<T1, T2, T3, T4, T5, T6, T7, T8> : ITypeWriter<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8>>>
            where T1 : notnull
            where T2 : notnull
            where T3 : notnull
            where T4 : notnull
            where T5 : notnull
            where T6 : notnull
            where T7 : notnull
            where T8 : notnull
        {
            public void Write(ref MessageWriter writer, (T1, T2, T3, T4, T5, T6, T7, T8) value)
            {
                writer.WriteStruct<T1, T2, T3, T4, T5, T6, T7, T8>(value);
            }

            public void WriteVariant(ref MessageWriter writer, object value)
            {
                WriteStructSignature<T1, T2, T3, T4, T5, T6, T7, T8>(ref writer);
                Write(ref writer, (ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8>>)value);
            }
        }

        sealed class TupleTypeWriter<T1, T2, T3, T4, T5, T6, T7, T8> : ITypeWriter<Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8>>>
            where T1 : notnull
            where T2 : notnull
            where T3 : notnull
            where T4 : notnull
            where T5 : notnull
            where T6 : notnull
            where T7 : notnull
            where T8 : notnull
        {
            public void Write(ref MessageWriter writer, Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8>> value)
            {
                writer.WriteStruct<T1, T2, T3, T4, T5, T6, T7, T8>((value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7, value.Rest.Item1));
            }

            public void WriteVariant(ref MessageWriter writer, object value)
            {
                WriteStructSignature<T1, T2, T3, T4, T5, T6, T7, T8>(ref writer);
                Write(ref writer, (Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8>>)value);
            }
        }

        private static ITypeWriter CreateValueTupleTypeWriter(Type type1, Type type2, Type type3, Type type4, Type type5, Type type6, Type type7, Type type8)
        {
            Type writerType = typeof(ValueTupleTypeWriter<,,,,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type6, type7, type8 });
            return (ITypeWriter)Activator.CreateInstance(writerType)!;
        }

        private static ITypeWriter CreateTupleTypeWriter(Type type1, Type type2, Type type3, Type type4, Type type5, Type type6, Type type7, Type type8)
        {
            Type writerType = typeof(TupleTypeWriter<,,,,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type6, type7, type8 });
            return (ITypeWriter)Activator.CreateInstance(writerType)!;
        }

        sealed class ValueTupleTypeWriter<T1, T2, T3, T4, T5, T6, T7, T8, T9> : ITypeWriter<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9>>>
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
            public void Write(ref MessageWriter writer, (T1, T2, T3, T4, T5, T6, T7, T8, T9) value)
            {
                writer.WriteStruct<T1, T2, T3, T4, T5, T6, T7, T8, T9>(value);
            }

            public void WriteVariant(ref MessageWriter writer, object value)
            {
                WriteStructSignature<T1, T2, T3, T4, T5, T6, T7, T8, T9>(ref writer);
                Write(ref writer, (ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9>>)value);
            }
        }

        sealed class TupleTypeWriter<T1, T2, T3, T4, T5, T6, T7, T8, T9> : ITypeWriter<Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9>>>
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
            public void Write(ref MessageWriter writer, Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9>> value)
            {
                writer.WriteStruct<T1, T2, T3, T4, T5, T6, T7, T8, T9>((value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7, value.Rest.Item1, value.Rest.Item2));
            }

            public void WriteVariant(ref MessageWriter writer, object value)
            {
                WriteStructSignature<T1, T2, T3, T4, T5, T6, T7, T8, T9>(ref writer);
                Write(ref writer, (Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9>>)value);
            }
        }

        private static ITypeWriter CreateValueTupleTypeWriter(Type type1, Type type2, Type type3, Type type4, Type type5, Type type6, Type type7, Type type8, Type type9)
        {
            Type writerType = typeof(ValueTupleTypeWriter<,,,,,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type6, type7, type8 });
            return (ITypeWriter)Activator.CreateInstance(writerType)!;
        }

        private static ITypeWriter CreateTupleTypeWriter(Type type1, Type type2, Type type3, Type type4, Type type5, Type type6, Type type7, Type type8, Type type9)
        {
            Type writerType = typeof(TupleTypeWriter<,,,,,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type6, type7, type8, type9 });
            return (ITypeWriter)Activator.CreateInstance(writerType)!;
        }

        sealed class ValueTupleTypeWriter<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : ITypeWriter<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10>>>
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
            public void Write(ref MessageWriter writer, (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10) value)
            {
                writer.WriteStruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(value);
            }

            public void WriteVariant(ref MessageWriter writer, object value)
            {
                WriteStructSignature<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(ref writer);
                Write(ref writer, (ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10>>)value);
            }
        }

        sealed class TupleTypeWriter<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : ITypeWriter<Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10>>>
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
            public void Write(ref MessageWriter writer, Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10>> value)
            {
                writer.WriteStruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>((value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7, value.Rest.Item1, value.Rest.Item2, value.Rest.Item3));
            }

            public void WriteVariant(ref MessageWriter writer, object value)
            {
                WriteStructSignature<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(ref writer);
                Write(ref writer, (Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10>>)value);
            }
        }

        private static ITypeWriter CreateValueTupleTypeWriter(Type type1, Type type2, Type type3, Type type4, Type type5, Type type6, Type type7, Type type8, Type type9, Type type10)
        {
            Type writerType = typeof(ValueTupleTypeWriter<,,,,,,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type6, type7, type8, type10 });
            return (ITypeWriter)Activator.CreateInstance(writerType)!;
        }

        private static ITypeWriter CreateTupleTypeWriter(Type type1, Type type2, Type type3, Type type4, Type type5, Type type6, Type type7, Type type8, Type type9, Type type10)
        {
            Type writerType = typeof(TupleTypeWriter<,,,,,,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type6, type7, type8, type9, type10 });
            return (ITypeWriter)Activator.CreateInstance(writerType)!;
        }
    }
}

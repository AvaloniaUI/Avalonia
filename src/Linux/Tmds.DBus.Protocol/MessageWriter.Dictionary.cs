namespace Tmds.DBus.Protocol;

// Using obsolete generic write members
#pragma warning disable CS0618

public ref partial struct MessageWriter
{
    public ArrayStart WriteDictionaryStart()
        => WriteArrayStart(DBusType.Struct);

    public void WriteDictionaryEnd(ArrayStart start)
        => WriteArrayEnd(start);

    public void WriteDictionaryEntryStart()
        => WriteStructureStart();

    // Write method for the common 'a{sv}' type.
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026")] // It's safe to call WriteDictionary with these types.
    public void WriteDictionary(IEnumerable<KeyValuePair<string, Variant>> value)
        => WriteDictionary<string, Variant>(value);

    // Write method for the common 'a{sv}' type.
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026")] // It's safe to call WriteDictionary with these types.
    public void WriteDictionary(KeyValuePair<string, Variant>[] value)
        => WriteDictionary<string, Variant>(value);

    // Write method for the common 'a{sv}' type.
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026")] // It's safe to call WriteDictionary with these types.
    public void WriteDictionary(Dictionary<string, Variant> value)
        => WriteDictionary<string, Variant>(value);

    [RequiresUnreferencedCode(Strings.UseNonGenericWriteDictionary)]
    [Obsolete(Strings.UseNonGenericWriteDictionaryObsolete)]
    public void WriteDictionary<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> value)
        where TKey : notnull
        where TValue : notnull
    {
        ArrayStart arrayStart = WriteDictionaryStart();
        foreach (var item in value)
        {
            WriteDictionaryEntryStart();
            Write<TKey>(item.Key);
            Write<TValue>(item.Value);
        }
        WriteDictionaryEnd(arrayStart);
    }

    [RequiresUnreferencedCode(Strings.UseNonGenericWriteDictionary)]
    [Obsolete(Strings.UseNonGenericWriteDictionaryObsolete)]
    public void WriteDictionary<TKey, TValue>(KeyValuePair<TKey, TValue>[] value)
        where TKey : notnull
        where TValue : notnull
    {
        ArrayStart arrayStart = WriteDictionaryStart();
        foreach (var item in value)
        {
            WriteDictionaryEntryStart();
            Write<TKey>(item.Key);
            Write<TValue>(item.Value);
        }
        WriteDictionaryEnd(arrayStart);
    }

    [RequiresUnreferencedCode(Strings.UseNonGenericWriteDictionary)]
    [Obsolete(Strings.UseNonGenericWriteDictionaryObsolete)]
    public void WriteDictionary<TKey, TValue>(Dictionary<TKey, TValue> value)
        where TKey : notnull
        where TValue : notnull
    {
        ArrayStart arrayStart = WriteDictionaryStart();
        foreach (var item in value)
        {
            WriteDictionaryEntryStart();
            Write<TKey>(item.Key);
            Write<TValue>(item.Value);
        }
        WriteDictionaryEnd(arrayStart);
    }

    [RequiresUnreferencedCode(Strings.UseNonGenericWriteVariantDictionary)]
    [Obsolete(Strings.UseNonGenericWriteVariantDictionaryObsolete)]
    public void WriteVariantDictionary<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> value)
        where TKey : notnull
        where TValue : notnull
    {
        WriteDictionarySignature<TKey, TValue>(ref this);
        WriteDictionary(value);
    }

    [RequiresUnreferencedCode(Strings.UseNonGenericWriteVariantDictionary)]
    [Obsolete(Strings.UseNonGenericWriteVariantDictionaryObsolete)]
    public void WriteVariantDictionary<TKey, TValue>(KeyValuePair<TKey, TValue>[] value)
        where TKey : notnull
        where TValue : notnull
    {
        WriteDictionarySignature<TKey, TValue>(ref this);
        WriteDictionary(value);
    }

    [RequiresUnreferencedCode(Strings.UseNonGenericWriteVariantDictionary)]
    [Obsolete(Strings.UseNonGenericWriteVariantDictionaryObsolete)]
    public void WriteVariantDictionary<TKey, TValue>(Dictionary<TKey, TValue> value)
        where TKey : notnull
        where TValue : notnull
    {
        WriteDictionarySignature<TKey, TValue>(ref this);
        WriteDictionary(value);
    }

    // This method writes a Dictionary without using generics at the 'cost' of boxing.
    // private void WriteDictionary(IDictionary value)
    // {
    //     ArrayStart arrayStart = WriteDictionaryStart();
    //     foreach (System.Collections.DictionaryEntry de in dictionary)
    //     {
    //         WriteDictionaryEntryStart();
    //         Write(de.Key, asVariant: keyType == typeof(object));
    //         Write(de.Value, asVariant: valueType == typeof(object));
    //     }
    //     WriteDictionaryEnd(ref arrayStart);
    // }

    private static void WriteDictionarySignature<TKey, TValue>(ref MessageWriter writer) where TKey : notnull where TValue : notnull
    {
        Span<byte> buffer = stackalloc byte[ProtocolConstants.MaxSignatureLength];
        writer.WriteSignature(TypeModel.GetSignature<Dict<TKey, TValue>>(buffer));
    }
}

namespace Tmds.DBus.Protocol;

public ref partial struct MessageWriter
{
    [RequiresUnreferencedCode(Strings.UseNonGenericWriteStruct)]
    [Obsolete(Strings.UseNonGenericWriteStructObsolete)]
    public void WriteStruct<T1>(ValueTuple<T1> value)
        where T1 : notnull
    {
        WriteStructureStart();
        Write<T1>(value.Item1);
    }

    private static void WriteStructSignature<T1>(ref MessageWriter writer)
        where T1 : notnull
    {
        Span<byte> buffer = stackalloc byte[ProtocolConstants.MaxSignatureLength];
        writer.WriteSignature(TypeModel.GetSignature<Struct<T1>>(buffer));
    }

    [RequiresUnreferencedCode(Strings.UseNonGenericWriteStruct)]
    [Obsolete(Strings.UseNonGenericWriteStructObsolete)]
    public void WriteStruct<T1, T2>((T1, T2) value)
        where T1 : notnull
        where T2 : notnull
    {
        WriteStructureStart();
        Write<T1>(value.Item1);
        Write<T2>(value.Item2);
    }

    private static void WriteStructSignature<T1, T2>(ref MessageWriter writer)
        where T1 : notnull
        where T2 : notnull
    {
        Span<byte> buffer = stackalloc byte[ProtocolConstants.MaxSignatureLength];
        writer.WriteSignature(TypeModel.GetSignature<Struct<T1, T2>>(buffer));
    }

    [RequiresUnreferencedCode(Strings.UseNonGenericWriteStruct)]
    [Obsolete(Strings.UseNonGenericWriteStructObsolete)]
    public void WriteStruct<T1, T2, T3>((T1, T2, T3) value)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
    {
        WriteStructureStart();
        Write<T1>(value.Item1);
        Write<T2>(value.Item2);
        Write<T3>(value.Item3);
    }

    private static void WriteStructSignature<T1, T2, T3>(ref MessageWriter writer)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
    {
        Span<byte> buffer = stackalloc byte[ProtocolConstants.MaxSignatureLength];
        writer.WriteSignature(TypeModel.GetSignature<Struct<T1, T2, T3>>(buffer));
    }

    [RequiresUnreferencedCode(Strings.UseNonGenericWriteStruct)]
    [Obsolete(Strings.UseNonGenericWriteStructObsolete)]
    public void WriteStruct<T1, T2, T3, T4>((T1, T2, T3, T4) value)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
    {
        WriteStructureStart();
        Write<T1>(value.Item1);
        Write<T2>(value.Item2);
        Write<T3>(value.Item3);
        Write<T4>(value.Item4);
    }

    private static void WriteStructSignature<T1, T2, T3, T4>(ref MessageWriter writer)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
    {
        Span<byte> buffer = stackalloc byte[ProtocolConstants.MaxSignatureLength];
        writer.WriteSignature(TypeModel.GetSignature<Struct<T1, T2, T3, T4>>(buffer));
    }

    [RequiresUnreferencedCode(Strings.UseNonGenericWriteStruct)]
    [Obsolete(Strings.UseNonGenericWriteStructObsolete)]
    public void WriteStruct<T1, T2, T3, T4, T5>((T1, T2, T3, T4, T5) value)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
    {
        WriteStructureStart();
        Write<T1>(value.Item1);
        Write<T2>(value.Item2);
        Write<T3>(value.Item3);
        Write<T4>(value.Item4);
        Write<T5>(value.Item5);
    }

    private static void WriteStructSignature<T1, T2, T3, T4, T5>(ref MessageWriter writer)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
    {
        Span<byte> buffer = stackalloc byte[ProtocolConstants.MaxSignatureLength];
        writer.WriteSignature(TypeModel.GetSignature<Struct<T1, T2, T3, T4, T5>>(buffer));
    }

    [RequiresUnreferencedCode(Strings.UseNonGenericWriteStruct)]
    [Obsolete(Strings.UseNonGenericWriteStructObsolete)]
    public void WriteStruct<T1, T2, T3, T4, T5, T6>((T1, T2, T3, T4, T5, T6) value)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
    {
        WriteStructureStart();
        Write<T1>(value.Item1);
        Write<T2>(value.Item2);
        Write<T3>(value.Item3);
        Write<T4>(value.Item4);
        Write<T5>(value.Item5);
        Write<T6>(value.Item6);
    }

    private static void WriteStructSignature<T1, T2, T3, T4, T5, T6>(ref MessageWriter writer)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
    {
        Span<byte> buffer = stackalloc byte[ProtocolConstants.MaxSignatureLength];
        writer.WriteSignature(TypeModel.GetSignature<Struct<T1, T2, T3, T4, T5, T6>>(buffer));
    }

    [RequiresUnreferencedCode(Strings.UseNonGenericWriteStruct)]
    [Obsolete(Strings.UseNonGenericWriteStructObsolete)]
    public void WriteStruct<T1, T2, T3, T4, T5, T6, T7>((T1, T2, T3, T4, T5, T6, T7) value)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
    {
        WriteStructureStart();
        Write<T1>(value.Item1);
        Write<T2>(value.Item2);
        Write<T3>(value.Item3);
        Write<T4>(value.Item4);
        Write<T5>(value.Item5);
        Write<T6>(value.Item6);
        Write<T7>(value.Item7);
    }

    private static void WriteStructSignature<T1, T2, T3, T4, T5, T6, T7>(ref MessageWriter writer)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
    {
        Span<byte> buffer = stackalloc byte[ProtocolConstants.MaxSignatureLength];
        writer.WriteSignature(TypeModel.GetSignature<Struct<T1, T2, T3, T4, T5, T6, T7>>(buffer));
    }

    [RequiresUnreferencedCode(Strings.UseNonGenericWriteStruct)]
    [Obsolete(Strings.UseNonGenericWriteStructObsolete)]
    public void WriteStruct<T1, T2, T3, T4, T5, T6, T7, T8>((T1, T2, T3, T4, T5, T6, T7, T8) value)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
        where T8 : notnull
    {
        WriteStructureStart();
        Write<T1>(value.Item1);
        Write<T2>(value.Item2);
        Write<T3>(value.Item3);
        Write<T4>(value.Item4);
        Write<T5>(value.Item5);
        Write<T6>(value.Item6);
        Write<T7>(value.Item7);
        Write<T8>(value.Rest.Item1);
    }

    private static void WriteStructSignature<T1, T2, T3, T4, T5, T6, T7, T8>(ref MessageWriter writer)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
        where T8 : notnull
    {
        Span<byte> buffer = stackalloc byte[ProtocolConstants.MaxSignatureLength];
        writer.WriteSignature(TypeModel.GetSignature<Struct<T1, T2, T3, T4, T5, T6, T7, T8>>(buffer));
    }

    [RequiresUnreferencedCode(Strings.UseNonGenericWriteStruct)]
    [Obsolete(Strings.UseNonGenericWriteStructObsolete)]
    public void WriteStruct<T1, T2, T3, T4, T5, T6, T7, T8, T9>((T1, T2, T3, T4, T5, T6, T7, T8, T9) value)
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
        WriteStructureStart();
        Write<T1>(value.Item1);
        Write<T2>(value.Item2);
        Write<T3>(value.Item3);
        Write<T4>(value.Item4);
        Write<T5>(value.Item5);
        Write<T6>(value.Item6);
        Write<T7>(value.Item7);
        Write<T8>(value.Rest.Item1);
        Write<T9>(value.Rest.Item2);
    }

    private static void WriteStructSignature<T1, T2, T3, T4, T5, T6, T7, T8, T9>(ref MessageWriter writer)
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
        Span<byte> buffer = stackalloc byte[ProtocolConstants.MaxSignatureLength];
        writer.WriteSignature(TypeModel.GetSignature<Struct<T1, T2, T3, T4, T5, T6, T7, T8, T9>>(buffer));
    }

    [RequiresUnreferencedCode(Strings.UseNonGenericWriteStruct)]
    [Obsolete(Strings.UseNonGenericWriteStructObsolete)]
    public void WriteStruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>((T1, T2, T3, T4, T5, T6, T7, T8, T9, T10) value)
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
        WriteStructureStart();
        Write<T1>(value.Item1);
        Write<T2>(value.Item2);
        Write<T3>(value.Item3);
        Write<T4>(value.Item4);
        Write<T5>(value.Item5);
        Write<T6>(value.Item6);
        Write<T7>(value.Item7);
        Write<T8>(value.Rest.Item1);
        Write<T9>(value.Rest.Item2);
        Write<T10>(value.Rest.Item3);
    }

    private static void WriteStructSignature<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(ref MessageWriter writer)
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
        Span<byte> buffer = stackalloc byte[ProtocolConstants.MaxSignatureLength];
        writer.WriteSignature(TypeModel.GetSignature<Struct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>>(buffer));
    }
}

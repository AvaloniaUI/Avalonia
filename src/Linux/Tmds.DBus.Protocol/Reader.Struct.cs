namespace Tmds.DBus.Protocol;

public ref partial struct Reader
{
    [RequiresUnreferencedCode(Strings.UseNonGenericReadStruct)]
    [Obsolete(Strings.UseNonGenericReadStructObsolete)]
    public ValueTuple<T1> ReadStruct
        <
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T1
        >()
    {
        AlignStruct();
        return ValueTuple.Create(Read<T1>());
    }

    private Tuple<T1> ReadStructAsTuple
        <
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T1
        >()
    {
        AlignStruct();
        return Tuple.Create(Read<T1>());
    }

    [RequiresUnreferencedCode(Strings.UseNonGenericReadStruct)]
    [Obsolete(Strings.UseNonGenericReadStructObsolete)]
    public ValueTuple<T1, T2> ReadStruct
        <
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T1,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T2
        >()
        where T1 : notnull
        where T2 : notnull
    {
        AlignStruct();
        return ValueTuple.Create(Read<T1>(), Read<T2>());
    }

    private Tuple<T1, T2> ReadStructAsTuple
        <
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T1,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T2
        >()
        where T1 : notnull
        where T2 : notnull
    {
        AlignStruct();
        return Tuple.Create(Read<T1>(), Read<T2>());
    }

    [RequiresUnreferencedCode(Strings.UseNonGenericReadStruct)]
    [Obsolete(Strings.UseNonGenericReadStructObsolete)]
    public ValueTuple<T1, T2, T3> ReadStruct
        <
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T1,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T2,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T3
        >()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
    {
        AlignStruct();
        return ValueTuple.Create(Read<T1>(), Read<T2>(), Read<T3>());
    }

    private Tuple<T1, T2, T3> ReadStructAsTuple
        <
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T1,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T2,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T3
        >()
    {
        AlignStruct();
        return Tuple.Create(Read<T1>(), Read<T2>(), Read<T3>());
    }

    [RequiresUnreferencedCode(Strings.UseNonGenericReadStruct)]
    [Obsolete(Strings.UseNonGenericReadStructObsolete)]
    public ValueTuple<T1, T2, T3, T4> ReadStruct
        <
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T1,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T2,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T3,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T4
        >()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
    {
        AlignStruct();
        return ValueTuple.Create(Read<T1>(), Read<T2>(), Read<T3>(), Read<T4>());
    }

    private Tuple<T1, T2, T3, T4> ReadStructAsTuple
        <
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T1,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T2,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T3,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T4
        >()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
    {
        AlignStruct();
        return Tuple.Create(Read<T1>(), Read<T2>(), Read<T3>(), Read<T4>());
    }

    [RequiresUnreferencedCode(Strings.UseNonGenericReadStruct)]
    [Obsolete(Strings.UseNonGenericReadStructObsolete)]
    public ValueTuple<T1, T2, T3, T4, T5> ReadStruct
        <
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T1,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T2,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T3,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T4,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T5
        >()
    {
        AlignStruct();
        return ValueTuple.Create(Read<T1>(), Read<T2>(), Read<T3>(), Read<T4>(), Read<T5>());
    }

    private Tuple<T1, T2, T3, T4, T5> ReadStructAsTuple
        <
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T1,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T2,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T3,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T4,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T5
        >()
    {
        AlignStruct();
        return Tuple.Create(Read<T1>(), Read<T2>(), Read<T3>(), Read<T4>(), Read<T5>());
    }

    [RequiresUnreferencedCode(Strings.UseNonGenericReadStruct)]
    [Obsolete(Strings.UseNonGenericReadStructObsolete)]
    public ValueTuple<T1, T2, T3, T4, T5, T6> ReadStruct
        <
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T1,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T2,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T3,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T4,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T5,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T6
        >()
    {
        AlignStruct();
        return ValueTuple.Create(Read<T1>(), Read<T2>(), Read<T3>(), Read<T4>(), Read<T5>(), Read<T6>());
    }

    private Tuple<T1, T2, T3, T4, T5, T6> ReadStructAsTuple
        <
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T1,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T2,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T3,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T4,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T5,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T6
        >()
    {
        AlignStruct();
        return Tuple.Create(Read<T1>(), Read<T2>(), Read<T3>(), Read<T4>(), Read<T5>(), Read<T6>());
    }

    [RequiresUnreferencedCode(Strings.UseNonGenericReadStruct)]
    [Obsolete(Strings.UseNonGenericReadStructObsolete)]
    public ValueTuple<T1, T2, T3, T4, T5, T6, T7> ReadStruct
        <
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T1,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T2,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T3,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T4,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T5,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T6,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T7
        >()
    {
        AlignStruct();
        return ValueTuple.Create(Read<T1>(), Read<T2>(), Read<T3>(), Read<T4>(), Read<T5>(), Read<T6>(), Read<T7>());
    }

    private Tuple<T1, T2, T3, T4, T5, T6, T7> ReadStructAsTuple
        <
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T1,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T2,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T3,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T4,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T5,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T6,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T7
        >()
    {
        AlignStruct();
        return Tuple.Create(Read<T1>(), Read<T2>(), Read<T3>(), Read<T4>(), Read<T5>(), Read<T6>(), Read<T7>());
    }

    [RequiresUnreferencedCode(Strings.UseNonGenericReadStruct)]
    [Obsolete(Strings.UseNonGenericReadStructObsolete)]
    public ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8>> ReadStruct
        <
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T1,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T2,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T3,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T4,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T5,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T6,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T7,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T8
        >()
    {
        AlignStruct();
        return ValueTuple.Create(Read<T1>(), Read<T2>(), Read<T3>(), Read<T4>(), Read<T5>(), Read<T6>(), Read<T7>(), Read<T8>());
    }

    private Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8>> ReadStructAsTuple
        <
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T1,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T2,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T3,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T4,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T5,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T6,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T7,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T8
        >()
    {
        AlignStruct();
        return Tuple.Create(Read<T1>(), Read<T2>(), Read<T3>(), Read<T4>(), Read<T5>(), Read<T6>(), Read<T7>(), Read<T8>());
    }

    [RequiresUnreferencedCode(Strings.UseNonGenericReadStruct)]
    [Obsolete(Strings.UseNonGenericReadStructObsolete)]
    public ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9>> ReadStruct
        <
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T1,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T2,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T3,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T4,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T5,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T6,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T7,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T8,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T9
        >()
    {
        AlignStruct();
        return (Read<T1>(), Read<T2>(), Read<T3>(), Read<T4>(), Read<T5>(), Read<T6>(), Read<T7>(), Read<T8>(), Read<T9>());
    }

    private Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9>> ReadStructAsTuple
        <
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T1,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T2,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T3,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T4,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T5,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T6,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T7,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T8,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T9
        >()
    {
        AlignStruct();
        return new Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9>>(Read<T1>(), Read<T2>(), Read<T3>(), Read<T4>(), Read<T5>(), Read<T6>(), Read<T7>(), Tuple.Create(Read<T8>(), Read<T9>()));
    }

    [RequiresUnreferencedCode(Strings.UseNonGenericReadStruct)]
    [Obsolete(Strings.UseNonGenericReadStructObsolete)]
    public ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10>> ReadStruct
        <
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T1,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T2,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T3,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T4,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T5,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T6,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T7,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T8,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T9,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T10
        >()
    {
        AlignStruct();
        return (Read<T1>(), Read<T2>(), Read<T3>(), Read<T4>(), Read<T5>(), Read<T6>(), Read<T7>(), Read<T8>(), Read<T9>(), Read<T10>());
    }

    private Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10>> ReadStructAsTuple
        <
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T1,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T2,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T3,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T4,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T5,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T6,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T7,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T8,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T9,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T10
        >()
    {
        AlignStruct();
        return new Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10>>(Read<T1>(), Read<T2>(), Read<T3>(), Read<T4>(), Read<T5>(), Read<T6>(), Read<T7>(), Tuple.Create(Read<T8>(), Read<T9>(), Read<T10>()));
    }
}

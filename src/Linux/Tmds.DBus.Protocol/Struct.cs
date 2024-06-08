namespace Tmds.DBus.Protocol;

// Using obsolete generic write members
#pragma warning disable CS0618

public static class Struct
{
    public static Struct<T1> Create<T1>(T1 item1)
        where T1 : notnull
            => new Struct<T1>(item1);

    public static Struct<T1, T2> Create<T1,T2>(T1 item1, T2 item2)
        where T1 : notnull
        where T2 : notnull
            => new Struct<T1, T2>(item1, item2);

    public static Struct<T1, T2, T3> Create<T1,T2,T3>(T1 item1, T2 item2, T3 item3)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
            => new Struct<T1, T2, T3>(item1, item2, item3);

    public static Struct<T1, T2, T3, T4> Create<T1,T2,T3,T4>(T1 item1, T2 item2, T3 item3, T4 item4)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
            => new Struct<T1, T2, T3, T4>(item1, item2, item3, item4);

    public static Struct<T1, T2, T3, T4, T5> Create<T1,T2,T3,T4,T5>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
            => new Struct<T1, T2, T3, T4, T5>(item1, item2, item3, item4, item5);

    public static Struct<T1, T2, T3, T4, T5, T6> Create<T1,T2,T3,T4,T5,T6>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
            => new Struct<T1, T2, T3, T4, T5, T6>(item1, item2, item3, item4, item5, item6);

    public static Struct<T1, T2, T3, T4, T5, T6, T7> Create<T1,T2,T3,T4,T5,T6,T7>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
            => new Struct<T1, T2, T3, T4, T5, T6, T7>(item1, item2, item3, item4, item5, item6, item7);

    public static Struct<T1, T2, T3, T4, T5, T6, T7, T8> Create<T1,T2,T3,T4,T5,T6,T7,T8>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
        where T8 : notnull
            => new Struct<T1, T2, T3, T4, T5, T6, T7, T8>(item1, item2, item3, item4, item5, item6, item7, item8);

    public static Struct<T1, T2, T3, T4, T5, T6, T7, T8, T9> Create<T1,T2,T3,T4,T5,T6,T7,T8,T9>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
        where T8 : notnull
        where T9 : notnull
            => new Struct<T1, T2, T3, T4, T5, T6, T7, T8, T9>(item1, item2, item3, item4, item5, item6, item7, item8, item9);

    public static Struct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Create<T1,T2,T3,T4,T5,T6,T7,T8,T9,T10>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10)
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
            => new Struct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(item1, item2, item3, item4, item5, item6, item7, item8, item9, item10);
}

public sealed class Struct<T1> : IDBusWritable
    where T1  : notnull
{
    public T1 Item1;

    public Struct(T1 item1)
    {
        TypeModel.EnsureSupportedVariantType<T1>();
        Item1 = item1;
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026")] // this is a supported variant type.
    void IDBusWritable.WriteTo(ref MessageWriter writer)
        => writer.WriteStruct(ToValueTuple());

    private ValueTuple<T1> ToValueTuple()
        => new ValueTuple<T1>(Item1);

    public Variant AsVariant()
        => Variant.FromStruct(this);

    public static implicit operator Variant(Struct<T1> value)
        => value.AsVariant();
}

public sealed class Struct<T1,T2> : IDBusWritable
    where T1  : notnull
    where T2  : notnull
{
    public T1 Item1;
    public T2 Item2;

    public Struct(T1 item1, T2 item2)
    {
        TypeModel.EnsureSupportedVariantType<T1>();
        TypeModel.EnsureSupportedVariantType<T2>();
        (Item1, Item2) = (item1, item2);
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026")] // this is a supported variant type.
    void IDBusWritable.WriteTo(ref MessageWriter writer)
        => writer.WriteStruct(ToValueTuple());

    private (T1, T2) ToValueTuple()
        => (Item1, Item2);

    public Variant AsVariant()
        => Variant.FromStruct(this);

    public static implicit operator Variant(Struct<T1, T2> value)
        => value.AsVariant();
}
public sealed class Struct<T1,T2,T3> : IDBusWritable
    where T1  : notnull
    where T2  : notnull
    where T3  : notnull
{
    public T1 Item1;
    public T2 Item2;
    public T3 Item3;

    public Struct(T1 item1, T2 item2, T3 item3)
    {
        TypeModel.EnsureSupportedVariantType<T1>();
        TypeModel.EnsureSupportedVariantType<T2>();
        TypeModel.EnsureSupportedVariantType<T3>();
        (Item1, Item2, Item3) = (item1, item2, item3);
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026")] // this is a supported variant type.
    void IDBusWritable.WriteTo(ref MessageWriter writer)
        => writer.WriteStruct(ToValueTuple());

    private (T1, T2, T3) ToValueTuple()
        => (Item1, Item2, Item3);

    public Variant AsVariant()
        => Variant.FromStruct(this);

    public static implicit operator Variant(Struct<T1, T2, T3> value)
        => value.AsVariant();
}
public sealed class Struct<T1,T2,T3,T4> : IDBusWritable
    where T1  : notnull
    where T2  : notnull
    where T3  : notnull
    where T4  : notnull
{
    public T1 Item1;
    public T2 Item2;
    public T3 Item3;
    public T4 Item4;

    public Struct(T1 item1, T2 item2, T3 item3, T4 item4)
    {
        TypeModel.EnsureSupportedVariantType<T1>();
        TypeModel.EnsureSupportedVariantType<T2>();
        TypeModel.EnsureSupportedVariantType<T3>();
        TypeModel.EnsureSupportedVariantType<T4>();
        (Item1, Item2, Item3, Item4) = (item1, item2, item3, item4);
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026")] // this is a supported variant type.
    void IDBusWritable.WriteTo(ref MessageWriter writer)
        => writer.WriteStruct(ToValueTuple());

    private (T1, T2, T3, T4) ToValueTuple()
        => (Item1, Item2, Item3, Item4);

    public Variant AsVariant()
        => Variant.FromStruct(this);

    public static implicit operator Variant(Struct<T1, T2, T3, T4> value)
        => value.AsVariant();
}
public sealed class Struct<T1,T2,T3,T4,T5> : IDBusWritable
    where T1  : notnull
    where T2  : notnull
    where T3  : notnull
    where T4  : notnull
    where T5  : notnull
{
    public T1 Item1;
    public T2 Item2;
    public T3 Item3;
    public T4 Item4;
    public T5 Item5;

    public Struct(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5)
    {
        TypeModel.EnsureSupportedVariantType<T1>();
        TypeModel.EnsureSupportedVariantType<T2>();
        TypeModel.EnsureSupportedVariantType<T3>();
        TypeModel.EnsureSupportedVariantType<T4>();
        TypeModel.EnsureSupportedVariantType<T5>();
        (Item1, Item2, Item3, Item4, Item5) = (item1, item2, item3, item4, item5);
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026")] // this is a supported variant type.
    void IDBusWritable.WriteTo(ref MessageWriter writer)
        => writer.WriteStruct(ToValueTuple());

    private (T1, T2, T3, T4, T5) ToValueTuple()
        => (Item1, Item2, Item3, Item4, Item5);

    public Variant AsVariant()
        => Variant.FromStruct(this);

    public static implicit operator Variant(Struct<T1, T2, T3, T4, T5> value)
        => value.AsVariant();
}
public sealed class Struct<T1,T2,T3,T4,T5,T6> : IDBusWritable
    where T1  : notnull
    where T2  : notnull
    where T3  : notnull
    where T4  : notnull
    where T5  : notnull
    where T6  : notnull
{
    public T1 Item1;
    public T2 Item2;
    public T3 Item3;
    public T4 Item4;
    public T5 Item5;
    public T6 Item6;

    public Struct(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6)
    {
        TypeModel.EnsureSupportedVariantType<T1>();
        TypeModel.EnsureSupportedVariantType<T2>();
        TypeModel.EnsureSupportedVariantType<T3>();
        TypeModel.EnsureSupportedVariantType<T4>();
        TypeModel.EnsureSupportedVariantType<T5>();
        TypeModel.EnsureSupportedVariantType<T6>();
        (Item1, Item2, Item3, Item4, Item5, Item6) = (item1, item2, item3, item4, item5, item6);
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026")] // this is a supported variant type.
    void IDBusWritable.WriteTo(ref MessageWriter writer)
        => writer.WriteStruct(ToValueTuple());

    private (T1, T2, T3, T4, T5, T6) ToValueTuple()
        => (Item1, Item2, Item3, Item4, Item5, Item6);

    public Variant AsVariant()
        => Variant.FromStruct(this);

    public static implicit operator Variant(Struct<T1, T2, T3, T4, T5, T6> value)
        => value.AsVariant();
}
public sealed class Struct<T1,T2,T3,T4,T5,T6,T7> : IDBusWritable
    where T1  : notnull
    where T2  : notnull
    where T3  : notnull
    where T4  : notnull
    where T5  : notnull
    where T6  : notnull
    where T7  : notnull
{
    public T1 Item1;
    public T2 Item2;
    public T3 Item3;
    public T4 Item4;
    public T5 Item5;
    public T6 Item6;
    public T7 Item7;

    public Struct(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7)
    {
        TypeModel.EnsureSupportedVariantType<T1>();
        TypeModel.EnsureSupportedVariantType<T2>();
        TypeModel.EnsureSupportedVariantType<T3>();
        TypeModel.EnsureSupportedVariantType<T4>();
        TypeModel.EnsureSupportedVariantType<T5>();
        TypeModel.EnsureSupportedVariantType<T6>();
        TypeModel.EnsureSupportedVariantType<T7>();
        (Item1, Item2, Item3, Item4, Item5, Item6, Item7) = (item1, item2, item3, item4, item5, item6, item7);
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026")] // this is a supported variant type.
    void IDBusWritable.WriteTo(ref MessageWriter writer)
        => writer.WriteStruct(ToValueTuple());

    private (T1, T2, T3, T4, T5, T6, T7) ToValueTuple()
        => (Item1, Item2, Item3, Item4, Item5, Item6, Item7);

    public Variant AsVariant()
        => Variant.FromStruct(this);

    public static implicit operator Variant(Struct<T1, T2, T3, T4, T5, T6, T7> value)
        => value.AsVariant();
}
public sealed class Struct<T1,T2,T3,T4,T5,T6,T7,T8> : IDBusWritable
    where T1  : notnull
    where T2  : notnull
    where T3  : notnull
    where T4  : notnull
    where T5  : notnull
    where T6  : notnull
    where T7  : notnull
    where T8  : notnull
{
    public T1 Item1;
    public T2 Item2;
    public T3 Item3;
    public T4 Item4;
    public T5 Item5;
    public T6 Item6;
    public T7 Item7;
    public T8 Item8;

    public Struct(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8)
    {
        TypeModel.EnsureSupportedVariantType<T1>();
        TypeModel.EnsureSupportedVariantType<T2>();
        TypeModel.EnsureSupportedVariantType<T3>();
        TypeModel.EnsureSupportedVariantType<T4>();
        TypeModel.EnsureSupportedVariantType<T5>();
        TypeModel.EnsureSupportedVariantType<T6>();
        TypeModel.EnsureSupportedVariantType<T7>();
        TypeModel.EnsureSupportedVariantType<T8>();
        (Item1, Item2, Item3, Item4, Item5, Item6, Item7, Item8) = (item1, item2, item3, item4, item5, item6, item7, item8);
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026")] // this is a supported variant type.
    void IDBusWritable.WriteTo(ref MessageWriter writer)
        => writer.WriteStruct(ToValueTuple());

    private (T1, T2, T3, T4, T5, T6, T7, T8) ToValueTuple()
        => (Item1, Item2, Item3, Item4, Item5, Item6, Item7, Item8);

    public Variant AsVariant()
        => Variant.FromStruct(this);

    public static implicit operator Variant(Struct<T1, T2, T3, T4, T5, T6, T7, T8> value)
        => value.AsVariant();
}
public sealed class Struct<T1,T2,T3,T4,T5,T6,T7,T8,T9> : IDBusWritable
    where T1  : notnull
    where T2  : notnull
    where T3  : notnull
    where T4  : notnull
    where T5  : notnull
    where T6  : notnull
    where T7  : notnull
    where T8  : notnull
    where T9  : notnull
{
    public T1 Item1;
    public T2 Item2;
    public T3 Item3;
    public T4 Item4;
    public T5 Item5;
    public T6 Item6;
    public T7 Item7;
    public T8 Item8;
    public T9 Item9;

    public Struct(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9)
    {
        TypeModel.EnsureSupportedVariantType<T1>();
        TypeModel.EnsureSupportedVariantType<T2>();
        TypeModel.EnsureSupportedVariantType<T3>();
        TypeModel.EnsureSupportedVariantType<T4>();
        TypeModel.EnsureSupportedVariantType<T5>();
        TypeModel.EnsureSupportedVariantType<T6>();
        TypeModel.EnsureSupportedVariantType<T7>();
        TypeModel.EnsureSupportedVariantType<T8>();
        TypeModel.EnsureSupportedVariantType<T9>();
        (Item1, Item2, Item3, Item4, Item5, Item6, Item7, Item8, Item9) = (item1, item2, item3, item4, item5, item6, item7, item8, item9);
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026")] // this is a supported variant type.
    void IDBusWritable.WriteTo(ref MessageWriter writer)
        => writer.WriteStruct(ToValueTuple());

    private (T1, T2, T3, T4, T5, T6, T7, T8, T9) ToValueTuple()
        => (Item1, Item2, Item3, Item4, Item5, Item6, Item7, Item8, Item9);

    public Variant AsVariant()
        => Variant.FromStruct(this);

    public static implicit operator Variant(Struct<T1, T2, T3, T4, T5, T6, T7, T8, T9> value)
        => value.AsVariant();
}
public sealed class Struct<T1,T2,T3,T4,T5,T6,T7,T8,T9,T10> : IDBusWritable
    where T1  : notnull
    where T2  : notnull
    where T3  : notnull
    where T4  : notnull
    where T5  : notnull
    where T6  : notnull
    where T7  : notnull
    where T8  : notnull
    where T9  : notnull
    where T10  : notnull
{
    public T1 Item1;
    public T2 Item2;
    public T3 Item3;
    public T4 Item4;
    public T5 Item5;
    public T6 Item6;
    public T7 Item7;
    public T8 Item8;
    public T9 Item9;
    public T10 Item10;

    public Struct(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10)
    {
        TypeModel.EnsureSupportedVariantType<T1>();
        TypeModel.EnsureSupportedVariantType<T2>();
        TypeModel.EnsureSupportedVariantType<T3>();
        TypeModel.EnsureSupportedVariantType<T4>();
        TypeModel.EnsureSupportedVariantType<T5>();
        TypeModel.EnsureSupportedVariantType<T6>();
        TypeModel.EnsureSupportedVariantType<T7>();
        TypeModel.EnsureSupportedVariantType<T8>();
        TypeModel.EnsureSupportedVariantType<T9>();
        TypeModel.EnsureSupportedVariantType<T10>();
        (Item1, Item2, Item3, Item4, Item5, Item6, Item7, Item8, Item9, Item10) = (item1, item2, item3, item4, item5, item6, item7, item8, item9, item10);
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026")] // this is a supported variant type.
    void IDBusWritable.WriteTo(ref MessageWriter writer)
        => writer.WriteStruct(ToValueTuple());

    private (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10) ToValueTuple()
        => (Item1, Item2, Item3, Item4, Item5, Item6, Item7, Item8, Item9, Item10);

    public Variant AsVariant()
        => Variant.FromStruct(this);

    public static implicit operator Variant(Struct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> value)
        => value.AsVariant();
}

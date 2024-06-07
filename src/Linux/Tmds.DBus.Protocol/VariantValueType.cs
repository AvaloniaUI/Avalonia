namespace Tmds.DBus.Protocol;

public enum VariantValueType
{
    Invalid = 0,

    // VariantValue is used for a variant for which we read the value
    // and no longer track its signature.
    VariantValue = 1,

    //  Match the DBusType values for easy conversion.
    Byte = DBusType.Byte,
    Bool = DBusType.Bool,
    Int16 = DBusType.Int16,
    UInt16 = DBusType.UInt16,
    Int32 = DBusType.Int32,
    UInt32 = DBusType.UInt32,
    Int64 = DBusType.Int64,
    UInt64 = DBusType.UInt64,
    Double = DBusType.Double,
    String = DBusType.String,
    ObjectPath = DBusType.ObjectPath,
    Signature = DBusType.Signature,
    Array = DBusType.Array,
    Struct = DBusType.Struct,
    Dictionary = DBusType.DictEntry,
    UnixFd = DBusType.UnixFd,
    // We don't need this : variants are resolved into the VariantValue.
    // Variant = DBusType.Variant,
}
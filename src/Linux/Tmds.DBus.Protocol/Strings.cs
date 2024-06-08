namespace Tmds.DBus.Protocol;

static class Strings
{
    public const string AddTypeReaderMethodObsolete = "AddTypeReader methods are obsolete. Remove the call to this method.";
    public const string AddTypeWriterMethodObsolete = "AddTypeWriter methods are obsolete. Remove the call to this method.";

    public const string UseNonGenericWriteArray = $"Use a non-generic overload of '{nameof(MessageWriter.WriteArray)}' if it exists for the item type, and otherwise write out the elements separately surrounded by a call to '{nameof(MessageWriter.WriteArrayStart)}' and '{nameof(MessageWriter.WriteArrayEnd)}'.";
    public const string UseNonGenericReadArray = $"Use a '{nameof(Reader.ReadArray)}Of*' method if it exists for the item type, and otherwise read out the elements in a while loop using '{nameof(Reader.ReadArrayStart)}' and '{nameof(Reader.HasNext)}'.";
    public const string UseNonGenericReadDictionary = $"Read the dictionary by calling '{nameof(Reader.ReadDictionaryStart)} and reading the key-value pairs in a while loop using '{nameof(Reader.HasNext)}'.";
    public const string UseNonGenericWriteDictionary = $"Write the dictionary by calling '{nameof(MessageWriter.WriteDictionaryStart)}', for each element call '{nameof(MessageWriter.WriteDictionaryEntryStart)}', write the key and value. Complete the dictionary writing by calling '{nameof(MessageWriter.WriteDictionaryEnd)}'.";
    public const string UseNonGenericWriteVariantDictionary = $"Write the signature using '{nameof(MessageWriter.WriteSignature)}', then write the dictionary by calling '{nameof(MessageWriter.WriteDictionaryStart)}', for each element call '{nameof(MessageWriter.WriteDictionaryEntryStart)}', write the key and value. Complete the dictionary writing by calling '{nameof(MessageWriter.WriteDictionaryEnd)}'.";
    public const string UseNonGenericReadStruct = $"Read the struct by calling '{nameof(Reader.AlignStruct)}' and then reading all the struct fields.";
    public const string UseNonGenericWriteStruct = $"Write the struct by calling '{nameof(MessageWriter.WriteStructureStart)}' and then writing all the struct fields.";
    public const string UseNonObjectWriteVariant = $"Use the overload of '{nameof(MessageWriter.WriteVariant)}' that accepts a '{nameof(Variant)}' instead.";
    public const string UseNonObjectReadVariantValue = $"Use '{nameof(Reader.ReadVariantValue)}' instead.";

    private const string MethodIsNotCompatibleWithTrimmingNativeAot = "Method is not compatible with trimming/NativeAOT.";

    public const string UseNonGenericWriteArrayObsolete = $"{MethodIsNotCompatibleWithTrimmingNativeAot} {UseNonGenericWriteArray}";
    public const string UseNonGenericReadArrayObsolete = $"{MethodIsNotCompatibleWithTrimmingNativeAot} {UseNonGenericReadArray}";
    public const string UseNonGenericReadDictionaryObsolete = $"{MethodIsNotCompatibleWithTrimmingNativeAot} {UseNonGenericReadDictionary}";
    public const string UseNonGenericWriteDictionaryObsolete = $"{MethodIsNotCompatibleWithTrimmingNativeAot} {UseNonGenericWriteDictionary}";
    public const string UseNonGenericWriteVariantDictionaryObsolete = $"{MethodIsNotCompatibleWithTrimmingNativeAot} {UseNonGenericWriteVariantDictionary}";
    public const string UseNonGenericReadStructObsolete = $"{MethodIsNotCompatibleWithTrimmingNativeAot} {UseNonGenericReadStruct}";
    public const string UseNonGenericWriteStructObsolete = $"{MethodIsNotCompatibleWithTrimmingNativeAot} {UseNonGenericWriteStruct}";
    public const string UseNonObjectWriteVariantObsolete = $"{MethodIsNotCompatibleWithTrimmingNativeAot} {UseNonObjectWriteVariant}";
    public const string UseNonObjectReadVariantValueObsolete = $"{MethodIsNotCompatibleWithTrimmingNativeAot} {UseNonObjectReadVariantValue}";
}
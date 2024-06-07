namespace Tmds.DBus.Protocol;

// Code in this file is not trimmer friendly.
#pragma warning disable IL3050
#pragma warning disable IL2070

static partial class TypeModel
{
    private static DBusType GetTypeAlignmentDynamic<T>()
    {
        if (typeof(T).IsArray)
        {
            return DBusType.Array;
        }
        else if (ExtractGenericInterface(typeof(T), typeof(System.Collections.Generic.IEnumerable<>)) != null)
        {
            return DBusType.Array;
        }
        else
        {
            return DBusType.Struct;
        }
    }

    private static int AppendTypeSignatureDynamic(Type type, Span<byte> signature)
    {
        Type? extractedType;
        if (type == typeof(object))
        {
            signature[0] = (byte)DBusType.Variant;
            return 1;
        }
        else if (type.IsArray)
        {
            int bytesWritten = 0;
            signature[bytesWritten++] = (byte)DBusType.Array;
            bytesWritten += AppendTypeSignature(type.GetElementType()!, signature.Slice(bytesWritten));
            return bytesWritten;
        }
        else if (type.FullName!.StartsWith("System.ValueTuple"))
        {
            int bytesWritten = 0;
            signature[bytesWritten++] = (byte)'(';
            Type[] typeArguments = type.GenericTypeArguments;
            do
            {
                for (int i = 0; i < typeArguments.Length; i++)
                {
                    if (i == 7)
                    {
                        break;
                    }
                    bytesWritten += AppendTypeSignature(typeArguments[i], signature.Slice(bytesWritten));
                }
                if (typeArguments.Length == 8)
                {
                    typeArguments = typeArguments[7].GenericTypeArguments;
                }
                else
                {
                    break;
                }
            } while (true);
            signature[bytesWritten++] = (byte)')';
            return bytesWritten;
        }
        else if ((extractedType = TypeModel.ExtractGenericInterface(type, typeof(IDictionary<,>))) != null)
        {
            int bytesWritten = 0;
            signature[bytesWritten++] = (byte)'a';
            signature[bytesWritten++] = (byte)'{';
            bytesWritten += AppendTypeSignature(extractedType.GenericTypeArguments[0], signature.Slice(bytesWritten));
            bytesWritten += AppendTypeSignature(extractedType.GenericTypeArguments[1], signature.Slice(bytesWritten));
            signature[bytesWritten++] = (byte)'}';
            return bytesWritten;
        }

        ThrowNotSupportedType(type);
        return 0;
    }

    public static Type? ExtractGenericInterface(Type queryType, Type interfaceType)
    {
        if (IsGenericInstantiation(queryType, interfaceType))
        {
            return queryType;
        }

        return GetGenericInstantiation(queryType, interfaceType);
    }

    private static bool IsGenericInstantiation(Type candidate, Type interfaceType)
    {
        return
            candidate.IsGenericType &&
            candidate.GetGenericTypeDefinition() == interfaceType;
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070")]
    private static Type? GetGenericInstantiation(Type queryType, Type interfaceType)
    {
        Type? bestMatch = null;
        var interfaces = queryType.GetInterfaces();
        foreach (var @interface in interfaces)
        {
            if (IsGenericInstantiation(@interface, interfaceType))
            {
                if (bestMatch == null)
                {
                    bestMatch = @interface;
                }
                else if (StringComparer.Ordinal.Compare(@interface.FullName, bestMatch.FullName) < 0)
                {
                    bestMatch = @interface;
                }
            }
        }

        if (bestMatch != null)
        {
            return bestMatch;
        }

        var baseType = queryType?.BaseType;
        if (baseType == null)
        {
            return null;
        }
        else
        {
            return GetGenericInstantiation(baseType, interfaceType);
        }
    }
}
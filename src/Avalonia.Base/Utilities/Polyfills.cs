using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

internal static class Polyfills
{
    #if !NET6_0_OR_GREATER

    public static bool TryDequeue<T>(this Queue<T> queue, [MaybeNullWhen(false)]out T item)
    {
        if (queue.Count == 0)
        {
            item = default;
            return false;
        }

        item = queue.Dequeue();
        return true;
    }
    
    #endif
}

#if !NET7_0_OR_GREATER

namespace System.Diagnostics.CodeAnalysis
{
    [System.AttributeUsage(
        System.AttributeTargets.Method | System.AttributeTargets.Parameter | System.AttributeTargets.Property,
        AllowMultiple = false, Inherited = false)]
    internal sealed class UnscopedRefAttribute : Attribute
    {
    }
    
    struct S
    {
        int field; 

        // Okay: `field` has the ref-safe-to-escape of `this` which is *calling method* because 
        // it is a `ref`
        [UnscopedRef] ref int Prop1 => ref field;
    }
}
#endif
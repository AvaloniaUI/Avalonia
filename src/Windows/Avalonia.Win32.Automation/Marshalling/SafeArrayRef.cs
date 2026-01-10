using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Controls.Documents;
// ReSharper disable InconsistentNaming

namespace Avalonia.Win32.Automation.Marshalling;

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
#if NET7_0_OR_GREATER
internal unsafe partial struct SafeArrayRef
{
    private SAFEARRAY* _ptr;

    internal struct SAFEARRAY
    {
        /// <summary>The number of dimensions.</summary>
        internal ushort cDims;

        /// <summary>
        /// <para>Flags. </para>
        /// <para>This doc was truncated.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/oaidl/ns-oaidl-safearray#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        internal ADVANCED_FEATURE_FLAGS fFeatures;

        /// <summary>The size of an array element.</summary>
        internal uint cbElements;

        /// <summary>The number of times the array has been locked without a corresponding unlock.</summary>
        internal uint cLocks;

        /// <summary>The data.</summary>
        internal void* pvData;

        /// <summary>One bound for each dimension.</summary>
        internal VariableLengthInlineArray<SAFEARRAYBOUND> rgsabound;
    }
    internal struct SAFEARRAYBOUND
    {
        /// <summary>The number of elements in the dimension.</summary>
        internal uint cElements;

        /// <summary>The lower bound of the dimension.</summary>
        internal int lLbound;
    }

    internal struct VariableLengthInlineArray<T>
        where T : unmanaged
    {
        internal T e0;

        internal ref T this[int index]
        {
            [UnscopedRef]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref Unsafe.Add(ref this.e0, index);
        }

        [UnscopedRef]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Span<T> AsSpan(int length)
        {
            return MemoryMarshal.CreateSpan(ref this.e0, length);
        }
    }

    [Flags]
    internal enum ADVANCED_FEATURE_FLAGS : ushort
    {
        FADF_AUTO = 0x0001,
        FADF_STATIC = 0x0002,
        FADF_EMBEDDED = 0x0004,
        FADF_FIXEDSIZE = 0x0010,
        FADF_RECORD = 0x0020,
        FADF_HAVEIID = 0x0040,
        FADF_HAVEVARTYPE = 0x0080,
        FADF_BSTR = 0x0100,
        FADF_UNKNOWN = 0x0200,
        FADF_DISPATCH = 0x0400,
        FADF_VARIANT = 0x0800,
        FADF_RESERVED = 0xF008,
    }

    public void Destroy()
    {
        if (_ptr != default)
        {
            SafeArrayDestroy(_ptr);
        }
    }

    public static T[]? ToArray<T>(SafeArrayRef? safearray)
    {
        if (safearray is null) return null;

        return AccessData(safearray.Value, static (data, length) =>
        {
            var array = new T[length];
            if (typeof(T) == typeof(sbyte))
                Marshal.Copy(data, (byte[])(object)array, 0, length);
            else if (typeof(T) == typeof(short))
                Marshal.Copy(data, (short[])(object)array, 0, length);
            else if (typeof(T) == typeof(int))
                Marshal.Copy(data, (int[])(object)array, 0, length);
            else if (typeof(T) == typeof(long))
                Marshal.Copy(data, (long[])(object)array, 0, length);
            else if (typeof(T) == typeof(byte))
                Marshal.Copy(data, (byte[])(object)array, 0, length);
            else if (typeof(T) == typeof(ushort))
                Marshal.Copy(data, (short[])(object)array, 0, length);
            else if (typeof(T) == typeof(uint))
                Marshal.Copy(data, (int[])(object)array, 0, length);
            else if (typeof(T) == typeof(ulong))
                Marshal.Copy(data, (long[])(object)array, 0, length);
            else if (typeof(T) == typeof(float))
                Marshal.Copy(data, (float[])(object)array, 0, length);
            else if (typeof(T) == typeof(double))
                Marshal.Copy(data, (double[])(object)array, 0, length);
            else if (typeof(T) == typeof(nint))
                Marshal.Copy(data, (nint[])(object)array, 0, length);
            else if (typeof(T) == typeof(nuint))
                Marshal.Copy(data, (nint[])(object)array, 0, length);
            else if (typeof(T) == typeof(string))
            {
                var pointers = new IntPtr[length];
                Marshal.Copy(data, pointers, 0, array.Length);
                for (var i = 0; i < length; i++)
                {
                    array[i] = (T)(object)Marshal.PtrToStringBSTR(pointers[i]);
                }
            }
            else if (typeof(T).IsInterface)
            {
                var pointers = new IntPtr[length];
                Marshal.Copy(data, pointers, 0, array.Length);
                for (int i = 0; i < pointers.Length; i++)
                {
                    if (ComWrappers.TryGetObject(pointers[i], out var instance))
                    {
                        array[i] = (T)instance;
                    }
                    else
                    {
                        throw new NotImplementedException("COM items not owned by managed code can't be unwrapped from SafeArray.");
                    }
                }
            }
            else
            {
                throw new NotImplementedException();
            }

            return array; 
        });
    }

    public static bool TryCreate(IEnumerable? managed, [NotNullWhen(true)] out SafeArrayRef? safearray, out VarEnum varEnum)
    {
        safearray = default;
        varEnum = default;

        if (managed is null)
        {
            return false;
        }

        static SafeArrayRef CreateFromCollection<T>(IReadOnlyCollection<T> collection, VarEnum varEnum)
        {
            var collectionSpan = collection switch
            {
                T[] array => array,
                List<T> list => CollectionsMarshal.AsSpan(list),
                _ => collection.ToArray()
            };
            return CreateFromSpan<T>(collectionSpan, varEnum);
        }

        static SafeArrayRef CreateFromSpan<T>(ReadOnlySpan<T> span, VarEnum varEnum)
        {
            var bound = new SAFEARRAYBOUND { cElements = (uint)span.Length, lLbound = 0 };
            var safearray = SafeArrayCreate(varEnum, 1, bound);
            if (span.Length == 0)
            {
                return new SafeArrayRef
                {
                    _ptr = safearray
                };
            }

            var lockResult = SafeArrayLock(safearray);
            if (lockResult != 0) throw new Win32Exception(lockResult);

            try
            {
                // We assume it has the same length.
                var output = new Span<T>(safearray->pvData, (int)safearray->rgsabound[0].cElements);
                span.CopyTo(output);
            }
            finally
            {
                SafeArrayUnlock(safearray);
            }

            return new SafeArrayRef
            {
                _ptr = safearray
            };
        }

        static SafeArrayRef CreateFromStrings(IReadOnlyList<string> strings, VarEnum varEnum)
        {
            Debug.Assert(varEnum == VarEnum.VT_BSTR); // other types not supported yet
            var pointers = ArrayPool<IntPtr>.Shared.Rent(strings.Count);
            try
            {
                for (int i = 0; i < strings.Count; i++)
                {
                    pointers[i] = Marshal.StringToBSTR(strings[i]);
                }

                return CreateFromSpan<IntPtr>(pointers.AsSpan(0, strings.Count), varEnum);
            }
            finally
            {
                ArrayPool<IntPtr>.Shared.Return(pointers);
            }
        }

        static SafeArrayRef CreateFromBools(IReadOnlyList<bool> bools, VarEnum varEnum)
        {
            Debug.Assert(varEnum == VarEnum.VT_BOOL); // other types not supported
            var shorts = ArrayPool<short>.Shared.Rent(bools.Count);
            try
            {
                for (int i = 0; i < bools.Count; i++)
                {
                    shorts[i] = bools[i] ? ComVariant.VARIANT_TRUE : ComVariant.VARIANT_FALSE;
                }

                return CreateFromSpan<short>(shorts.AsSpan(0, bools.Count), varEnum);
            }
            finally
            {
                ArrayPool<short>.Shared.Return(shorts);
            }
        }

        static SafeArrayRef CreateFromObjects(IReadOnlyList<object> objects, VarEnum varEnum)
        {
            Debug.Assert(varEnum == VarEnum.VT_UNKNOWN); // other types not supported yet
            var pointers = ArrayPool<IntPtr>.Shared.Rent(objects.Count);
            try
            {
                for (int i = 0; i < objects.Count; i++)
                {
                    if (ComWrappers.TryGetComInstance(objects[i], out var pointer))
                    {
                        pointers[i] = pointer;
                    }
                }

                return CreateFromSpan<IntPtr>(pointers, varEnum);
            }
            finally
            {
                ArrayPool<IntPtr>.Shared.Return(pointers);
            }
        }

        safearray = managed switch
        {
            IReadOnlyCollection<sbyte> ints => CreateFromCollection(ints, varEnum = VarEnum.VT_I1),
            IReadOnlyCollection<short> ints => CreateFromCollection(ints, varEnum = VarEnum.VT_I2),
            IReadOnlyCollection<int> ints => CreateFromCollection(ints, varEnum = VarEnum.VT_I4),
            IReadOnlyCollection<long> ints => CreateFromCollection(ints, varEnum = VarEnum.VT_I8),

            IReadOnlyCollection<byte> ints => CreateFromCollection(ints, varEnum = VarEnum.VT_UI1),
            IReadOnlyCollection<ushort> ints => CreateFromCollection(ints, varEnum = VarEnum.VT_UI2),
            IReadOnlyCollection<uint> ints => CreateFromCollection(ints, varEnum = VarEnum.VT_UI4),
            IReadOnlyCollection<ulong> ints => CreateFromCollection(ints, varEnum = VarEnum.VT_UI8),

            IReadOnlyCollection<float> ints => CreateFromCollection(ints, varEnum = VarEnum.VT_R4),
            IReadOnlyCollection<double> ints => CreateFromCollection(ints, varEnum = VarEnum.VT_R8),

            IReadOnlyCollection<nint> ints => CreateFromCollection(ints, varEnum = VarEnum.VT_INT),
            IReadOnlyCollection<nuint> ints => CreateFromCollection(ints, varEnum = VarEnum.VT_UINT),

            IReadOnlyList<bool> bools => CreateFromBools(bools, varEnum = VarEnum.VT_BOOL),

            IReadOnlyList<string> strings => CreateFromStrings(strings, varEnum = VarEnum.VT_BSTR),

            IReadOnlyList<object> objects => CreateFromObjects(objects, varEnum = VarEnum.VT_UNKNOWN),
            
            _ => null
        };

        return safearray is not null;
    }

    [LibraryImport("oleaut32.dll")]
    private static unsafe partial SAFEARRAY* SafeArrayCreate(VarEnum vt, uint cDims, in SAFEARRAYBOUND rgsabound);

    [LibraryImport("oleaut32.dll")]
    private static unsafe partial void SafeArrayDestroy(SAFEARRAY* array);

    [LibraryImport("oleaut32.dll")]
    [PreserveSig]
    private static unsafe partial int SafeArrayLock(SAFEARRAY* array);

    [LibraryImport("oleaut32.dll")]
    private static unsafe partial void SafeArrayUnlock(SAFEARRAY* array);

    private static TRes AccessData<TRes>(SafeArrayRef safearray, Func<IntPtr, int, TRes> accessor)
    {
        var lockResult = SafeArrayLock(safearray._ptr);
        if (lockResult != 0)
        {
            throw new Win32Exception(lockResult);
        }

        Debug.Assert(safearray._ptr->cDims == 1);

        try
        {
            var data = safearray._ptr->pvData;
            var length= safearray._ptr->rgsabound[0].cElements;
            return accessor(new IntPtr(data), (int)length);
        }
        finally
        {
            SafeArrayUnlock(safearray._ptr);
        }
    }
}
#endif

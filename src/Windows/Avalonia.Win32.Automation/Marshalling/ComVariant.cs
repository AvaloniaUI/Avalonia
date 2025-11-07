using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Avalonia.Win32.Automation.Marshalling;

#if NET7_0_OR_GREATER
// Oversimplified ComVariant implementation based on https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Runtime/InteropServices/Marshalling/ComVariant.cs
// Available 
[StructLayout(LayoutKind.Explicit)]
internal struct ComVariant : IDisposable
{
    // VARIANT_BOOL constants.
    internal const short VARIANT_TRUE = -1;
    internal const short VARIANT_FALSE = 0;

#if DEBUG
    static unsafe ComVariant()
    {
        // Variant size is the size of 4 pointers (16 bytes) on a 32-bit processor,
        // and 3 pointers (24 bytes) on a 64-bit processor.
        // See definition in oaidl.h in the Windows SDK.
        int variantSize = sizeof(ComVariant);
        if (IntPtr.Size == 4)
        {
            Debug.Assert(variantSize == (4 * IntPtr.Size));
        }
        else
        {
            Debug.Assert(IntPtr.Size == 8);
            Debug.Assert(variantSize == (3 * IntPtr.Size));
        }
    }
#endif

    // Most of the data types in the Variant are carried in _typeUnion
    [FieldOffset(0)] private TypeUnion _typeUnion;

    [StructLayout(LayoutKind.Sequential)]
    private struct TypeUnion
    {
        public ushort _vt;
        public ushort _wReserved1;
        public ushort _wReserved2;
        public ushort _wReserved3;

        public UnionTypes _unionTypes;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Record
    {
        public IntPtr _record;
        public IntPtr _recordInfo;
    }

    [StructLayout(LayoutKind.Explicit)]
    private unsafe struct UnionTypes
    {
        [FieldOffset(0)] public sbyte _i1;
        [FieldOffset(0)] public short _i2;
        [FieldOffset(0)] public int _i4;
        [FieldOffset(0)] public long _i8;
        [FieldOffset(0)] public byte _ui1;
        [FieldOffset(0)] public ushort _ui2;
        [FieldOffset(0)] public uint _ui4;
        [FieldOffset(0)] public ulong _ui8;
        [FieldOffset(0)] public int _int;
        [FieldOffset(0)] public uint _uint;
        [FieldOffset(0)] public short _bool;
        [FieldOffset(0)] public int _error;
        [FieldOffset(0)] public float _r4;
        [FieldOffset(0)] public double _r8;
        [FieldOffset(0)] public long _cy;
        [FieldOffset(0)] public double _date;
        [FieldOffset(0)] public IntPtr _bstr;
        [FieldOffset(0)] public IntPtr _unknown;
        [FieldOffset(0)] public IntPtr _dispatch;
        [FieldOffset(0)] public IntPtr _pvarVal;
        [FieldOffset(0)] public IntPtr _byref;
        [FieldOffset(0)] public Record _record;
        [FieldOffset(0)] public SafeArrayRef parray;
        [FieldOffset(0)] public SafeArrayRef*pparray;
    }

    /// <summary>
    /// Release resources owned by this <see cref="ComVariant"/> instance.
    /// </summary>
    public void Dispose()
    {
        // Re-implement the same clearing semantics as PropVariantClear manually for non-Windows platforms.
        if (VarType == VarEnum.VT_BSTR)
        {
            Marshal.FreeBSTR(_typeUnion._unionTypes._bstr);
        }
        else if (VarType.HasFlag(VarEnum.VT_ARRAY))
        {
            _typeUnion._unionTypes.parray.Destroy();
        }
        else if (VarType == VarEnum.VT_UNKNOWN || VarType == VarEnum.VT_DISPATCH)
        {
            if (_typeUnion._unionTypes._unknown != IntPtr.Zero)
            {
                Marshal.Release(_typeUnion._unionTypes._unknown);
            }
        }
        else if (VarType == VarEnum.VT_LPSTR || VarType == VarEnum.VT_LPWSTR || VarType == VarEnum.VT_CLSID)
        {
            Marshal.FreeCoTaskMem(_typeUnion._unionTypes._byref);
        }
        else if (VarType == VarEnum.VT_STREAM || VarType == VarEnum.VT_STREAMED_OBJECT ||
                 VarType == VarEnum.VT_STORAGE || VarType == VarEnum.VT_STORED_OBJECT)
        {
            if (_typeUnion._unionTypes._unknown != IntPtr.Zero)
            {
                Marshal.Release(_typeUnion._unionTypes._unknown);
            }
        }

        // Clear out this ComVariant instance.
        this = default;
    }

    /// <summary>
    /// Create an <see cref="ComVariant"/> instance from the specified value.
    /// </summary>
    /// <param name="value">The value to wrap in an <see cref="ComVariant"/>.</param>
    /// <returns>An <see cref="ComVariant"/> that contains the provided value.</returns>
    public static unsafe ComVariant Create(object? value)
    {
        if (value is null) return Null;
        
        Unsafe.SkipInit(out ComVariant variant);

        if (value.GetType().IsEnum)
        {
            var underlyingType = Enum.GetUnderlyingType(value.GetType());
            value = Convert.ChangeType(value, underlyingType);
        }

        if (value is short)
        {
            variant.VarType = VarEnum.VT_I2;
            variant._typeUnion._unionTypes._i2 = (short)value;
        }
        else if (value is int)
        {
            variant.VarType = VarEnum.VT_I4;
            variant._typeUnion._unionTypes._i4 = (int)value;
        }
        else if (value is float)
        {
            variant.VarType = VarEnum.VT_R4;
            variant._typeUnion._unionTypes._r4 = (float)value;
        }
        else if (value is double)
        {
            variant.VarType = VarEnum.VT_R8;
            variant._typeUnion._unionTypes._r8 = (double)value;
        }
        else if (value is DateTime)
        {
            variant.VarType = VarEnum.VT_DATE;
            variant._typeUnion._unionTypes._date = ((DateTime)value).ToOADate();
        }
        else if (value is string)
        {
            variant.VarType = VarEnum.VT_BSTR;
            variant._typeUnion._unionTypes._bstr = Marshal.StringToBSTR((string)value);
        }
        else if (value is bool)
        {
            // bool values in OLE VARIANTs are VARIANT_BOOL values.
            variant.VarType = VarEnum.VT_BOOL;
            variant._typeUnion._unionTypes._bool = ((bool)value) ? VARIANT_TRUE : VARIANT_FALSE;
        }
        else if (value is sbyte)
        {
            variant.VarType = VarEnum.VT_I1;
            variant._typeUnion._unionTypes._i1 = (sbyte)value;
        }
        else if (value is byte)
        {
            variant.VarType = VarEnum.VT_UI1;
            variant._typeUnion._unionTypes._ui1 = (byte)value;
        }
        else if (value is ushort)
        {
            variant.VarType = VarEnum.VT_UI2;
            variant._typeUnion._unionTypes._ui2 = (ushort)value;
        }
        else if (value is uint)
        {
            variant.VarType = VarEnum.VT_UI4;
            variant._typeUnion._unionTypes._ui4 = (uint)value;
        }
        else if (value is long)
        {
            variant.VarType = VarEnum.VT_I8;
            variant._typeUnion._unionTypes._i8 = (long)value;
        }
        else if (value is ulong)
        {
            variant.VarType = VarEnum.VT_UI8;
            variant._typeUnion._unionTypes._ui8 = (ulong)value;
        }
        else if (value is IEnumerable list && SafeArrayRef.TryCreate(list, out var array, out var arrayEnum))
        {
            variant.VarType = arrayEnum | VarEnum.VT_ARRAY;
            variant._typeUnion._unionTypes.parray = array.Value;
        }
        else if (ComWrappers.TryGetComInstance(value, out var unknown))
        {
            variant.VarType = VarEnum.VT_UNKNOWN;
            variant._typeUnion._unionTypes._unknown = unknown;
        }
        else
        {
            throw new ArgumentException("UnsupportedType", value.GetType().Name);
        }

        return variant;
    }

    /// <summary>
    /// A <see cref="ComVariant"/> instance that represents a null value with <see cref="VarEnum.VT_NULL"/> type.
    /// </summary>
    public static ComVariant Null { get; } = new() { VarType = VarEnum.VT_NULL };

    /// <summary>
    /// Create a managed value based on the value in the <see cref="ComVariant"/> instance.
    /// </summary>
    /// <returns>The managed value contained in this variant.</returns>
    public readonly unsafe object? AsObject()
    {
        if (VarType == VarEnum.VT_EMPTY)
        {
            return null;
        }

        return VarType switch
        {
            VarEnum.VT_NULL => null,
            // integer
            VarEnum.VT_I1 => _typeUnion._unionTypes._i1,
            VarEnum.VT_I2 => _typeUnion._unionTypes._i2,
            VarEnum.VT_I4 => _typeUnion._unionTypes._i4,
            VarEnum.VT_I8 => _typeUnion._unionTypes._i8,
            VarEnum.VT_INT => _typeUnion._unionTypes._i4,
            VarEnum.VT_ERROR => _typeUnion._unionTypes._i4,
            // unsigned integer
            VarEnum.VT_UI1 => _typeUnion._unionTypes._ui1,
            VarEnum.VT_UI2 => _typeUnion._unionTypes._ui2,
            VarEnum.VT_UI4 => _typeUnion._unionTypes._ui4,
            VarEnum.VT_UI8 => _typeUnion._unionTypes._ui8,
            VarEnum.VT_UINT => _typeUnion._unionTypes._ui4,
            // floating
            VarEnum.VT_R4 => _typeUnion._unionTypes._r4,
            VarEnum.VT_R8 => _typeUnion._unionTypes._r8,
            // date
            VarEnum.VT_DATE => DateTime.FromOADate(_typeUnion._unionTypes._date),
            // string
            VarEnum.VT_BSTR => Marshal.PtrToStringBSTR(_typeUnion._unionTypes._bstr),
            // bool
            VarEnum.VT_BOOL => _typeUnion._unionTypes._bool != VARIANT_FALSE,
            // unknown
            VarEnum.VT_UNKNOWN => ComWrappers.TryGetObject(_typeUnion._unionTypes._unknown, out var obj) ? obj : null,
            // array
            { } varEnum when varEnum.HasFlag(VarEnum.VT_ARRAY) => (varEnum ^ VarEnum.VT_ARRAY) switch
            {
                // integer
                VarEnum.VT_I1 => SafeArrayRef.ToArray<sbyte>(_typeUnion._unionTypes.parray),
                VarEnum.VT_I2 => SafeArrayRef.ToArray<short>(_typeUnion._unionTypes.parray),
                VarEnum.VT_I4 => SafeArrayRef.ToArray<int>(_typeUnion._unionTypes.parray),
                VarEnum.VT_I8 => SafeArrayRef.ToArray<long>(_typeUnion._unionTypes.parray),
                VarEnum.VT_INT => SafeArrayRef.ToArray<int>(_typeUnion._unionTypes.parray),
                // unsigned integer
                VarEnum.VT_UI1 => SafeArrayRef.ToArray<byte>(_typeUnion._unionTypes.parray),
                VarEnum.VT_UI2 => SafeArrayRef.ToArray<ushort>(_typeUnion._unionTypes.parray),
                VarEnum.VT_UI4 => SafeArrayRef.ToArray<uint>(_typeUnion._unionTypes.parray),
                VarEnum.VT_UI8 => SafeArrayRef.ToArray<ulong>(_typeUnion._unionTypes.parray),
                VarEnum.VT_UINT => SafeArrayRef.ToArray<uint>(_typeUnion._unionTypes.parray),
                // floating
                VarEnum.VT_R4 => SafeArrayRef.ToArray<float>(_typeUnion._unionTypes.parray),
                VarEnum.VT_R8 => SafeArrayRef.ToArray<double>(_typeUnion._unionTypes.parray),
                // string
                VarEnum.VT_BSTR => SafeArrayRef.ToArray<string>(_typeUnion._unionTypes.parray),
                // variant
                VarEnum.VT_UNKNOWN => SafeArrayRef.ToArray<IntPtr>(_typeUnion._unionTypes.parray),
                _ => throw new ArgumentException($"Unknown variant type: {varEnum}")
            },
            _ => throw new ArgumentException($"Unknown variant type: {VarType}")
        };
    }

    /// <summary>
    /// The type of the data stored in this <see cref="ComVariant"/>.
    /// </summary>
    public VarEnum VarType
    {
        readonly get => (VarEnum)_typeUnion._vt;
        private set => _typeUnion._vt = (ushort)value;
    }
}
#endif

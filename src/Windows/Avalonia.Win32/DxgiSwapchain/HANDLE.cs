using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Win32.DxgiSwapchain
{
#nullable enable
    public unsafe struct HANDLE
    {
        public readonly void* Value;

        public HANDLE(void* value)
        {
            Value = value;
        }

        public static HANDLE INVALID_VALUE => new HANDLE((void*)(-1));

        public static HANDLE NULL => new HANDLE(null);

        public static bool operator ==(HANDLE left, HANDLE right) => left.Value == right.Value;

        public static bool operator !=(HANDLE left, HANDLE right) => left.Value != right.Value;

        public override bool Equals(object? obj) => (obj is HANDLE other) && Equals(other);

        public bool Equals(HANDLE other) => ((nuint)(Value)).Equals((nuint)(other.Value));

        public override int GetHashCode() => ((nuint)(Value)).GetHashCode();

        public override string ToString() => ((IntPtr)Value).ToString();
    }
#nullable restore
}

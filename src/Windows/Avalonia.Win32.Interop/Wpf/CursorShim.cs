using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Avalonia.Win32.Interop.Wpf
{
    static class CursorShim
    {
        public static Cursor FromHCursor(IntPtr hcursor)
        {
            var field = typeof(Cursor).GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(f => f.FieldType == typeof(SafeHandle));
            if (field == null)
                return null;
            var rv = (Cursor) FormatterServices.GetUninitializedObject(typeof(Cursor));
            field.SetValue(rv, new SafeHandleShim(hcursor));
            return rv;
        }

        class SafeHandleShim : SafeHandle
        {
            public SafeHandleShim(IntPtr hcursor) : base(new IntPtr(-1), false)
            {
                this.handle = hcursor;
            }

            protected override bool ReleaseHandle() => true;

            public override bool IsInvalid => false;
        }
    }
}

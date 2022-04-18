using System;
using System.Runtime.InteropServices;

namespace Avalonia.Wayland
{
    public static class LibXkbCommon
    {
        private const string libXkbCommon = "libxkbcommon.so.0";

        [DllImport(libXkbCommon)]
        public static extern IntPtr xkb_context_new(int flags);

        [DllImport(libXkbCommon)]
        public static extern IntPtr xkb_keymap_new_from_string(IntPtr context, IntPtr @string, uint format, uint flags);

        [DllImport(libXkbCommon)]
        public static extern IntPtr xkb_state_new(IntPtr keymap);

        [DllImport(libXkbCommon)]
        public static extern void xkb_keymap_unref(IntPtr keymap);

        [DllImport(libXkbCommon)]
        public static extern void xkb_state_unref(IntPtr state);

        [DllImport(libXkbCommon)]
        public static extern void xkb_context_unref(IntPtr context);

        [DllImport(libXkbCommon)]
        public static extern unsafe uint xkb_state_key_get_syms(IntPtr state, uint code, uint** syms);
    }
}

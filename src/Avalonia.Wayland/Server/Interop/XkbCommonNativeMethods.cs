using System;
using System.Runtime.InteropServices;

namespace Avalonia.Wayland.Server.Interop;

static unsafe class XkbCommonNativeMethods
{
    private const string LibXkbCommon = "libxkbcommon.so.0";

    // xkb_context

    [DllImport(LibXkbCommon)]
    public static extern IntPtr xkb_context_new(int flags);

    [DllImport(LibXkbCommon)]
    public static extern void xkb_context_unref(IntPtr context);

    // xkb_keymap

    [DllImport(LibXkbCommon)]
    public static extern IntPtr xkb_keymap_new_from_buffer(
        IntPtr context, byte* buffer, IntPtr length, int format, int flags);

    [DllImport(LibXkbCommon)]
    public static extern void xkb_keymap_unref(IntPtr keymap);

    [DllImport(LibXkbCommon)]
    public static extern uint xkb_keymap_num_layouts(IntPtr keymap);

    [DllImport(LibXkbCommon)]
    public static extern int xkb_keymap_key_get_syms_by_level(
        IntPtr keymap, uint key, uint layout, uint level, out uint* symsOut);

    // xkb_state

    [DllImport(LibXkbCommon)]
    public static extern IntPtr xkb_state_new(IntPtr keymap);

    [DllImport(LibXkbCommon)]
    public static extern void xkb_state_unref(IntPtr state);

    [DllImport(LibXkbCommon)]
    public static extern uint xkb_state_key_get_one_sym(IntPtr state, uint key);

    [DllImport(LibXkbCommon)]
    public static extern int xkb_state_key_get_utf8(
        IntPtr state, uint key, byte* buffer, IntPtr size);

    [DllImport(LibXkbCommon)]
    public static extern int xkb_state_update_mask(
        IntPtr state,
        uint modsDepressed, uint modsLatched, uint modsLocked,
        uint layoutDepressed, uint layoutLatched, uint layoutLocked);

    // Constants
    public const int XKB_CONTEXT_NO_FLAGS = 0;
    public const int XKB_KEYMAP_FORMAT_TEXT_V1 = 1;
    public const int XKB_KEYMAP_COMPILE_NO_FLAGS = 0;

    // mmap constants
    public const int PROT_READ = 1;
    public const int MAP_PRIVATE = 2;

    // xkb_compose

    [DllImport(LibXkbCommon)]
    public static extern IntPtr xkb_compose_table_new_from_locale(
        IntPtr context,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string locale,
        int flags);

    [DllImport(LibXkbCommon)]
    public static extern void xkb_compose_table_unref(IntPtr table);

    [DllImport(LibXkbCommon)]
    public static extern IntPtr xkb_compose_state_new(IntPtr table, int flags);

    [DllImport(LibXkbCommon)]
    public static extern void xkb_compose_state_unref(IntPtr state);

    [DllImport(LibXkbCommon)]
    public static extern int xkb_compose_state_feed(IntPtr state, uint keysym);

    [DllImport(LibXkbCommon)]
    public static extern int xkb_compose_state_get_status(IntPtr state);

    [DllImport(LibXkbCommon)]
    public static extern uint xkb_compose_state_get_one_sym(IntPtr state);

    [DllImport(LibXkbCommon)]
    public static extern int xkb_compose_state_get_utf8(
        IntPtr state, byte* buffer, IntPtr size);

    [DllImport(LibXkbCommon)]
    public static extern void xkb_compose_state_reset(IntPtr state);

    // Compose constants
    public const int XKB_COMPOSE_COMPILE_NO_FLAGS = 0;
    public const int XKB_COMPOSE_STATE_NO_FLAGS = 0;

    // xkb_compose_status
    public const int XKB_COMPOSE_NOTHING = 0;
    public const int XKB_COMPOSE_COMPOSING = 1;
    public const int XKB_COMPOSE_COMPOSED = 2;
    public const int XKB_COMPOSE_CANCELLED = 3;

    // xkb_compose_feed_result
    public const int XKB_COMPOSE_FEED_IGNORED = 0;
    public const int XKB_COMPOSE_FEED_ACCEPTED = 1;
}

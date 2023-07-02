using System;
using System.Runtime.InteropServices;
using Avalonia.FreeDesktop;

namespace Avalonia.Wayland
{
    internal static class LibXkbCommon
    {
        private const string XkbCommon = "libxkbcommon.so.0";

        [DllImport(XkbCommon)]
        public static extern IntPtr xkb_context_new(int flags);

        [DllImport(XkbCommon)]
        public static extern void xkb_context_unref(IntPtr context);

        [DllImport(XkbCommon)]
        public static extern IntPtr xkb_keymap_new_from_string(IntPtr context, IntPtr keymapFormat, uint format, uint flags);

        [DllImport(XkbCommon)]
        public static extern uint xkb_keymap_num_layouts_for_key(IntPtr keymap, uint key);

        [DllImport(XkbCommon)]
        public static extern uint xkb_state_mod_name_is_active(IntPtr keymap, string name, XkbStateComponent type);

        [DllImport(XkbCommon)]
        public static extern unsafe int xkb_keymap_key_get_syms_by_level(IntPtr keymap, uint code, uint layout, uint level, uint** syms_out);

        [DllImport(XkbCommon)]
        public static extern bool xkb_keymap_key_repeats(IntPtr keymap, uint key);

        [DllImport(XkbCommon)]
        public static extern void xkb_keymap_unref(IntPtr keymap);

        [DllImport(XkbCommon)]
        public static extern IntPtr xkb_state_new(IntPtr keymap);

        [DllImport(XkbCommon)]
        public static extern IntPtr xkb_state_get_keymap(IntPtr state);

        [DllImport(XkbCommon)]
        public static extern void xkb_state_update_mask(IntPtr state, uint modsDepressed, uint modsLatched, uint modsLocked, uint layoutDepressed, uint layoutLatched, uint layoutLocked);

        [DllImport(XkbCommon)]
        public static extern uint xkb_state_serialize_mods(IntPtr state, XkbStateComponent components);

        [DllImport(XkbCommon)]
        public static extern uint xkb_state_key_get_level(IntPtr state, uint code, uint layout);

        [DllImport(XkbCommon)]
        public static extern XkbKey xkb_state_key_get_one_sym(IntPtr state, uint key);

        [DllImport(XkbCommon)]
        public static extern unsafe int xkb_state_key_get_utf8(IntPtr state, uint key, byte* buffer, int size);

        [DllImport(XkbCommon)]
        public static extern void xkb_state_unref(IntPtr state);

        [DllImport(XkbCommon)]
        public static extern IntPtr xkb_compose_table_new_from_locale(IntPtr context, string locale, int flags);

        [DllImport(XkbCommon)]
        public static extern void xkb_compose_table_unref(IntPtr composeTable);

        [DllImport(XkbCommon)]
        public static extern IntPtr xkb_compose_state_new(IntPtr composeTable, int flags);

        [DllImport(XkbCommon)]
        public static extern XkbComposeFeedResult xkb_compose_state_feed(IntPtr composeState, XkbKey sym);

        [DllImport(XkbCommon)]
        public static extern XkbComposeStatus xkb_compose_state_get_status(IntPtr composeState);

        [DllImport(XkbCommon)]
        public static extern unsafe int xkb_compose_state_get_utf8(IntPtr composeState, byte* buffer, int size);

        [DllImport(XkbCommon)]
        public static extern void xkb_compose_state_reset(IntPtr composeState);

        [Flags]
        public enum XkbStateComponent
        {
            XKB_STATE_MODS_DEPRESSED = 1,
            XKB_STATE_MODS_LATCHED = 2,
            XKB_STATE_MODS_LOCKED = 4,
            XKB_STATE_MODS_EFFECTIVE = 8,
            XKB_STATE_LAYOUT_DEPRESSED = 16,
            XKB_STATE_LAYOUT_LATCHED = 32,
            XKB_STATE_LAYOUT_LOCKED = 64,
            XKB_STATE_LAYOUT_EFFECTIVE = 128,
            XKB_STATE_LEDS = 256
        }

        public enum XkbComposeFeedResult
        {
            XKB_COMPOSE_FEED_IGNORED,
            XKB_COMPOSE_FEED_ACCEPTED
        }

        public enum XkbComposeStatus
        {
            XKB_COMPOSE_NOTHING,
            XKB_COMPOSE_COMPOSING,
            XKB_COMPOSE_COMPOSED,
            XKB_COMPOSE_CANCELLED
        }
    }
}

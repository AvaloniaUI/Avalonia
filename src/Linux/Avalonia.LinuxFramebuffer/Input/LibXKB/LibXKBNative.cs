using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Avalonia.LinuxFramebuffer.Input.LibXKB;

internal static unsafe class LibXKBNative
{
    private const string LibXkb = "libxkbcommon.so.0";

    [SecurityCritical]
    public sealed class xkb_keymap : SafeHandleZeroOrMinusOneIsInvalid
    {
        [SecurityCritical]
        public xkb_keymap() :base(true)
        {
                
        }
        
        [SecurityCritical]
        public xkb_keymap(bool ownsHandle) : base(ownsHandle)
        {
        }

        [SecurityCritical]
        protected override bool ReleaseHandle()
        {
            xkb_keymap_unref(handle);
            return true;
        }
    }
    
    [SecurityCritical]
    public sealed class xkb_state : SafeHandleZeroOrMinusOneIsInvalid
    {
        [SecurityCritical]
        public xkb_state():base(true)
        {
                
        }
        [SecurityCritical]
        public xkb_state(bool ownsHandle) : base(ownsHandle)
        {
        }
        
        [SecurityCritical]
        protected override bool ReleaseHandle()
        {
            xkb_state_unref(handle);
            return true;
        }
    }
    
    [SecurityCritical]
    public sealed class xkb_context : SafeHandleZeroOrMinusOneIsInvalid
    {
        [SecurityCritical]
        public xkb_context():base(true)
        {
                
        }
        
        [SecurityCritical]
        public xkb_context(bool ownsHandle) : base(ownsHandle)
        {
        }

        [SecurityCritical]
        protected override bool ReleaseHandle()
        {
            xkb_context_unref(handle);
            return true;
        }
    }

    public enum xkb_key_direction
    {
        XKB_KEY_UP,
        XKB_KEY_DOWN,
    }

    public enum xkb_context_flags
    {
        XKB_CONTEXT_NO_FLAGS = 0,
        XKB_CONTEXT_NO_DEFAULT_INCLUDES = (1 << 0),
        XKB_CONTEXT_NO_ENVIRONMENT_NAMES = (1 << 1),
    }

    public enum xkb_state_component
    {
        XKB_STATE_MODS_DEPRESSED = (1 << 0),
        XKB_STATE_MODS_LATCHED = (1 << 1),
        XKB_STATE_MODS_LOCKED = (1 << 2),
        XKB_STATE_MODS_EFFECTIVE = (1 << 3),
        XKB_STATE_LAYOUT_DEPRESSED = (1 << 4),
        XKB_STATE_LAYOUT_LATCHED = (1 << 5),
        XKB_STATE_LAYOUT_LOCKED = (1 << 6),
        XKB_STATE_LAYOUT_EFFECTIVE = (1 << 7),
        XKB_STATE_LEDS = (1 << 8),
    }

    public const string XKB_MOD_NAME_SHIFT = "Shift";
    public const string XKB_MOD_NAME_CAPS = "Lock";
    public const string XKB_MOD_NAME_CTRL = "Control";
    public const string XKB_MOD_NAME_ALT = "Mod1";
    public const string XKB_MOD_NAME_NUM = "Mod2";
    public const string XKB_MOD_NAME_LOGO = "Mod4";

    public const string XKB_LED_NAME_CAPS = "Caps Lock";
    public const string XKB_LED_NAME_NUM = "Num Lock";
    public const string XKB_LED_NAME_SCROLL = "Scroll Lock";


    public enum xkb_keymap_compile_flags
    {
        XKB_KEYMAP_COMPILE_NO_FLAGS = 0,
    }

    public unsafe partial struct xkb_rule_names
    {
        //"const char *
        public sbyte* rules;

        //const char *
        public sbyte* model;

        //const char *
        public sbyte* layout;

        //const char *
        public sbyte* variant;

        //const char *
        public sbyte* options;
    }

    [DllImport(LibXkb, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern void xkb_state_unref(/* xkb_state* */ IntPtr state);

    [DllImport(LibXkb, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern void xkb_keymap_unref(/* struct xkb_keymap * */ IntPtr keymap);

    [DllImport(LibXkb, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern void xkb_context_unref(/*struct xkb_context * */ IntPtr context);

    [DllImport(LibXkb, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true, SetLastError = true)]
    public static extern xkb_context xkb_context_new(xkb_context_flags flags);

    [DllImport(LibXkb, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern xkb_keymap xkb_keymap_new_from_names(xkb_context context, xkb_rule_names* names, xkb_keymap_compile_flags flags);

    [DllImport(LibXkb, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern xkb_state xkb_state_new(xkb_keymap keymap);

    [DllImport(LibXkb, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern uint xkb_state_key_get_one_sym(xkb_state state, uint key);

    [DllImport(LibXkb, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern xkb_state_component xkb_state_update_key(xkb_state state, uint key, xkb_key_direction direction);

    [DllImport(LibXkb, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern bool xkb_keymap_key_repeats(xkb_keymap keymap, uint key);

    [DllImport(LibXkb, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int xkb_state_mod_name_is_active(xkb_state state, string name, xkb_state_component type);

    [DllImport(LibXkb, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern xkb_keymap xkb_state_get_keymap(xkb_state state);

    [DllImport(LibXkb, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern uint xkb_keymap_num_layouts_for_key(xkb_keymap keymap, uint key);

    [DllImport(LibXkb, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern uint xkb_state_key_get_layout(xkb_state state, uint key);

    [DllImport(LibXkb, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern uint xkb_state_key_get_level(xkb_state state, uint key, uint layout);

    [DllImport(LibXkb, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int xkb_keymap_key_get_syms_by_level(xkb_keymap keymap, uint key, uint layout, uint level, out IntPtr syms);

    [DllImport(LibXkb, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern uint xkb_state_serialize_mods(xkb_state state, xkb_state_component components);

    [DllImport(LibXkb, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern uint xkb_keymap_min_keycode(xkb_keymap keymap);

    [DllImport(LibXkb, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern uint xkb_keymap_max_keycode(xkb_keymap keymap);

    [DllImport(LibXkb, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern xkb_state_component xkb_state_update_mask(xkb_state state
        , uint depressed_mods
        , uint latched_mods
        , uint locked_mods
        , uint depressed_layout
        , uint latched_layout
        , uint locked_layout);

    [DllImport(LibXkb, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int xkb_keysym_to_utf8(uint keysym, [MarshalAs(UnmanagedType.LPStr)] StringBuilder buffer, nuint size);

    [DllImport(LibXkb, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int xkb_state_key_get_utf8(xkb_state state, uint key, [MarshalAs(UnmanagedType.LPStr)] StringBuilder buffer, nuint size);
}

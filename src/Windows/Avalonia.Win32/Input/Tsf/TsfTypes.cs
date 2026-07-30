using System;
using System.Runtime.InteropServices;

namespace Avalonia.Win32.Input.Tsf
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct TS_STATUS
    {
        public uint DynamicFlags;
        public uint StaticFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TS_SELECTIONSTYLE
    {
        public uint ActiveSelectionEnd;
        public int InterimChar;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TS_SELECTION_ACP
    {
        public int Start;
        public int End;
        public TS_SELECTIONSTYLE Style;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TS_RUNINFO
    {
        public uint Count;
        public uint Type;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TS_TEXTCHANGE
    {
        public int Start;
        public int OldEnd;
        public int NewEnd;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TF_DA_COLOR
    {
        public uint Type;
        public uint Value;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TF_DISPLAYATTRIBUTE
    {
        public TF_DA_COLOR Text;
        public TF_DA_COLOR Background;
        public uint LineStyle;
        public int BoldLine;
        public TF_DA_COLOR LineColor;
        public int Attribute;
    }

    /// <summary>
    /// Blittable VARIANT layout for reading scalar property values through an opaque
    /// pointer parameter; matches the native size on both pointer widths.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct TsfVariant
    {
        public ushort Vt;
        public ushort Reserved1;
        public ushort Reserved2;
        public ushort Reserved3;
        public IntPtr Data1;
        public IntPtr Data2;
    }

    internal static class TsfConstants
    {
        public const uint TS_LF_SYNC = 0x1;
        public const uint TS_LF_READ = 0x2;
        public const uint TS_LF_READWRITE = 0x6;

        public const uint TS_AS_TEXT_CHANGE = 0x1;
        public const uint TS_AS_SEL_CHANGE = 0x2;
        public const uint TS_AS_LAYOUT_CHANGE = 0x4;
        public const uint TS_AS_ATTR_CHANGE = 0x8;
        public const uint TS_AS_STATUS_CHANGE = 0x10;

        public const uint TS_SD_READONLY = 0x1;

        public const uint TS_SS_NOHIDDENTEXT = 0x8;

        public const uint TS_AE_NONE = 0;
        public const uint TS_AE_START = 1;
        public const uint TS_AE_END = 2;

        public const uint TS_RT_PLAIN = 0;

        public const uint TF_DEFAULT_SELECTION = 0xFFFFFFFF;

        public const uint TS_IAS_NOQUERY = 0x1;
        public const uint TS_IAS_QUERYONLY = 0x2;

        public const uint TF_LS_NONE = 0;
        public const uint TF_LS_SOLID = 1;
        public const uint TF_LS_DOT = 2;
        public const uint TF_LS_DASH = 3;
        public const uint TF_LS_SQUIGGLE = 4;

        public const uint TF_CT_NONE = 0;
        public const uint TF_CT_SYSCOLOR = 1;
        public const uint TF_CT_COLORREF = 2;

        public const int TF_ATTR_INPUT = 0;
        public const int TF_ATTR_TARGET_CONVERTED = 1;
        public const int TF_ATTR_CONVERTED = 2;
        public const int TF_ATTR_TARGET_NOTCONVERTED = 3;
        public const int TF_ATTR_INPUT_ERROR = 4;
        public const int TF_ATTR_FIXEDCONVERTED = 5;

        public const ushort VT_EMPTY = 0;
        public const ushort VT_I4 = 3;

        /// <summary>The context property holding per-clause display attribute atoms.</summary>
        public static readonly Guid PropAttribute = new("34745c63-b2f0-4784-8b67-5e12c8701a31");

        public static readonly Guid ClsidCategoryMgr = new("a4b544a1-438d-4b41-9325-869523e2d6c7");
        public static readonly Guid ClsidDisplayAttributeMgr = new("3ce74de4-53d3-4d74-8b83-431b3828ba53");

        public const int S_OK = 0;
        public const int TS_S_ASYNC = 0x00040300;
        public const int E_NOTIMPL = unchecked((int)0x80004001);
        public const int E_UNEXPECTED = unchecked((int)0x8000FFFF);
        public const int E_INVALIDARG = unchecked((int)0x80070057);
        public const int TS_E_INVALIDPOS = unchecked((int)0x80040200);
        public const int TS_E_NOLOCK = unchecked((int)0x80040201);
        public const int TS_E_NOLAYOUT = unchecked((int)0x80040206);
        public const int TS_E_INVALIDPOINT = unchecked((int)0x80040207);
        public const int TS_E_SYNCHRONOUS = unchecked((int)0x80040208);
        public const int TS_E_READONLY = unchecked((int)0x80040209);
    }
}

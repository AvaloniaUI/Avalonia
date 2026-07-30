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

    internal static class TsfConstants
    {
        public const uint TS_LF_SYNC = 0x1;
        public const uint TS_LF_READ = 0x2;
        public const uint TS_LF_READWRITE = 0x6;

        public const uint TS_AS_TEXT_CHANGE = 0x1;
        public const uint TS_AS_SEL_CHANGE = 0x2;
        public const uint TS_AS_LAYOUT_CHANGE = 0x4;
        public const uint TS_AS_STATUS_CHANGE = 0x8;

        public const uint TS_SD_READONLY = 0x2;

        public const uint TS_SS_NOHIDDENTEXT = 0x8;

        public const uint TS_AE_NONE = 0;
        public const uint TS_AE_START = 1;
        public const uint TS_AE_END = 2;

        public const uint TS_RT_PLAIN = 0;

        public const uint TF_DEFAULT_SELECTION = 0xFFFFFFFF;

        public const uint TS_IAS_NOQUERY = 0x1;
        public const uint TS_IAS_QUERYONLY = 0x2;

        public const int S_OK = 0;
        public const int TS_S_ASYNC = 0x1;
        public const int E_NOTIMPL = unchecked((int)0x80004001);
        public const int E_UNEXPECTED = unchecked((int)0x8000FFFF);
        public const int E_INVALIDARG = unchecked((int)0x80070057);
        public const int TS_E_NOLOCK = unchecked((int)0x80040201);
        public const int TS_E_SYNCHRONOUS = unchecked((int)0x80040207);
        public const int TS_E_INVALIDPOS = unchecked((int)0x80040200);
        public const int TS_E_READONLY = unchecked((int)0x80040206);
    }
}

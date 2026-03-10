using System;
using System.Runtime.InteropServices;

// Just to keep "netstandard2.0" build happy
namespace System.Runtime.InteropServices.Marshalling
{
}

namespace Avalonia.Win32.Automation.Interop
{
    internal static partial class UiaCoreTypesApi
    {
        internal enum AutomationIdType
        {
            Property,
            Pattern,
            Event,
            ControlType,
            TextAttribute
        }

        internal const int UIA_E_ELEMENTNOTENABLED = unchecked((int)0x80040200);
        internal const int UIA_E_ELEMENTNOTAVAILABLE = unchecked((int)0x80040201);
        internal const int UIA_E_NOCLICKABLEPOINT = unchecked((int)0x80040202);
        internal const int UIA_E_PROXYASSEMBLYNOTLOADED = unchecked((int)0x80040203);

        internal static int UiaLookupId(AutomationIdType type, ref Guid guid)
        {
            return RawUiaLookupId(type, ref guid);
        }

        [LibraryImport("UIAutomationCore.dll", EntryPoint = "UiaLookupId", StringMarshalling = StringMarshalling.Utf8)]
        private static partial int RawUiaLookupId(AutomationIdType type, ref Guid guid);
    }
}

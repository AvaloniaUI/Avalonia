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

        internal static bool IsNetComInteropAvailable
        {
            get
            {
#if NET8_0_OR_GREATER
                return true;
#else
#if NET6_0_OR_GREATER
                if (!System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported)
                {
                    return false;
                }
#endif
                var comConfig =
                    AppContext.GetData("System.Runtime.InteropServices.BuiltInComInterop.IsSupported") as string;
                return comConfig == null || bool.Parse(comConfig);
#endif
            }
        }

        internal static int UiaLookupId(AutomationIdType type, ref Guid guid)
        {
            return RawUiaLookupId(type, ref guid);
        }

#if NET7_0_OR_GREATER
        [LibraryImport("UIAutomationCore.dll", EntryPoint = "UiaLookupId", StringMarshalling = StringMarshalling.Utf8)]
        private static partial int RawUiaLookupId(AutomationIdType type, ref Guid guid);
#else
        [DllImport("UIAutomationCore.dll", EntryPoint = "UiaLookupId", CharSet = CharSet.Unicode)]
        private static extern int RawUiaLookupId(AutomationIdType type, ref Guid guid);
#endif
    }
}

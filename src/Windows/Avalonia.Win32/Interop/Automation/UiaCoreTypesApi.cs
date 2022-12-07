using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Avalonia.Win32.Interop.Automation
{
    internal static class UiaCoreTypesApi
    {
        private const string StartListeningExportName = "SynchronizedInputPattern_StartListening";

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

        private static bool? s_isNetComInteropAvailable;
        internal static bool IsNetComInteropAvailable => s_isNetComInteropAvailable ??= GetIsNetComInteropAvailable();

        internal static int UiaLookupId(AutomationIdType type, ref Guid guid)
        {   
            return RawUiaLookupId( type, ref guid );
        }

        [RequiresUnreferencedCode("Requires .NET COM interop")]
        internal static object UiaGetReservedNotSupportedValue()
        {
            object notSupportedValue;
            CheckError(RawUiaGetReservedNotSupportedValue(out notSupportedValue));
            return notSupportedValue;
        }

        [RequiresUnreferencedCode("Requires .NET COM interop")]
        internal static object UiaGetReservedMixedAttributeValue()
        {
            object mixedAttributeValue;
            CheckError(RawUiaGetReservedMixedAttributeValue(out mixedAttributeValue));
            return mixedAttributeValue;
        }

        private static void CheckError(int hr)
        {
            if (hr >= 0)
            {
                return;
            }

            Marshal.ThrowExceptionForHR(hr, (IntPtr)(-1));
        }
        
        private static bool GetIsNetComInteropAvailable()
        {
#if NET6_0_OR_GREATER
            if (!System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported)
            {
                return false;
            }
#endif

            var comConfig = AppContext.GetData("System.Runtime.InteropServices.BuiltInComInterop.IsSupported");
            return comConfig == null || bool.Parse(comConfig.ToString());
        }

        [DllImport("UIAutomationCore.dll", EntryPoint = "UiaLookupId", CharSet = CharSet.Unicode)]
        private static extern int RawUiaLookupId(AutomationIdType type, ref Guid guid);

        [DllImport("UIAutomationCore.dll", EntryPoint = "UiaGetReservedNotSupportedValue", CharSet = CharSet.Unicode)]
        private static extern int RawUiaGetReservedNotSupportedValue([MarshalAs(UnmanagedType.IUnknown)] out object notSupportedValue);

        [DllImport("UIAutomationCore.dll", EntryPoint = "UiaGetReservedMixedAttributeValue", CharSet = CharSet.Unicode)]
        private static extern int RawUiaGetReservedMixedAttributeValue([MarshalAs(UnmanagedType.IUnknown)] out object mixedAttributeValue);
    }
}

namespace Tmds.DBus.Protocol;

static class Feature
{
        public static bool IsDynamicCodeEnabled =>
#if NETSTANDARD2_0
            true
#else
            System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported
#endif
         && EnableDynamicCode;

        private static readonly bool EnableDynamicCode = Environment.GetEnvironmentVariable("TMDS_DBUS_PROTOCOL_DYNAMIC_CODE") != "0";
}
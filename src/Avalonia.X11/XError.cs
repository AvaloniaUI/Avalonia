using System;

namespace Avalonia.X11
{
    internal static class XError
    {
        private static readonly XErrorHandler s_errorHandlerDelegate = Handler;
        public static XErrorEvent LastError;

        private static int Handler(IntPtr display, ref XErrorEvent error)
        {
            LastError = error;
            return 0;
        }

        public static void ThrowLastError(string desc)
        {
            var err = LastError;
            LastError = new XErrorEvent();
            if (err.error_code == 0)
                throw new X11Exception(desc);
            throw new X11Exception(desc + ": " + err.error_code);

        }

        public static void Init()
        {
            XLib.XSetErrorHandler(s_errorHandlerDelegate);
        }
    }
}

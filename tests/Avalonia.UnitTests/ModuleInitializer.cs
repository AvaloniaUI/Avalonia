using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Avalonia.Base.UnitTests
{
    internal static class ModuleInitializer
    {
        [ModuleInitializer]
        internal static void TestInit()
        {
            Trace.Listeners.Insert(0, new ThrowListener());
        }

        private class ThrowListener : TextWriterTraceListener
        {
            public override void Fail(string message)
            {
                throw new Exception("Assertion Failed. " + message);
            }

            public override void Fail(string message, string detailMessage)
            {
                throw new Exception("Assertion Failed. " + message + detailMessage);
            }
        }
    }
}

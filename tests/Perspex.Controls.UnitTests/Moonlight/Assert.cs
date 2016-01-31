using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perspex.Controls.UnitTests.Moonlight
{
    internal static class Assert
    {
        public static void AreEqual<T>(T expected, T actual, string message = null, params object[] args)
        {
            Xunit.Assert.Equal(expected, actual);
        }

        public static void AreSame<T>(T expected, T actual, string message = null, params object[] args)
        {
            Xunit.Assert.Same(expected, actual);
        }

        public static void Fail(string message, params object[] args)
        {
            Xunit.Assert.True(false);
        }

        public static void IsBetween(double min, double max, double actual, string message = null)
        {
            if (actual > max || actual < min)
            {
                throw new Exception(string.Format("Actual value '{0}' is not between '{1}' and '{2}'). ", actual, min, max));
            }
        }

        public static void IsNull<T>(T o, string message = null) where T : class
        {
            Xunit.Assert.Null(o);
        }

        public static void IsNotNull<T>(T o, string message = null) where T : class
        {
            Xunit.Assert.NotNull(o);
        }

        public static void IsTrue(bool value, string message = null)
        {
            Xunit.Assert.True(value);
        }

        public static void IsFalse(bool value, string message = null)
        {
            Xunit.Assert.False(value);
        }

        public static void Throws<T>(Action action, string message = null) where T : Exception
        {
            Xunit.Assert.Throws<T>(action);
        }
    }
}

using System;
using System.Globalization;
using System.Threading;

namespace Avalonia.UnitTests
{
    /// <summary>
    /// Runs tests in the invariant culture.
    /// </summary>
    /// <remarks>
    /// Some tests check exception messages, and those from the .NET framework will be translated.
    /// Use this fixture to set the current culture to the invariant culture.
    /// </remarks>
    public class InvariantCultureFixture : IDisposable
    {
        private CultureInfo _restore;

        public InvariantCultureFixture()
        {
            _restore = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _restore;
        }
    }
}

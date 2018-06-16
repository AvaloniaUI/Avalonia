// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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
#if NET461
            _restore = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
#else
            _restore = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
#endif
        }

        public void Dispose()
        {
#if NET461
            Thread.CurrentThread.CurrentCulture = _restore;
#else
            CultureInfo.CurrentCulture = _restore;
#endif
        }
    }
}

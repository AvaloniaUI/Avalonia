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
            _restore = CultureInfo.CurrentUICulture;
            CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture = _restore;
        }
    }
}

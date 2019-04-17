// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia.Native.Interop;
using Avalonia.Platform;

namespace Avalonia.Native
{
    class ScreenImpl : IScreenImpl, IDisposable
    {
        private IAvnScreens _native;

        public ScreenImpl(IAvnScreens native)
        {
            _native = native;
        }

        public int ScreenCount => _native.GetScreenCount();

        public IReadOnlyList<Screen> AllScreens
        {
            get
            {
                var count = ScreenCount;
                var result = new Screen[count];

                for(int i = 0; i < count; i++)
                {
                    var screen = _native.GetScreen(i);

                    result[i] = new Screen(
                        screen.Bounds.ToAvaloniaPixelRect(),
                        screen.WorkingArea.ToAvaloniaPixelRect(),
                        screen.Primary);
                }

                return result;
            }
        }

        public void Dispose ()
        {
            _native?.Dispose();
            _native = null;
        }
    }
}

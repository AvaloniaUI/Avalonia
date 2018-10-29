// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Avalonia.Native.Interop;
using Avalonia.Platform;
using SharpGen.Runtime;

namespace Avalonia.Native
{
    public class DeferredFramebuffer : ILockedFramebuffer
    {
        private readonly Func<Action<IAvnWindowBase>, bool> _lockWindow;

        public DeferredFramebuffer(Func<Action<IAvnWindowBase>, bool> lockWindow,
                                   int width, int height, Vector dpi)
        {
            _lockWindow = lockWindow;
            Address = Marshal.AllocHGlobal(width * height * 4);
            Size = new PixelSize(width, height);
            RowBytes = width * 4;
            Dpi = dpi;
            Format = PixelFormat.Rgba8888;
        }

        public IntPtr Address { get; set; }
        public PixelSize Size { get; set; }
        public int Height { get; set; }
        public int RowBytes { get; set; }
        public Vector Dpi { get; set; }
        public PixelFormat Format { get; set; }

        class Disposer : CallbackBase
        {
            private IntPtr _ptr;

            public Disposer(IntPtr ptr)
            {
                _ptr = ptr;
            }

            protected override void Destroyed()
            {
                if(_ptr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(_ptr);
                    _ptr = IntPtr.Zero;
                }
            }
        }

        public void Dispose()
        {
            if (Address == IntPtr.Zero)
                return;

            if (!_lockWindow(win =>
            {
                var fb = new AvnFramebuffer
                {
                    Data = Address,
                    Dpi = new AvnVector
                    {
                        X = Dpi.X,
                        Y = Dpi.Y
                    },
                    Width = Size.Width,
                    Height = Size.Height,
                    PixelFormat = (AvnPixelFormat)Format,
                    Stride = RowBytes
                };

                using (var d = new Disposer(Address))
                {
                    win.ThreadSafeSetSwRenderedFrame(ref fb, d);
                }
            }))
            {
                Marshal.FreeHGlobal(Address);
            }

            Address = IntPtr.Zero;
        }
    }
}

//
// Code copy-pasted from from Mono / System.Drawing.*.cs
// Original license below:
//
// Authors: 
//  Alexandre Pigolkine (pigolkine@gmx.de)
//  Jordi Mas (jordi@ximian.com)
//	Sanjay Gupta (gsanjay@novell.com)
//	Ravindra (rkumar@novell.com)
//	Peter Dennis Bartok (pbartok@novell.com)
//	Sebastien Pouliot  <sebastien@ximian.com>
//
//
// Copyright (C) 2004, 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Win32
{
    static class Gdip
    {
        public enum Status
        {
            Ok = 0,
            GenericError = 1,
            InvalidParameter = 2,
            OutOfMemory = 3,
            ObjectBusy = 4,
            InsufficientBuffer = 5,
            NotImplemented = 6,
            Win32Error = 7,
            WrongState = 8,
            Aborted = 9,
            FileNotFound = 10,
            ValueOverflow = 11,
            AccessDenied = 12,
            UnknownImageFormat = 13,
            FontFamilyNotFound = 14,
            FontStyleNotFound = 15,
            NotTrueTypeFont = 16,
            UnsupportedGdiplusVersion = 17,
            GdiplusNotInitialized = 18,
            PropertyNotFound = 19,
            PropertyNotSupported = 20,
            ProfileNotFound = 21
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct GdiplusStartupInput
        {
            // internalted to silent compiler
            internal uint GdiplusVersion;
            internal IntPtr DebugEventCallback;
            internal int SuppressBackgroundThread;
            internal int SuppressExternalCodecs;

            internal static GdiplusStartupInput MakeGdiplusStartupInput()
            {
                GdiplusStartupInput result = new GdiplusStartupInput();
                result.GdiplusVersion = 1;
                result.DebugEventCallback = IntPtr.Zero;
                result.SuppressBackgroundThread = 0;
                result.SuppressExternalCodecs = 0;
                return result;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct GdiplusStartupOutput
        {
            internal IntPtr NotificationHook;
            internal IntPtr NotificationUnhook;

            internal static GdiplusStartupOutput MakeGdiplusStartupOutput()
            {
                GdiplusStartupOutput result = new GdiplusStartupOutput();
                result.NotificationHook = result.NotificationUnhook = IntPtr.Zero;
                return result;
            }
        }


        [DllImport("gdiplus.dll")]
        public static extern Status GdiplusStartup(ref ulong token, ref GdiplusStartupInput input, ref GdiplusStartupOutput output);

        [DllImport("gdiplus.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        public static extern Status GdipLoadImageFromStream([MarshalAs(UnmanagedType.Interface, MarshalTypeRef = typeof(IStream))] IStream stream, out IntPtr image);
        [DllImport("gdiplus.dll")]
        public static extern Status GdipCreateHICONFromBitmap(IntPtr bmp, out IntPtr HandleIcon);

        [DllImport("gdiplus.dll")]
        internal static extern Status GdipDisposeImage(IntPtr image);

        static Gdip()
        {
            ulong token = 0;
            var input = GdiplusStartupInput.MakeGdiplusStartupInput();
            var output = GdiplusStartupOutput.MakeGdiplusStartupOutput();
            GdiplusStartup(ref token, ref input, ref output);
        }
    }
}

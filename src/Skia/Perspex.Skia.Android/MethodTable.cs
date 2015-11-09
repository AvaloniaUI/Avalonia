using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Perspex.Skia
{
    class MethodTableImpl : MethodTable
    {
        [DllImport(@"perspesk")]
        private static extern IntPtr GetPerspexMethodTable();
        [DllImport(@"perspesk")]
        private static extern IntPtr PerspexJniInit(IntPtr jniEnv);

        public MethodTableImpl() : base(GetPerspexMethodTable())
        {
            PerspexJniInit(JNIEnv.Handle);
        }
    }
}
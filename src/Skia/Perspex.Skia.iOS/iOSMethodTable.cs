using System;
using System.Runtime.InteropServices;
using ObjCRuntime;

/* No longer needed with SkiaSharp

[assembly: LinkWith("Perspex.Skia.iOS.libperspesk_standalone.a", ForceLoad = true, SmartLink = true, IsCxx = true, Frameworks = "ImageIO MobileCoreServices CoreText")]

namespace Perspex.Skia
{
    class MethodTableImpl : MethodTable
    {
        [DllImport("__Internal")]
        static extern IntPtr GetPerspexMethodTable();

        public MethodTableImpl() : base(GetPerspexMethodTable())
        {
        }
    }

}

*/

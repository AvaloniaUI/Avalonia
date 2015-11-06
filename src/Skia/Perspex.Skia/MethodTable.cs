using System;
using System.Linq;
using System.Runtime.InteropServices;
using Perspex.Media;

// ReSharper disable InconsistentNaming

namespace Perspex.Skia
{
    internal unsafe abstract class MethodTable
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr _CreateRenderTarget(IntPtr nativeHandle, int width, int height);

        public _CreateRenderTarget CreateRenderTarget;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr _RenderTargetCreateRenderingContext(IntPtr target);

        public _RenderTargetCreateRenderingContext RenderTargetCreateRenderingContext;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void _RenderTargetResize(IntPtr target, int width, int height);

        public _RenderTargetResize RenderTargetResize;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void _DisposeRenderTarget(IntPtr target);

        public _DisposeRenderTarget DisposeRenderTarget;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void _DisposeRenderingContext(IntPtr ctx);

        public _DisposeRenderingContext DisposeRenderingContext;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void _DrawRectangle(IntPtr ctx, void* brush, ref SkRect rect, float borderRadius);

        public _DrawRectangle DrawRectangle;


        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void _PushClip(IntPtr ctx, ref SkRect rect);

        public _PushClip PushClip;


        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void _PopClip(IntPtr ctx);

        public _PopClip PopClip;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void _SetTransform(IntPtr ctx, float[] matrix6);

        public _SetTransform SetTransform;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void _DrawLine(IntPtr ctx, void* brush, float x1, float y1, float x2, float y2);

        public _DrawLine DrawLine;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr _CreatePath(SkiaGeometryElement[] elements, int count, out SkRect bounds);

        public _CreatePath CreatePath;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void _DisposePath(IntPtr handle);

        public _DisposePath DisposePath;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void _DrawGeometry(IntPtr ctx, IntPtr path, void* fill, void* stroke);

        public _DrawGeometry DrawGeometry;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void _DestroySkData(IntPtr handle);

        public _DestroySkData DestroySkData;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool _LoadImage(byte[] data, int len, out IntPtr image, out int width, out int height);

        public _LoadImage LoadImage;

        public enum SkiaImageType 
        {
            Png,Gif,Jpeg
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr _SaveImage(IntPtr image, SkiaImageType type, int quality);

        public _SaveImage SaveImage;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void _DrawImage(IntPtr ctx, IntPtr image, float opacity, ref SkRect srcRect, ref SkRect destRect);

        public _DrawImage DrawImage;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr _CreateRenderTargetBitmap(int width, int height);

        public _CreateRenderTargetBitmap CreateRenderTargetBitmap;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void _ResizeBitmap(IntPtr image, int width, int height);

        public _ResizeBitmap ResizeBitmap;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void _DisposeImage(IntPtr image);

        public _DisposeImage DisposeImage;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int _GetSkDataSize(IntPtr data);

        public _GetSkDataSize GetSkDataSize;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void _ReadSkData(IntPtr data, byte[] buffer, int count);

        public _ReadSkData ReadSkData;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate NativeDrawingContextSettings* _GetDrawingContextSettingsPtr(IntPtr ctx);

        public _GetDrawingContextSettingsPtr GetDrawingContextSettingsPtr;


        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr _CreateTypeface(void* name, int style);

        public _CreateTypeface CreateTypeface;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr _CreateFormattedText(void*utf16, int len, IntPtr typeface, float fontSize, TextAlignment align, NativeFormattedText** shared);

        public _CreateFormattedText CreateFormattedText;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void _RebuildFormattedText(IntPtr handle);

        public _RebuildFormattedText RebuildFormattedText;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void _DestroyFormattedText(IntPtr handle);

        public _DestroyFormattedText DestroyFormattedText;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void _DrawFormattedText(IntPtr ctx, void* brush, IntPtr text, float x, float y);

        public _DrawFormattedText DrawFormattedText;

        private static readonly Type[] TableOrder = new Type[]
        {
            typeof (_CreateRenderTarget),
            typeof (_DisposeRenderTarget),
            typeof (_RenderTargetResize),
            typeof (_RenderTargetCreateRenderingContext),
            typeof (_DisposeRenderingContext),
            typeof (_DrawRectangle),
            typeof (_PushClip),
            typeof (_PopClip),
            typeof (_SetTransform),
            typeof (_DrawLine),
            typeof (_CreatePath),
            typeof (_DisposePath),
            typeof (_DrawGeometry),
            typeof (_GetSkDataSize),
            typeof (_ReadSkData),
            typeof (_DestroySkData),
            typeof (_LoadImage),
            typeof (_SaveImage),
            typeof (_DrawImage),
            typeof (_CreateRenderTargetBitmap),
            typeof (_ResizeBitmap),
            typeof (_DisposeImage),
            typeof (_GetDrawingContextSettingsPtr),
            typeof (_CreateTypeface),
            typeof (_CreateFormattedText),
            typeof (_RebuildFormattedText),
            typeof (_DestroyFormattedText),
            typeof (_DrawFormattedText)
        };



        protected MethodTable(IntPtr methodTable)
        {
            var dic = typeof (MethodTable).GetFields().ToDictionary(f => f.FieldType, f => f);

            for (var c = 0; c < TableOrder.Length; c++)
            {
                IntPtr pMethod = Marshal.ReadIntPtr(methodTable, IntPtr.Size*c);
                var t = TableOrder[c];
                dic[t].SetValue(this, Marshal.GetDelegateForFunctionPointer(pMethod, t));
            }
        }
        
        public static readonly MethodTable Instance = new Win32MethodTable();
    }

    class Win32MethodTable : MethodTable
    {
        [DllImport(@"libperspesk.dll")]
        private static extern IntPtr GetPerspexMethodTable();

        public Win32MethodTable() : base(GetPerspexMethodTable()) { }
    }
}

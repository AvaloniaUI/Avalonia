using System;
using Avalonia.Platform;
using MonoMac.AppKit;
using System.Runtime.InteropServices;
using MonoMac.CoreGraphics;
using MonoMac.ImageIO;
using MonoMac.Foundation;

namespace Avalonia.MonoMac
{
    class EmulatedFramebuffer : ILockedFramebuffer
	{
		[DllImport("/System/Library/Frameworks/ApplicationServices.framework/Versions/A/Frameworks/CoreGraphics.framework/CoreGraphics")]
		extern static IntPtr CGBitmapContextCreate (IntPtr data, UIntPtr width, UIntPtr height, UIntPtr bitsPerComponent,
                                UIntPtr bytesPerRow, IntPtr colorSpace, uint bitmapInfo);

		public EmulatedFramebuffer(NSView view)
		{
            //TODO: Check if this is correct
            var factor = view.Window.UserSpaceScaleFactor;
			var frame = view.Frame;
            Width = (int)(frame.Width * factor);
            Height = (int)(frame.Height * factor);
			RowBytes = Width * 4;
			Dpi = new Size(96, 96) * factor;
            Format = PixelFormat.Rgba8888;
			Address = Marshal.AllocHGlobal(Height * RowBytes);
		}

		public void Dispose()
		{
			if (Address == IntPtr.Zero)
				return;
			var nfo = (int)CGBitmapFlags.ByteOrder32Big | (int)CGImageAlphaInfo.PremultipliedLast;

            using (var colorSpace = CGColorSpace.CreateDeviceRGB())
            using (var bContext = new CGBitmapContext (Address, Width, Height, 8, Width * 4,
				colorSpace, (CGImageAlphaInfo)nfo))
			using (var image = bContext.ToImage())
            using (var nscontext = NSGraphicsContext.CurrentContext)
            using(var context = nscontext.GraphicsPort)
			{
                /*
                var url = NSUrl.FromFilename("/tmp/wat.png");
                var dest = CGImageDestination.FromUrl(url, "public.png", 1);
                dest.AddImage(image, new NSDictionary());
                dest.Close();
                dest.Dispose();

                var buf = new byte[Height * RowBytes];
                Marshal.Copy(Address, buf, 0, buf.Length);
                System.IO.File.WriteAllBytes("/tmp/wat.bin", buf);
*/
                // flip the image for CGContext.DrawImage
                //context.TranslateCTM(0, Height);
                //context.ScaleCTM(1, -1);

                context.SetFillColor(255, 255, 255, 255);
                context.FillRect(new CGRect(0, 0, Width, Height));

				context.DrawImage(new CGRect(0, 0, Width, Height), image);
			}
			Marshal.FreeHGlobal(Address);
			Address = IntPtr.Zero;
		}

		public IntPtr Address { get; private set; }
		public int Width { get; }
		public int Height { get; }
		public int RowBytes { get; }
		public Size Dpi { get; }
		public PixelFormat Format { get; }
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform;

namespace Avalonia.Win32
{
    public class IconImpl : IWindowIconImpl
    {
        private readonly MemoryStream _ms;

        public IconImpl(Stream data)
        {
            _ms =  new MemoryStream();
            data.CopyTo(_ms);
            _ms.Seek(0, SeekOrigin.Begin);
            IntPtr bitmap;
            var status = Gdip.GdipLoadImageFromStream(new ComIStreamWrapper(_ms), out bitmap);
            if (status != Gdip.Status.Ok)
                throw new Exception("Unable to load icon, gdip status: " + (int) status);
            IntPtr icon;
            status = Gdip.GdipCreateHICONFromBitmap(bitmap, out icon);
            if (status != Gdip.Status.Ok)
                throw new Exception("Unable to create HICON, gdip status: " + (int)status);
            Gdip.GdipDisposeImage(bitmap);
            HIcon = icon;
        }

        public  IntPtr HIcon { get;}
        public void Save(Stream outputStream)
        {
            lock (_ms)
            {
                _ms.Seek(0, SeekOrigin.Begin);
                _ms.CopyTo(outputStream);
            }
        }
        
    }
}

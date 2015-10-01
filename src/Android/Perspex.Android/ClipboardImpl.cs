using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Perspex.Input.Platform;

namespace Perspex.Android
{
    class ClipboardImpl : IClipboard
    {
        public Task<string> GetTextAsync()
        {
            throw new NotImplementedException();
        }

        public Task SetTextAsync(string text)
        {
            throw new NotImplementedException();
        }

        public Task ClearAsync()
        {
            throw new NotImplementedException();
        }
    }
}
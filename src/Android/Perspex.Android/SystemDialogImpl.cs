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
using Perspex.Controls;
using Perspex.Controls.Platform;
using Perspex.Platform;

namespace Perspex.Android
{
    class SystemDialogImpl : ISystemDialogImpl
    {
        public Task<string[]> ShowFileDialogAsync(FileDialog dialog, IWindowImpl parent)
        {
            throw new NotImplementedException();
        }
    }
}
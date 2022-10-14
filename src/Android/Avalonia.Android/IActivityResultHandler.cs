using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;

namespace Avalonia.Android
{
    public interface IActivityResultHandler
    {
        public Action<int, Result, Intent> ActivityResult { get; set; }
    }
}

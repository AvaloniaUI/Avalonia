using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Web.Interop;
internal static partial class ShareHelper
{
    [JSImport("globalThis.navigator.share")]
    public static partial JSObject Share(JSObject data);

    [JSImport("globalThis.navigator.canShare")]
    public static partial bool CanShare(JSObject data);
}

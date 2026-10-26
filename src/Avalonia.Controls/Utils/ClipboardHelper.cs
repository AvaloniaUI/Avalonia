using System;
using System.Runtime.InteropServices;

namespace Avalonia.Controls.Utils;

internal static class ClipboardHelper
{
    public static bool IsExpectedClipboardException(Exception exception)
        => exception is TimeoutException
            or OperationCanceledException
            or UnauthorizedAccessException
            or COMException;
}

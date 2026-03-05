using System.Windows.Forms;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32.Interoperability;

/// <summary>
/// Provides a message filter for integrating Avalonia within a WinForms application.
/// </summary>
/// <remarks>
/// This filter ensures that key messages, which are typically handled specially by WinForms,
/// are intercepted and routed to Avalonia's windows. This is necessary to preserve proper input handling
/// in mixed WinForms and Avalonia application scenarios.
/// </remarks>
public class WinFormsAvaloniaMessageFilter : IMessageFilter
{
    /// <inheritdoc />
    public bool PreFilterMessage(ref Message m)
    {
        // WinForms handles key messages specially, preventing them from reaching Avalonia's windows.
        // Handle them first.
        if (m.Msg >= (int)WindowsMessage.WM_KEYFIRST &&
            m.Msg <= (int)WindowsMessage.WM_KEYLAST &&
            WindowImpl.IsOurWindowGlobal(m.HWnd))
        {
            var msg = new MSG
            {
                hwnd = m.HWnd,
                message = (uint)m.Msg,
                wParam = m.WParam,
                lParam = m.LParam
            };

            TranslateMessage(ref msg);
            DispatchMessage(ref msg);
            return true;
        }

        return false;
    }
}

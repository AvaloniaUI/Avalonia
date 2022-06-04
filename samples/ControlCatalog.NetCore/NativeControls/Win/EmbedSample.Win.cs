using System;
using System.Text;

using Avalonia.Controls.Platform;
using Avalonia.Platform;

using ControlCatalog.Pages;

namespace ControlCatalog.NetCore;

public class EmbedSampleWin : INativeDemoControl
{
    private const string RichText =
        @"{\rtf1\ansi\ansicpg1251\deff0\nouicompat\deflang1049{\fonttbl{\f0\fnil\fcharset0 Calibri;}}
{\colortbl ;\red255\green0\blue0;\red0\green77\blue187;\red0\green176\blue80;\red155\green0\blue211;\red247\green150\blue70;\red75\green172\blue198;}
{\*\generator Riched20 6.3.9600}\viewkind4\uc1 
\pard\sa200\sl276\slmult1\f0\fs22\lang9 <PREFIX>I \i am\i0  a \cf1\b Rich Text \cf0\b0\fs24 control\cf2\fs28 !\cf3\fs32 !\cf4\fs36 !\cf1\fs40 !\cf5\fs44 !\cf6\fs48 !\cf0\fs44\par
}";

    public IPlatformHandle CreateControl(bool isSecond, IPlatformHandle parent, Func<IPlatformHandle> createDefault)
    {
        WinApi.LoadLibrary("Msftedit.dll");
        var handle = WinApi.CreateWindowEx(0, "RICHEDIT50W",
            @"Rich Edit",
            0x800000 | 0x10000000 | 0x40000000 | 0x800000 | 0x10000 | 0x0004, 0, 0, 1, 1, parent.Handle,
            IntPtr.Zero, WinApi.GetModuleHandle(null), IntPtr.Zero);
        var st = new WinApi.SETTEXTEX { Codepage = 65001, Flags = 0x00000008 };
        var text = RichText.Replace("<PREFIX>", isSecond ? "\\qr " : "");
        var bytes = Encoding.UTF8.GetBytes(text);
        WinApi.SendMessage(handle, 0x0400 + 97, ref st, bytes);
        return new Win32WindowControlHandle(handle, "HWND");
    }
}

internal class Win32WindowControlHandle : PlatformHandle, INativeControlHostDestroyableControlHandle
{
    public Win32WindowControlHandle(IntPtr handle, string descriptor) : base(handle, descriptor)
    {
    }

    public void Destroy()
    {
        _ = WinApi.DestroyWindow(Handle);
    }
}

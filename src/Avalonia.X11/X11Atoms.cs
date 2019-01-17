// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
// Copyright (c) 2006 Novell, Inc. (http://www.novell.com)
//
//

using System;
using System.Linq;
using static Avalonia.X11.XLib;
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable IdentifierTypo
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable CommentTypo
// ReSharper disable ArrangeThisQualifier
// ReSharper disable NotAccessedField.Global
// ReSharper disable InconsistentNaming
// ReSharper disable StringLiteralTypo
#pragma warning disable 649

namespace Avalonia.X11
{

    internal class X11Atoms
    {

// Our atoms
        public readonly IntPtr AnyPropertyType = (IntPtr)0;
        public readonly IntPtr XA_PRIMARY = (IntPtr)1;
        public readonly IntPtr XA_SECONDARY = (IntPtr)2;
        public readonly IntPtr XA_ARC = (IntPtr)3;
        public readonly IntPtr XA_ATOM = (IntPtr)4;
        public readonly IntPtr XA_BITMAP = (IntPtr)5;
        public readonly IntPtr XA_CARDINAL = (IntPtr)6;
        public readonly IntPtr XA_COLORMAP = (IntPtr)7;
        public readonly IntPtr XA_CURSOR = (IntPtr)8;
        public readonly IntPtr XA_CUT_BUFFER0 = (IntPtr)9;
        public readonly IntPtr XA_CUT_BUFFER1 = (IntPtr)10;
        public readonly IntPtr XA_CUT_BUFFER2 = (IntPtr)11;
        public readonly IntPtr XA_CUT_BUFFER3 = (IntPtr)12;
        public readonly IntPtr XA_CUT_BUFFER4 = (IntPtr)13;
        public readonly IntPtr XA_CUT_BUFFER5 = (IntPtr)14;
        public readonly IntPtr XA_CUT_BUFFER6 = (IntPtr)15;
        public readonly IntPtr XA_CUT_BUFFER7 = (IntPtr)16;
        public readonly IntPtr XA_DRAWABLE = (IntPtr)17;
        public readonly IntPtr XA_FONT = (IntPtr)18;
        public readonly IntPtr XA_INTEGER = (IntPtr)19;
        public readonly IntPtr XA_PIXMAP = (IntPtr)20;
        public readonly IntPtr XA_POINT = (IntPtr)21;
        public readonly IntPtr XA_RECTANGLE = (IntPtr)22;
        public readonly IntPtr XA_RESOURCE_MANAGER = (IntPtr)23;
        public readonly IntPtr XA_RGB_COLOR_MAP = (IntPtr)24;
        public readonly IntPtr XA_RGB_BEST_MAP = (IntPtr)25;
        public readonly IntPtr XA_RGB_BLUE_MAP = (IntPtr)26;
        public readonly IntPtr XA_RGB_DEFAULT_MAP = (IntPtr)27;
        public readonly IntPtr XA_RGB_GRAY_MAP = (IntPtr)28;
        public readonly IntPtr XA_RGB_GREEN_MAP = (IntPtr)29;
        public readonly IntPtr XA_RGB_RED_MAP = (IntPtr)30;
        public readonly IntPtr XA_STRING = (IntPtr)31;
        public readonly IntPtr XA_VISUALID = (IntPtr)32;
        public readonly IntPtr XA_WINDOW = (IntPtr)33;
        public readonly IntPtr XA_WM_COMMAND = (IntPtr)34;
        public readonly IntPtr XA_WM_HINTS = (IntPtr)35;
        public readonly IntPtr XA_WM_CLIENT_MACHINE = (IntPtr)36;
        public readonly IntPtr XA_WM_ICON_NAME = (IntPtr)37;
        public readonly IntPtr XA_WM_ICON_SIZE = (IntPtr)38;
        public readonly IntPtr XA_WM_NAME = (IntPtr)39;
        public readonly IntPtr XA_WM_NORMAL_HINTS = (IntPtr)40;
        public readonly IntPtr XA_WM_SIZE_HINTS = (IntPtr)41;
        public readonly IntPtr XA_WM_ZOOM_HINTS = (IntPtr)42;
        public readonly IntPtr XA_MIN_SPACE = (IntPtr)43;
        public readonly IntPtr XA_NORM_SPACE = (IntPtr)44;
        public readonly IntPtr XA_MAX_SPACE = (IntPtr)45;
        public readonly IntPtr XA_END_SPACE = (IntPtr)46;
        public readonly IntPtr XA_SUPERSCRIPT_X = (IntPtr)47;
        public readonly IntPtr XA_SUPERSCRIPT_Y = (IntPtr)48;
        public readonly IntPtr XA_SUBSCRIPT_X = (IntPtr)49;
        public readonly IntPtr XA_SUBSCRIPT_Y = (IntPtr)50;
        public readonly IntPtr XA_UNDERLINE_POSITION = (IntPtr)51;
        public readonly IntPtr XA_UNDERLINE_THICKNESS = (IntPtr)52;
        public readonly IntPtr XA_STRIKEOUT_ASCENT = (IntPtr)53;
        public readonly IntPtr XA_STRIKEOUT_DESCENT = (IntPtr)54;
        public readonly IntPtr XA_ITALIC_ANGLE = (IntPtr)55;
        public readonly IntPtr XA_X_HEIGHT = (IntPtr)56;
        public readonly IntPtr XA_QUAD_WIDTH = (IntPtr)57;
        public readonly IntPtr XA_WEIGHT = (IntPtr)58;
        public readonly IntPtr XA_POINT_SIZE = (IntPtr)59;
        public readonly IntPtr XA_RESOLUTION = (IntPtr)60;
        public readonly IntPtr XA_COPYRIGHT = (IntPtr)61;
        public readonly IntPtr XA_NOTICE = (IntPtr)62;
        public readonly IntPtr XA_FONT_NAME = (IntPtr)63;
        public readonly IntPtr XA_FAMILY_NAME = (IntPtr)64;
        public readonly IntPtr XA_FULL_NAME = (IntPtr)65;
        public readonly IntPtr XA_CAP_HEIGHT = (IntPtr)66;
        public readonly IntPtr XA_WM_CLASS = (IntPtr)67;
        public readonly IntPtr XA_WM_TRANSIENT_FOR = (IntPtr)68;

        public readonly IntPtr WM_PROTOCOLS;
        public readonly IntPtr WM_DELETE_WINDOW;
        public readonly IntPtr WM_TAKE_FOCUS;
        public readonly IntPtr _NET_SUPPORTED;
        public readonly IntPtr _NET_CLIENT_LIST;
        public readonly IntPtr _NET_NUMBER_OF_DESKTOPS;
        public readonly IntPtr _NET_DESKTOP_GEOMETRY;
        public readonly IntPtr _NET_DESKTOP_VIEWPORT;
        public readonly IntPtr _NET_CURRENT_DESKTOP;
        public readonly IntPtr _NET_DESKTOP_NAMES;
        public readonly IntPtr _NET_ACTIVE_WINDOW;
        public readonly IntPtr _NET_WORKAREA;
        public readonly IntPtr _NET_SUPPORTING_WM_CHECK;
        public readonly IntPtr _NET_VIRTUAL_ROOTS;
        public readonly IntPtr _NET_DESKTOP_LAYOUT;
        public readonly IntPtr _NET_SHOWING_DESKTOP;
        public readonly IntPtr _NET_CLOSE_WINDOW;
        public readonly IntPtr _NET_MOVERESIZE_WINDOW;
        public readonly IntPtr _NET_WM_MOVERESIZE;
        public readonly IntPtr _NET_RESTACK_WINDOW;
        public readonly IntPtr _NET_REQUEST_FRAME_EXTENTS;
        public readonly IntPtr _NET_WM_NAME;
        public readonly IntPtr _NET_WM_VISIBLE_NAME;
        public readonly IntPtr _NET_WM_ICON_NAME;
        public readonly IntPtr _NET_WM_VISIBLE_ICON_NAME;
        public readonly IntPtr _NET_WM_DESKTOP;
        public readonly IntPtr _NET_WM_WINDOW_TYPE;
        public readonly IntPtr _NET_WM_STATE;
        public readonly IntPtr _NET_WM_ALLOWED_ACTIONS;
        public readonly IntPtr _NET_WM_STRUT;
        public readonly IntPtr _NET_WM_STRUT_PARTIAL;
        public readonly IntPtr _NET_WM_ICON_GEOMETRY;
        public readonly IntPtr _NET_WM_ICON;
        public readonly IntPtr _NET_WM_PID;
        public readonly IntPtr _NET_WM_HANDLED_ICONS;
        public readonly IntPtr _NET_WM_USER_TIME;
        public readonly IntPtr _NET_FRAME_EXTENTS;
        public readonly IntPtr _NET_WM_PING;
        public readonly IntPtr _NET_WM_SYNC_REQUEST;
        public readonly IntPtr _NET_SYSTEM_TRAY_S;
        public readonly IntPtr _NET_SYSTEM_TRAY_ORIENTATION;
        public readonly IntPtr _NET_SYSTEM_TRAY_OPCODE;
        public readonly IntPtr _NET_WM_STATE_MAXIMIZED_HORZ;
        public readonly IntPtr _NET_WM_STATE_MAXIMIZED_VERT;
        public readonly IntPtr _XEMBED;
        public readonly IntPtr _XEMBED_INFO;
        public readonly IntPtr _MOTIF_WM_HINTS;
        public readonly IntPtr _NET_WM_STATE_SKIP_TASKBAR;
        public readonly IntPtr _NET_WM_STATE_ABOVE;
        public readonly IntPtr _NET_WM_STATE_MODAL;
        public readonly IntPtr _NET_WM_STATE_HIDDEN;
        public readonly IntPtr _NET_WM_CONTEXT_HELP;
        public readonly IntPtr _NET_WM_WINDOW_OPACITY;
        public readonly IntPtr _NET_WM_WINDOW_TYPE_DESKTOP;
        public readonly IntPtr _NET_WM_WINDOW_TYPE_DOCK;
        public readonly IntPtr _NET_WM_WINDOW_TYPE_TOOLBAR;
        public readonly IntPtr _NET_WM_WINDOW_TYPE_MENU;
        public readonly IntPtr _NET_WM_WINDOW_TYPE_UTILITY;
        public readonly IntPtr _NET_WM_WINDOW_TYPE_SPLASH;
        public readonly IntPtr _NET_WM_WINDOW_TYPE_DIALOG;
        public readonly IntPtr _NET_WM_WINDOW_TYPE_NORMAL;
        public readonly IntPtr CLIPBOARD;
        public readonly IntPtr CLIPBOARD_MANAGER;
        public readonly IntPtr SAVE_TARGETS;
        public readonly IntPtr MULTIPLE;
        public readonly IntPtr PRIMARY;
        public readonly IntPtr OEMTEXT;
        public readonly IntPtr UNICODETEXT;
        public readonly IntPtr TARGETS;
        public readonly IntPtr UTF8_STRING;
        public readonly IntPtr UTF16_STRING;
        public readonly IntPtr ATOM_PAIR;


        public X11Atoms(IntPtr display)
        {

            // make sure this array stays in sync with the statements below

            var fields = typeof(X11Atoms).GetFields()
                .Where(f => f.FieldType == typeof(IntPtr) && (IntPtr)f.GetValue(this) == IntPtr.Zero).ToArray();
            var atomNames = fields.Select(f => f.Name).ToArray();

            IntPtr[] atoms = new IntPtr [atomNames.Length];
            ;

            XInternAtoms(display, atomNames, atomNames.Length, true, atoms);

            for (var c = 0; c < fields.Length; c++)
                fields[c].SetValue(this, atoms[c]);
        }
    }
}

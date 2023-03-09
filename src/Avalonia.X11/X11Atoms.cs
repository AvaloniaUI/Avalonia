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
// Copyright (c) 2006 Novell, Inc. (https://www.novell.com)
//
//

using System;
using System.Collections.Generic;
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

    internal partial class X11Atoms
    {
        private readonly IntPtr _display;

        // Our atoms
        public IntPtr AnyPropertyType = (IntPtr)0;
        public IntPtr XA_PRIMARY = (IntPtr)1;
        public IntPtr XA_SECONDARY = (IntPtr)2;
        public IntPtr XA_ARC = (IntPtr)3;
        public IntPtr XA_ATOM = (IntPtr)4;
        public IntPtr XA_BITMAP = (IntPtr)5;
        public IntPtr XA_CARDINAL = (IntPtr)6;
        public IntPtr XA_COLORMAP = (IntPtr)7;
        public IntPtr XA_CURSOR = (IntPtr)8;
        public IntPtr XA_CUT_BUFFER0 = (IntPtr)9;
        public IntPtr XA_CUT_BUFFER1 = (IntPtr)10;
        public IntPtr XA_CUT_BUFFER2 = (IntPtr)11;
        public IntPtr XA_CUT_BUFFER3 = (IntPtr)12;
        public IntPtr XA_CUT_BUFFER4 = (IntPtr)13;
        public IntPtr XA_CUT_BUFFER5 = (IntPtr)14;
        public IntPtr XA_CUT_BUFFER6 = (IntPtr)15;
        public IntPtr XA_CUT_BUFFER7 = (IntPtr)16;
        public IntPtr XA_DRAWABLE = (IntPtr)17;
        public IntPtr XA_FONT = (IntPtr)18;
        public IntPtr XA_INTEGER = (IntPtr)19;
        public IntPtr XA_PIXMAP = (IntPtr)20;
        public IntPtr XA_POINT = (IntPtr)21;
        public IntPtr XA_RECTANGLE = (IntPtr)22;
        public IntPtr XA_RESOURCE_MANAGER = (IntPtr)23;
        public IntPtr XA_RGB_COLOR_MAP = (IntPtr)24;
        public IntPtr XA_RGB_BEST_MAP = (IntPtr)25;
        public IntPtr XA_RGB_BLUE_MAP = (IntPtr)26;
        public IntPtr XA_RGB_DEFAULT_MAP = (IntPtr)27;
        public IntPtr XA_RGB_GRAY_MAP = (IntPtr)28;
        public IntPtr XA_RGB_GREEN_MAP = (IntPtr)29;
        public IntPtr XA_RGB_RED_MAP = (IntPtr)30;
        public IntPtr XA_STRING = (IntPtr)31;
        public IntPtr XA_VISUALID = (IntPtr)32;
        public IntPtr XA_WINDOW = (IntPtr)33;
        public IntPtr XA_WM_COMMAND = (IntPtr)34;
        public IntPtr XA_WM_HINTS = (IntPtr)35;
        public IntPtr XA_WM_CLIENT_MACHINE = (IntPtr)36;
        public IntPtr XA_WM_ICON_NAME = (IntPtr)37;
        public IntPtr XA_WM_ICON_SIZE = (IntPtr)38;
        public IntPtr XA_WM_NAME = (IntPtr)39;
        public IntPtr XA_WM_NORMAL_HINTS = (IntPtr)40;
        public IntPtr XA_WM_SIZE_HINTS = (IntPtr)41;
        public IntPtr XA_WM_ZOOM_HINTS = (IntPtr)42;
        public IntPtr XA_MIN_SPACE = (IntPtr)43;
        public IntPtr XA_NORM_SPACE = (IntPtr)44;
        public IntPtr XA_MAX_SPACE = (IntPtr)45;
        public IntPtr XA_END_SPACE = (IntPtr)46;
        public IntPtr XA_SUPERSCRIPT_X = (IntPtr)47;
        public IntPtr XA_SUPERSCRIPT_Y = (IntPtr)48;
        public IntPtr XA_SUBSCRIPT_X = (IntPtr)49;
        public IntPtr XA_SUBSCRIPT_Y = (IntPtr)50;
        public IntPtr XA_UNDERLINE_POSITION = (IntPtr)51;
        public IntPtr XA_UNDERLINE_THICKNESS = (IntPtr)52;
        public IntPtr XA_STRIKEOUT_ASCENT = (IntPtr)53;
        public IntPtr XA_STRIKEOUT_DESCENT = (IntPtr)54;
        public IntPtr XA_ITALIC_ANGLE = (IntPtr)55;
        public IntPtr XA_X_HEIGHT = (IntPtr)56;
        public IntPtr XA_QUAD_WIDTH = (IntPtr)57;
        public IntPtr XA_WEIGHT = (IntPtr)58;
        public IntPtr XA_POINT_SIZE = (IntPtr)59;
        public IntPtr XA_RESOLUTION = (IntPtr)60;
        public IntPtr XA_COPYRIGHT = (IntPtr)61;
        public IntPtr XA_NOTICE = (IntPtr)62;
        public IntPtr XA_FONT_NAME = (IntPtr)63;
        public IntPtr XA_FAMILY_NAME = (IntPtr)64;
        public IntPtr XA_FULL_NAME = (IntPtr)65;
        public IntPtr XA_CAP_HEIGHT = (IntPtr)66;
        public IntPtr XA_WM_CLASS = (IntPtr)67;
        public IntPtr XA_WM_TRANSIENT_FOR = (IntPtr)68;

        public IntPtr EDID;

        public IntPtr WM_PROTOCOLS;
        public IntPtr WM_DELETE_WINDOW;
        public IntPtr WM_TAKE_FOCUS;
        public IntPtr _NET_SUPPORTED;
        public IntPtr _NET_CLIENT_LIST;
        public IntPtr _NET_NUMBER_OF_DESKTOPS;
        public IntPtr _NET_DESKTOP_GEOMETRY;
        public IntPtr _NET_DESKTOP_VIEWPORT;
        public IntPtr _NET_CURRENT_DESKTOP;
        public IntPtr _NET_DESKTOP_NAMES;
        public IntPtr _NET_ACTIVE_WINDOW;
        public IntPtr _NET_WORKAREA;
        public IntPtr _NET_SUPPORTING_WM_CHECK;
        public IntPtr _NET_VIRTUAL_ROOTS;
        public IntPtr _NET_DESKTOP_LAYOUT;
        public IntPtr _NET_SHOWING_DESKTOP;
        public IntPtr _NET_CLOSE_WINDOW;
        public IntPtr _NET_MOVERESIZE_WINDOW;
        public IntPtr _NET_WM_MOVERESIZE;
        public IntPtr _NET_RESTACK_WINDOW;
        public IntPtr _NET_REQUEST_FRAME_EXTENTS;
        public IntPtr _NET_WM_NAME;
        public IntPtr _NET_WM_VISIBLE_NAME;
        public IntPtr _NET_WM_ICON_NAME;
        public IntPtr _NET_WM_VISIBLE_ICON_NAME;
        public IntPtr _NET_WM_DESKTOP;
        public IntPtr _NET_WM_WINDOW_TYPE;
        public IntPtr _NET_WM_STATE;
        public IntPtr _NET_WM_ALLOWED_ACTIONS;
        public IntPtr _NET_WM_STRUT;
        public IntPtr _NET_WM_STRUT_PARTIAL;
        public IntPtr _NET_WM_ICON_GEOMETRY;
        public IntPtr _NET_WM_ICON;
        public IntPtr _NET_WM_PID;
        public IntPtr _NET_WM_HANDLED_ICONS;
        public IntPtr _NET_WM_USER_TIME;
        public IntPtr _NET_FRAME_EXTENTS;
        public IntPtr _NET_WM_PING;
        public IntPtr _NET_WM_SYNC_REQUEST;
        public IntPtr _NET_WM_SYNC_REQUEST_COUNTER;
        public IntPtr _NET_SYSTEM_TRAY_S;
        public IntPtr _NET_SYSTEM_TRAY_ORIENTATION;
        public IntPtr _NET_SYSTEM_TRAY_OPCODE;
        public IntPtr _NET_WM_STATE_MAXIMIZED_HORZ;
        public IntPtr _NET_WM_STATE_MAXIMIZED_VERT;
        public IntPtr _NET_WM_STATE_FULLSCREEN;
        public IntPtr _XEMBED;
        public IntPtr _XEMBED_INFO;
        public IntPtr _MOTIF_WM_HINTS;
        public IntPtr _NET_WM_STATE_SKIP_TASKBAR;
        public IntPtr _NET_WM_STATE_ABOVE;
        public IntPtr _NET_WM_STATE_MODAL;
        public IntPtr _NET_WM_STATE_HIDDEN;
        public IntPtr _NET_WM_CONTEXT_HELP;
        public IntPtr _NET_WM_WINDOW_OPACITY;
        public IntPtr _NET_WM_WINDOW_TYPE_DESKTOP;
        public IntPtr _NET_WM_WINDOW_TYPE_DOCK;
        public IntPtr _NET_WM_WINDOW_TYPE_TOOLBAR;
        public IntPtr _NET_WM_WINDOW_TYPE_MENU;
        public IntPtr _NET_WM_WINDOW_TYPE_UTILITY;
        public IntPtr _NET_WM_WINDOW_TYPE_SPLASH;
        public IntPtr _NET_WM_WINDOW_TYPE_DIALOG;
        public IntPtr _NET_WM_WINDOW_TYPE_NORMAL;
        public IntPtr CLIPBOARD;
        public IntPtr CLIPBOARD_MANAGER;
        public IntPtr SAVE_TARGETS;
        public IntPtr MULTIPLE;
        public IntPtr PRIMARY;
        public IntPtr OEMTEXT;
        public IntPtr UNICODETEXT;
        public IntPtr TARGETS;
        public IntPtr UTF8_STRING;
        public IntPtr UTF16_STRING;
        public IntPtr ATOM_PAIR;
        public IntPtr MANAGER;
        public IntPtr _KDE_NET_WM_BLUR_BEHIND_REGION;
        public IntPtr INCR;

        private readonly Dictionary<string, IntPtr> _namesToAtoms  = new Dictionary<string, IntPtr>();
        private readonly Dictionary<IntPtr, string> _atomsToNames = new Dictionary<IntPtr, string>();
        public X11Atoms(IntPtr display)
        {
            _display = display;
            PopulateAtoms(display);
        }

        private void InitAtom(ref IntPtr field, string name, IntPtr value)
        {
            if (value != IntPtr.Zero)
            {
                field = value;
                _namesToAtoms[name] = value;
                _atomsToNames[value] = name;
            }
        }

        public IntPtr GetAtom(string name)
        {
            if (_namesToAtoms.TryGetValue(name, out var rv))
                return rv;
            var atom = XInternAtom(_display, name, false);
            _namesToAtoms[name] = atom;
            _atomsToNames[atom] = name;
            return atom;
        }

        public string GetAtomName(IntPtr atom)
        {
            if (_atomsToNames.TryGetValue(atom, out var rv))
                return rv;
            var name = XLib.GetAtomName(_display, atom);
            if (name == null)
                return null;
            _atomsToNames[atom] = name;
            _namesToAtoms[name] = atom;
            return name;
        }
    }
}

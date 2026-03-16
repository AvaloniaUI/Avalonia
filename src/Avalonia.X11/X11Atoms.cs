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
        public readonly IntPtr PRIMARY = (IntPtr)1;
        public readonly IntPtr SECONDARY = (IntPtr)2;
        public readonly IntPtr ARC = (IntPtr)3;
        public readonly IntPtr ATOM = (IntPtr)4;
        public readonly IntPtr BITMAP = (IntPtr)5;
        public readonly IntPtr CARDINAL = (IntPtr)6;
        public readonly IntPtr COLORMAP = (IntPtr)7;
        public readonly IntPtr CURSOR = (IntPtr)8;
        public readonly IntPtr CUT_BUFFER0 = (IntPtr)9;
        public readonly IntPtr CUT_BUFFER1 = (IntPtr)10;
        public readonly IntPtr CUT_BUFFER2 = (IntPtr)11;
        public readonly IntPtr CUT_BUFFER3 = (IntPtr)12;
        public readonly IntPtr CUT_BUFFER4 = (IntPtr)13;
        public readonly IntPtr CUT_BUFFER5 = (IntPtr)14;
        public readonly IntPtr CUT_BUFFER6 = (IntPtr)15;
        public readonly IntPtr CUT_BUFFER7 = (IntPtr)16;
        public readonly IntPtr DRAWABLE = (IntPtr)17;
        public readonly IntPtr FONT = (IntPtr)18;
        public readonly IntPtr INTEGER = (IntPtr)19;
        public readonly IntPtr PIXMAP = (IntPtr)20;
        public readonly IntPtr POINT = (IntPtr)21;
        public readonly IntPtr RECTANGLE = (IntPtr)22;
        public readonly IntPtr RESOURCE_MANAGER = (IntPtr)23;
        public readonly IntPtr RGB_COLOR_MAP = (IntPtr)24;
        public readonly IntPtr RGB_BEST_MAP = (IntPtr)25;
        public readonly IntPtr RGB_BLUE_MAP = (IntPtr)26;
        public readonly IntPtr RGB_DEFAULT_MAP = (IntPtr)27;
        public readonly IntPtr RGB_GRAY_MAP = (IntPtr)28;
        public readonly IntPtr RGB_GREEN_MAP = (IntPtr)29;
        public readonly IntPtr RGB_RED_MAP = (IntPtr)30;
        public readonly IntPtr STRING = (IntPtr)31;
        public readonly IntPtr VISUALID = (IntPtr)32;
        public readonly IntPtr WINDOW = (IntPtr)33;
        public readonly IntPtr WM_COMMAND = (IntPtr)34;
        public readonly IntPtr WM_HINTS = (IntPtr)35;
        public readonly IntPtr WM_CLIENT_MACHINE = (IntPtr)36;
        public readonly IntPtr WM_ICON_NAME = (IntPtr)37;
        public readonly IntPtr WM_ICON_SIZE = (IntPtr)38;
        public readonly IntPtr WM_NAME = (IntPtr)39;
        public readonly IntPtr WM_NORMAL_HINTS = (IntPtr)40;
        public readonly IntPtr WM_SIZE_HINTS = (IntPtr)41;
        public readonly IntPtr WM_ZOOM_HINTS = (IntPtr)42;
        public readonly IntPtr MIN_SPACE = (IntPtr)43;
        public readonly IntPtr NORM_SPACE = (IntPtr)44;
        public readonly IntPtr MAX_SPACE = (IntPtr)45;
        public readonly IntPtr END_SPACE = (IntPtr)46;
        public readonly IntPtr SUPERSCRIPT_X = (IntPtr)47;
        public readonly IntPtr SUPERSCRIPT_Y = (IntPtr)48;
        public readonly IntPtr SUBSCRIPT_X = (IntPtr)49;
        public readonly IntPtr SUBSCRIPT_Y = (IntPtr)50;
        public readonly IntPtr UNDERLINE_POSITION = (IntPtr)51;
        public readonly IntPtr UNDERLINE_THICKNESS = (IntPtr)52;
        public readonly IntPtr STRIKEOUT_ASCENT = (IntPtr)53;
        public readonly IntPtr STRIKEOUT_DESCENT = (IntPtr)54;
        public readonly IntPtr ITALIC_ANGLE = (IntPtr)55;
        public readonly IntPtr X_HEIGHT = (IntPtr)56;
        public readonly IntPtr QUAD_WIDTH = (IntPtr)57;
        public readonly IntPtr WEIGHT = (IntPtr)58;
        public readonly IntPtr POINT_SIZE = (IntPtr)59;
        public readonly IntPtr RESOLUTION = (IntPtr)60;
        public readonly IntPtr COPYRIGHT = (IntPtr)61;
        public readonly IntPtr NOTICE = (IntPtr)62;
        public readonly IntPtr FONT_NAME = (IntPtr)63;
        public readonly IntPtr FAMILY_NAME = (IntPtr)64;
        public readonly IntPtr FULL_NAME = (IntPtr)65;
        public readonly IntPtr CAP_HEIGHT = (IntPtr)66;
        public readonly IntPtr WM_CLASS = (IntPtr)67;
        public readonly IntPtr WM_TRANSIENT_FOR = (IntPtr)68;

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
        public IntPtr OEMTEXT;
        public IntPtr UNICODETEXT;
        public IntPtr TARGETS;
        public IntPtr UTF8_STRING;
        public IntPtr UTF16_STRING;
        public IntPtr ATOM_PAIR;
        public IntPtr MANAGER;
        public IntPtr _KDE_NET_WM_BLUR_BEHIND_REGION;
        public IntPtr INCR;
        public IntPtr _NET_WM_STATE_FOCUSED;

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
                SetName(name, value);
            }
        }

        private void SetName(string name, IntPtr value)
        {
            _namesToAtoms[name] = value;
            _atomsToNames[value] = name;
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

        public string? GetAtomName(IntPtr atom)
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

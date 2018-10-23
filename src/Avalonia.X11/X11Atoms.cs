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

namespace Avalonia.X11 {

	internal class X11Atoms {

		// Our atoms
		public readonly IntPtr AnyPropertyType		= (IntPtr)0;
		public readonly IntPtr XA_PRIMARY		= (IntPtr)1;
		public readonly IntPtr XA_SECONDARY		= (IntPtr)2;
		public readonly IntPtr XA_ARC			= (IntPtr)3;
		public readonly IntPtr XA_ATOM			= (IntPtr)4;
		public readonly IntPtr XA_BITMAP		= (IntPtr)5;
		public readonly IntPtr XA_CARDINAL		= (IntPtr)6;
		public readonly IntPtr XA_COLORMAP		= (IntPtr)7;
		public readonly IntPtr XA_CURSOR		= (IntPtr)8;
		public readonly IntPtr XA_CUT_BUFFER0		= (IntPtr)9;
		public readonly IntPtr XA_CUT_BUFFER1		= (IntPtr)10;
		public readonly IntPtr XA_CUT_BUFFER2		= (IntPtr)11;
		public readonly IntPtr XA_CUT_BUFFER3		= (IntPtr)12;
		public readonly IntPtr XA_CUT_BUFFER4		= (IntPtr)13;
		public readonly IntPtr XA_CUT_BUFFER5		= (IntPtr)14;
		public readonly IntPtr XA_CUT_BUFFER6		= (IntPtr)15;
		public readonly IntPtr XA_CUT_BUFFER7		= (IntPtr)16;
		public readonly IntPtr XA_DRAWABLE		= (IntPtr)17;
		public readonly IntPtr XA_FONT			= (IntPtr)18;
		public readonly IntPtr XA_INTEGER		= (IntPtr)19;
		public readonly IntPtr XA_PIXMAP		= (IntPtr)20;
		public readonly IntPtr XA_POINT			= (IntPtr)21;
		public readonly IntPtr XA_RECTANGLE		= (IntPtr)22;
		public readonly IntPtr XA_RESOURCE_MANAGER	= (IntPtr)23;
		public readonly IntPtr XA_RGB_COLOR_MAP		= (IntPtr)24;
		public readonly IntPtr XA_RGB_BEST_MAP		= (IntPtr)25;
		public readonly IntPtr XA_RGB_BLUE_MAP		= (IntPtr)26;
		public readonly IntPtr XA_RGB_DEFAULT_MAP	= (IntPtr)27;
		public readonly IntPtr XA_RGB_GRAY_MAP		= (IntPtr)28;
		public readonly IntPtr XA_RGB_GREEN_MAP		= (IntPtr)29;
		public readonly IntPtr XA_RGB_RED_MAP		= (IntPtr)30;
		public readonly IntPtr XA_STRING		= (IntPtr)31;
		public readonly IntPtr XA_VISUALID		= (IntPtr)32;
		public readonly IntPtr XA_WINDOW		= (IntPtr)33;
		public readonly IntPtr XA_WM_COMMAND		= (IntPtr)34;
		public readonly IntPtr XA_WM_HINTS		= (IntPtr)35;
		public readonly IntPtr XA_WM_CLIENT_MACHINE	= (IntPtr)36;
		public readonly IntPtr XA_WM_ICON_NAME		= (IntPtr)37;
		public readonly IntPtr XA_WM_ICON_SIZE		= (IntPtr)38;
		public readonly IntPtr XA_WM_NAME		= (IntPtr)39;
		public readonly IntPtr XA_WM_NORMAL_HINTS	= (IntPtr)40;
		public readonly IntPtr XA_WM_SIZE_HINTS		= (IntPtr)41;
		public readonly IntPtr XA_WM_ZOOM_HINTS		= (IntPtr)42;
		public readonly IntPtr XA_MIN_SPACE		= (IntPtr)43;
		public readonly IntPtr XA_NORM_SPACE		= (IntPtr)44;
		public readonly IntPtr XA_MAX_SPACE		= (IntPtr)45;
		public readonly IntPtr XA_END_SPACE		= (IntPtr)46;
		public readonly IntPtr XA_SUPERSCRIPT_X		= (IntPtr)47;
		public readonly IntPtr XA_SUPERSCRIPT_Y		= (IntPtr)48;
		public readonly IntPtr XA_SUBSCRIPT_X		= (IntPtr)49;
		public readonly IntPtr XA_SUBSCRIPT_Y		= (IntPtr)50;
		public readonly IntPtr XA_UNDERLINE_POSITION	= (IntPtr)51;
		public readonly IntPtr XA_UNDERLINE_THICKNESS	= (IntPtr)52;
		public readonly IntPtr XA_STRIKEOUT_ASCENT	= (IntPtr)53;
		public readonly IntPtr XA_STRIKEOUT_DESCENT	= (IntPtr)54;
		public readonly IntPtr XA_ITALIC_ANGLE		= (IntPtr)55;
		public readonly IntPtr XA_X_HEIGHT		= (IntPtr)56;
		public readonly IntPtr XA_QUAD_WIDTH		= (IntPtr)57;
		public readonly IntPtr XA_WEIGHT		= (IntPtr)58;
		public readonly IntPtr XA_POINT_SIZE		= (IntPtr)59;
		public readonly IntPtr XA_RESOLUTION		= (IntPtr)60;
		public readonly IntPtr XA_COPYRIGHT		= (IntPtr)61;
		public readonly IntPtr XA_NOTICE		= (IntPtr)62;
		public readonly IntPtr XA_FONT_NAME		= (IntPtr)63;
		public readonly IntPtr XA_FAMILY_NAME		= (IntPtr)64;
		public readonly IntPtr XA_FULL_NAME		= (IntPtr)65;
		public readonly IntPtr XA_CAP_HEIGHT		= (IntPtr)66;
		public readonly IntPtr XA_WM_CLASS		= (IntPtr)67;
		public readonly IntPtr XA_WM_TRANSIENT_FOR	= (IntPtr)68;

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
		public readonly IntPtr PRIMARY;
		public readonly IntPtr DIB;
		public readonly IntPtr OEMTEXT;
		public readonly IntPtr UNICODETEXT;
		public readonly IntPtr TARGETS;
		public readonly IntPtr PostAtom;
		public readonly IntPtr HoverState;
		public readonly IntPtr AsyncAtom;


		public X11Atoms (IntPtr display) {

			// make sure this array stays in sync with the statements below
			string [] atom_names = new string[] {
				"WM_PROTOCOLS",
				"WM_DELETE_WINDOW",
				"WM_TAKE_FOCUS",
				"_NET_SUPPORTED",
				"_NET_CLIENT_LIST",
				"_NET_NUMBER_OF_DESKTOPS",
				"_NET_DESKTOP_GEOMETRY",
				"_NET_DESKTOP_VIEWPORT",
				"_NET_CURRENT_DESKTOP",
				"_NET_DESKTOP_NAMES",
				"_NET_ACTIVE_WINDOW",
				"_NET_WORKAREA",
				"_NET_SUPPORTING_WM_CHECK",
				"_NET_VIRTUAL_ROOTS",
				"_NET_DESKTOP_LAYOUT",
				"_NET_SHOWING_DESKTOP",
				"_NET_CLOSE_WINDOW",
				"_NET_MOVERESIZE_WINDOW",
				"_NET_WM_MOVERESIZE",
				"_NET_RESTACK_WINDOW",
				"_NET_REQUEST_FRAME_EXTENTS",
				"_NET_WM_NAME",
				"_NET_WM_VISIBLE_NAME",
				"_NET_WM_ICON_NAME",
				"_NET_WM_VISIBLE_ICON_NAME",
				"_NET_WM_DESKTOP",
				"_NET_WM_WINDOW_TYPE",
				"_NET_WM_STATE",
				"_NET_WM_ALLOWED_ACTIONS",
				"_NET_WM_STRUT",
				"_NET_WM_STRUT_PARTIAL",
				"_NET_WM_ICON_GEOMETRY",
				"_NET_WM_ICON",
				"_NET_WM_PID",
				"_NET_WM_HANDLED_ICONS",
				"_NET_WM_USER_TIME",
				"_NET_FRAME_EXTENTS",
				"_NET_WM_PING",
				"_NET_WM_SYNC_REQUEST",
				"_NET_SYSTEM_TRAY_OPCODE",
				"_NET_SYSTEM_TRAY_ORIENTATION",
				"_NET_WM_STATE_MAXIMIZED_HORZ",
				"_NET_WM_STATE_MAXIMIZED_VERT",
				"_NET_WM_STATE_HIDDEN",
				"_XEMBED",
				"_XEMBED_INFO",
				"_MOTIF_WM_HINTS",
				"_NET_WM_STATE_SKIP_TASKBAR",
				"_NET_WM_STATE_ABOVE",
				"_NET_WM_STATE_MODAL",
				"_NET_WM_CONTEXT_HELP",
				"_NET_WM_WINDOW_OPACITY",
				"_NET_WM_WINDOW_TYPE_DESKTOP",
				"_NET_WM_WINDOW_TYPE_DOCK",
				"_NET_WM_WINDOW_TYPE_TOOLBAR",
				"_NET_WM_WINDOW_TYPE_MENU",
				"_NET_WM_WINDOW_TYPE_UTILITY",
				"_NET_WM_WINDOW_TYPE_DIALOG",
				"_NET_WM_WINDOW_TYPE_SPLASH",
				"_NET_WM_WINDOW_TYPE_NORMAL",
				"CLIPBOARD",
				"PRIMARY",
				"COMPOUND_TEXT",
				"UTF8_STRING",
				"TARGETS",
				"_SWF_AsyncAtom",
				"_SWF_PostMessageAtom",
				"_SWF_HoverAtom" };

			IntPtr[] atoms = new IntPtr [atom_names.Length];;

			XInternAtoms (display, atom_names, atom_names.Length, false, atoms);

			int off = 0;
			WM_PROTOCOLS = atoms [off++];
			WM_DELETE_WINDOW = atoms [off++];
			WM_TAKE_FOCUS = atoms [off++];
			_NET_SUPPORTED = atoms [off++];
			_NET_CLIENT_LIST = atoms [off++];
			_NET_NUMBER_OF_DESKTOPS = atoms [off++];
			_NET_DESKTOP_GEOMETRY = atoms [off++];
			_NET_DESKTOP_VIEWPORT = atoms [off++];
			_NET_CURRENT_DESKTOP = atoms [off++];
			_NET_DESKTOP_NAMES = atoms [off++];
			_NET_ACTIVE_WINDOW = atoms [off++];
			_NET_WORKAREA = atoms [off++];
			_NET_SUPPORTING_WM_CHECK = atoms [off++];
			_NET_VIRTUAL_ROOTS = atoms [off++];
			_NET_DESKTOP_LAYOUT = atoms [off++];
			_NET_SHOWING_DESKTOP = atoms [off++];
			_NET_CLOSE_WINDOW = atoms [off++];
			_NET_MOVERESIZE_WINDOW = atoms [off++];
			_NET_WM_MOVERESIZE = atoms [off++];
			_NET_RESTACK_WINDOW = atoms [off++];
			_NET_REQUEST_FRAME_EXTENTS = atoms [off++];
			_NET_WM_NAME = atoms [off++];
			_NET_WM_VISIBLE_NAME = atoms [off++];
			_NET_WM_ICON_NAME = atoms [off++];
			_NET_WM_VISIBLE_ICON_NAME = atoms [off++];
			_NET_WM_DESKTOP = atoms [off++];
			_NET_WM_WINDOW_TYPE = atoms [off++];
			_NET_WM_STATE = atoms [off++];
			_NET_WM_ALLOWED_ACTIONS = atoms [off++];
			_NET_WM_STRUT = atoms [off++];
			_NET_WM_STRUT_PARTIAL = atoms [off++];
			_NET_WM_ICON_GEOMETRY = atoms [off++];
			_NET_WM_ICON = atoms [off++];
			_NET_WM_PID = atoms [off++];
			_NET_WM_HANDLED_ICONS = atoms [off++];
			_NET_WM_USER_TIME = atoms [off++];
			_NET_FRAME_EXTENTS = atoms [off++];
			_NET_WM_PING = atoms [off++];
			_NET_WM_SYNC_REQUEST = atoms [off++];
			_NET_SYSTEM_TRAY_OPCODE = atoms [off++];
			_NET_SYSTEM_TRAY_ORIENTATION = atoms [off++];
			_NET_WM_STATE_MAXIMIZED_HORZ = atoms [off++];
			_NET_WM_STATE_MAXIMIZED_VERT = atoms [off++];
			_NET_WM_STATE_HIDDEN = atoms [off++];
			_XEMBED = atoms [off++];
			_XEMBED_INFO = atoms [off++];
			_MOTIF_WM_HINTS = atoms [off++];
			_NET_WM_STATE_SKIP_TASKBAR = atoms [off++];
			_NET_WM_STATE_ABOVE = atoms [off++];
			_NET_WM_STATE_MODAL = atoms [off++];
			_NET_WM_CONTEXT_HELP = atoms [off++];
			_NET_WM_WINDOW_OPACITY = atoms [off++];
			_NET_WM_WINDOW_TYPE_DESKTOP = atoms [off++];
			_NET_WM_WINDOW_TYPE_DOCK = atoms [off++];
			_NET_WM_WINDOW_TYPE_TOOLBAR = atoms [off++];
			_NET_WM_WINDOW_TYPE_MENU = atoms [off++];
			_NET_WM_WINDOW_TYPE_UTILITY = atoms [off++];
			_NET_WM_WINDOW_TYPE_DIALOG = atoms [off++];
			_NET_WM_WINDOW_TYPE_SPLASH = atoms [off++];
			_NET_WM_WINDOW_TYPE_NORMAL = atoms [off++];
			CLIPBOARD = atoms [off++];
			PRIMARY = atoms [off++];
			OEMTEXT = atoms [off++];
			UNICODETEXT = atoms [off++];
			TARGETS = atoms [off++];
			AsyncAtom = atoms [off++];
			PostAtom = atoms [off++];
			HoverState = atoms [off++];

			DIB = XA_PIXMAP;

		    var defScreen = XDefaultScreen(display);
			// XXX multi screen stuff here
			_NET_SYSTEM_TRAY_S = XInternAtom (display, "_NET_SYSTEM_TRAY_S" + defScreen.ToString(), false);
		}

	}

}


using System;

namespace Avalonia.X11
{

    internal enum Status
    {
        Success = 0, /* everything's okay */
        BadRequest = 1, /* bad request code */
        BadValue = 2, /* int parameter out of range */
        BadWindow = 3, /* parameter not a Window */
        BadPixmap = 4, /* parameter not a Pixmap */
        BadAtom = 5, /* parameter not an Atom */
        BadCursor = 6, /* parameter not a Cursor */
        BadFont = 7, /* parameter not a Font */
        BadMatch = 8, /* parameter mismatch */
        BadDrawable = 9, /* parameter not a Pixmap or Window */
        BadAccess = 10, /* depending on context:
                                 - key/button already grabbed
                                 - attempt to free an illegal 
                                   cmap entry 
                                - attempt to store into a read-only 
                                   color map entry.
                                - attempt to modify the access control
                                   list from other than the local host.
                                */
        BadAlloc = 11, /* insufficient resources */
        BadColor = 12, /* no such colormap */
        BadGC = 13, /* parameter not a GC */
        BadIDChoice = 14, /* choice not in range or already used */
        BadName = 15, /* font or color name doesn't exist */
        BadLength = 16, /* Request length incorrect */
        BadImplementation = 17, /* server is defective */

        FirstExtensionError = 128,
        LastExtensionError = 255,

    }

    [Flags]
    internal enum XEventMask : int
    {
        NoEventMask = 0,
        KeyPressMask = (1 << 0),
        KeyReleaseMask = (1 << 1),
        ButtonPressMask = (1 << 2),
        ButtonReleaseMask = (1 << 3),
        EnterWindowMask = (1 << 4),
        LeaveWindowMask = (1 << 5),
        PointerMotionMask = (1 << 6),
        PointerMotionHintMask = (1 << 7),
        Button1MotionMask = (1 << 8),
        Button2MotionMask = (1 << 9),
        Button3MotionMask = (1 << 10),
        Button4MotionMask = (1 << 11),
        Button5MotionMask = (1 << 12),
        ButtonMotionMask = (1 << 13),
        KeymapStateMask = (1 << 14),
        ExposureMask = (1 << 15),
        VisibilityChangeMask = (1 << 16),
        StructureNotifyMask = (1 << 17),
        ResizeRedirectMask = (1 << 18),
        SubstructureNotifyMask = (1 << 19),
        SubstructureRedirectMask = (1 << 20),
        FocusChangeMask = (1 << 21),
        PropertyChangeMask = (1 << 22),
        ColormapChangeMask = (1 << 23),
        OwnerGrabButtonMask = (1 << 24)
    }

    [Flags]
    internal enum XModifierMask
    {
        ShiftMask = (1 << 0),
        LockMask = (1 << 1),
        ControlMask = (1 << 2),
        Mod1Mask = (1 << 3),
        Mod2Mask = (1 << 4),
        Mod3Mask = (1 << 5),
        Mod4Mask = (1 << 6),
        Mod5Mask = (1 << 7),
        Button1Mask = (1 << 8),
        Button2Mask = (1 << 9),
        Button3Mask = (1 << 10),
        Button4Mask = (1 << 11),
        Button5Mask = (1 << 12),
        AnyModifier = (1 << 15)

    }
    
    [Flags]
    internal enum XCreateWindowFlags
    {
        CWBackPixmap = (1 << 0),
        CWBackPixel = (1 << 1),
        CWBorderPixmap = (1 << 2),
        CWBorderPixel = (1 << 3),
        CWBitGravity = (1 << 4),
        CWWinGravity = (1 << 5),
        CWBackingStore = (1 << 6),
        CWBackingPlanes = (1 << 7),
        CWBackingPixel = (1 << 8),
        CWOverrideRedirect = (1 << 9),
        CWSaveUnder = (1 << 10),
        CWEventMask = (1 << 11),
        CWDontPropagate = (1 << 12),
        CWColormap = (1 << 13),
        CWCursor = (1 << 14),
    }
}

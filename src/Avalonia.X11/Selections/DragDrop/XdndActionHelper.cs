using System;
using Avalonia.Input;

namespace Avalonia.X11.Selections.DragDrop;

internal static class XdndActionHelper
{
    public static DragDropEffects ActionToEffects(IntPtr action, X11Atoms atoms)
    {
        if (action == atoms.XdndActionCopy)
            return DragDropEffects.Copy;
        if (action == atoms.XdndActionMove)
            return DragDropEffects.Move;
        if (action == atoms.XdndActionLink)
            return DragDropEffects.Link;
        return DragDropEffects.None;
    }

    public static IntPtr EffectsToAction(DragDropEffects effects, X11Atoms atoms)
    {
        if ((effects & DragDropEffects.Copy) != 0)
            return atoms.XdndActionCopy;
        if ((effects & DragDropEffects.Move) != 0)
            return atoms.XdndActionMove;
        if ((effects & DragDropEffects.Link) != 0)
            return atoms.XdndActionLink;
        return 0;
    }
}

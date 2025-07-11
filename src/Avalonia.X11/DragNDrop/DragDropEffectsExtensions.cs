using System;
using Avalonia.Input;

namespace Avalonia.X11
{
    internal static class DragDropEffectsExtensions
    {
        public static DragDropEffects ConvertDropEffect(this X11Atoms atoms, IntPtr effect)
        {
            DragDropEffects result = DragDropEffects.None;
            if (((uint)effect & (uint)(atoms.XdndActionCopy)) == (uint)atoms.XdndActionCopy)
                result |= DragDropEffects.Copy;
            if (((uint)effect & (uint)(atoms.XdndActionMove)) == (uint)atoms.XdndActionMove)
                result |= DragDropEffects.Move;
            if (((uint)effect & (uint)(atoms.XdndActionLink)) == (uint)atoms.XdndActionLink)
                result |= DragDropEffects.Link;
            return result;
        }       

        public static IntPtr ConvertDropEffect(this X11Atoms atoms, DragDropEffects operation)
        {
            uint result = (uint)IntPtr.Zero;
            if (operation.HasAllFlags(DragDropEffects.Copy))
                result |= (uint)atoms.XdndActionCopy;
            if (operation.HasAllFlags(DragDropEffects.Move))
                result |= (uint)atoms.XdndActionMove;
            if (operation.HasAllFlags(DragDropEffects.Link))
                result |= (uint)atoms.XdndActionLink;
            return (IntPtr)result;
        }
    }
}

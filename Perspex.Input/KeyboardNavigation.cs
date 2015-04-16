// -----------------------------------------------------------------------
// <copyright file="KeyboardNavigation.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input
{
    using System.Linq;
    using Perspex.VisualTree;

    /// <summary>
    /// TODO: This class is a temporary stop-gap just to add tab handling. Should be replaced 
    /// with something better thought out and testable etc.
    /// </summary>
    public static class KeyboardNavigation
    {
        public static void MoveNext(IInputElement element)
        {
            var siblings = element.GetVisualSiblings().OfType<IInputElement>()
                .Where(x => x.Focusable)
                .SkipWhile(x => x != element)
                .Skip(1);

            var next = siblings.FirstOrDefault();

            if (next != null)
            {
                FocusManager.Instance.Focus(next, true);
            }
        }

        public static void MovePrevious(IInputElement element)
        {
            var siblings = element.GetVisualSiblings().OfType<IInputElement>()
                .Where(x => x.Focusable)
                .Reverse()
                .SkipWhile(x => x != element)
                .Skip(1);

            var next = siblings.FirstOrDefault();

            if (next != null)
            {
                FocusManager.Instance.Focus(next, true);
            }
        }
    }
}

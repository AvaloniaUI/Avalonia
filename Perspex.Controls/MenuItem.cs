// -----------------------------------------------------------------------
// <copyright file="MenuItem.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using Perspex.Controls.Primitives;
    using Perspex.Input;

    public class MenuItem : HeaderedItemsControl
    {
        public static readonly PerspexProperty<bool> IsSubMenuOpenProperty =
            PerspexProperty.Register<MenuItem, bool>("IsSubMenuOpen");

        public bool IsSubMenuOpen
        {
            get { return this.GetValue(IsSubMenuOpenProperty); }
            set { this.SetValue(IsSubMenuOpenProperty, value); }
        }

        protected override void OnPointerPressed(PointerPressEventArgs e)
        {
            base.OnPointerPressed(e);
            this.IsSubMenuOpen = !this.IsSubMenuOpen;
        }
    }
}

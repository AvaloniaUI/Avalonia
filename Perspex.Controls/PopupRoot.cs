// -----------------------------------------------------------------------
// <copyright file="PopupRoot.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using Perspex.Media;
    using Perspex.Platform;
    using Splat;

    public class PopupRoot : TopLevel
    {
        static PopupRoot()
        {
            BackgroundProperty.OverrideDefaultValue(typeof(PopupRoot), Brushes.White);
        }

        public PopupRoot()
            : base(Locator.Current.GetService<IPopupImpl>())
        {
        }

        public new IPopupImpl PlatformImpl
        {
            get { return (IPopupImpl)base.PlatformImpl; }
        }

        public void SetPosition(Point p)
        {
            this.PlatformImpl.SetPosition(p);
        }

        public void Hide()
        {
            this.PlatformImpl.Hide();
            this.IsVisible = false;
        }

        public void Show()
        {
            this.PlatformImpl.Show();
            this.LayoutManager.ExecuteLayoutPass();
            this.IsVisible = true;
        }
    }
}

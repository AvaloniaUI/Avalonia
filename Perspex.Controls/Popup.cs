// -----------------------------------------------------------------------
// <copyright file="ListBox.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using Perspex.Platform;
    using Perspex.Rendering;

    public class Popup : ContentControl
    {
        public static readonly PerspexProperty<bool> IsOpenProperty =
            PerspexProperty.Register<Popup, bool>("IsOpen");

        private IPopupImpl impl;

        private Window window;

        static Popup()
        {
            IsOpenProperty.Changed.Subscribe(x =>
            {
                Popup popup = x.Sender as Popup;

                if (popup != null)
                {
                    if ((bool)x.NewValue)
                    {
                        popup.Open();
                    }
                    else
                    {
                        popup.Close();
                    }
                }
            });
        }

        public bool IsOpen
        {
            get { return this.GetValue(IsOpenProperty); }
            set { this.SetValue(IsOpenProperty, value); }
        }

        public void Open()
        {
            if (this.impl == null)
            {
                this.impl = this.window.CreatePopup();
            }
        }

        public void Close()
        {
            if (this.impl != null)
            {
                this.impl.Dispose();
            }
        }

        protected override void OnAttachedToVisualTree(IRenderRoot root)
        {
            base.OnAttachedToVisualTree(root);
            this.window = root as Window;
        }

        protected override void OnDetachedFromVisualTree(IRenderRoot oldRoot)
        {
            base.OnDetachedFromVisualTree(oldRoot);
            this.window = null;
        }
    }
}

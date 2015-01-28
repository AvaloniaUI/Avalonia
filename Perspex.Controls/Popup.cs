// -----------------------------------------------------------------------
// <copyright file="Popup.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using Perspex.Platform;
    using Perspex.Rendering;

    public class Popup : Control
    {
        public static readonly PerspexProperty<Control> ChildProperty =
            PerspexProperty.Register<Popup, Control>("Child");

        public static readonly PerspexProperty<bool> IsOpenProperty =
            PerspexProperty.Register<Popup, bool>("IsOpen");

        public static readonly PerspexProperty<Control> PlacementTargetProperty =
            PerspexProperty.Register<Popup, Control>("PlacementTarget");

        public static readonly PerspexProperty<bool> StaysOpenProperty =
            PerspexProperty.Register<Popup, bool>("StaysOpen", true);

        private PopupRoot popupRoot;

        private TopLevel topLevel;

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

        public Control Child
        {
            get { return this.GetValue(ChildProperty); }
            set { this.SetValue(ChildProperty, value); }
        }

        public bool IsOpen
        {
            get { return this.GetValue(IsOpenProperty); }
            set { this.SetValue(IsOpenProperty, value); }
        }

        public Control PlacementTarget
        {
            get { return this.GetValue(PlacementTargetProperty); }
            set { this.SetValue(PlacementTargetProperty, value); }
        }

        public bool StaysOpen
        {
            get { return this.GetValue(StaysOpenProperty); }
            set { this.SetValue(StaysOpenProperty, value); }
        }

        public void Open()
        {
            if (this.popupRoot == null)
            {
                this.popupRoot = new PopupRoot();
                this.popupRoot.Parent = this;
                this.popupRoot[~PopupRoot.ContentProperty] = this[~ChildProperty];
            }

            this.popupRoot.SetPosition(this.GetPosition());
            this.popupRoot.PreviewPointerPressed += this.MaybeClose;
            this.topLevel.PreviewPointerPressed += this.MaybeClose;
            this.topLevel.Deactivated += this.MaybeClose;
            this.popupRoot.Show();
        }

        public void Close()
        {
            if (this.popupRoot != null)
            {
                this.popupRoot.PreviewPointerPressed -= this.MaybeClose;
                this.topLevel.PreviewPointerPressed -= this.MaybeClose;
                this.topLevel.Deactivated -= this.MaybeClose;
                this.popupRoot.Hide();
            }

            this.IsOpen = false;
        }

        protected override void OnAttachedToVisualTree(IRenderRoot root)
        {
            base.OnAttachedToVisualTree(root);
            this.topLevel = root as TopLevel;
        }

        protected override void OnDetachedFromVisualTree(IRenderRoot oldRoot)
        {
            base.OnDetachedFromVisualTree(oldRoot);
            this.topLevel = null;
        }

        private Point GetPosition()
        {
            if (this.PlacementTarget != null)
            {
                return this.PlacementTarget.PointToScreen(new Point(0, this.PlacementTarget.ActualSize.Height));
            }
            else
            {
                return new Point();
            }
        }

        private void MaybeClose(object sender, EventArgs e)
        {
            if (!this.StaysOpen)
            {
                this.Close();
            }
        }
    }
}

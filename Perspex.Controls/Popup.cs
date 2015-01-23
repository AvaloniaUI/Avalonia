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

    public class Popup : Control
    {
        public static readonly PerspexProperty<Control> ChildProperty =
            PerspexProperty.Register<Popup, Control>("Child");

        public static readonly PerspexProperty<bool> IsOpenProperty =
            PerspexProperty.Register<Popup, bool>("IsOpen");

        public static readonly PerspexProperty<Control> PlacementTargetProperty =
            PerspexProperty.Register<Popup, Control>("PlacementTarget");

        private PopupRoot root;

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

        public void Open()
        {
            if (this.root == null)
            {
                this.root = new PopupRoot();
                this.root.Parent = this;
                this.root[~PopupRoot.ContentProperty] = this[~ChildProperty];
            }

            this.root.SetPosition(this.GetPosition());
            this.root.Show();
        }

        public void Close()
        {
            if (this.root != null)
            {
                this.root.Hide();
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
    }
}

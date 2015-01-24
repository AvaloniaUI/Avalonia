// -----------------------------------------------------------------------
// <copyright file="DropDown.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using Perspex.Collections;
    using Perspex.Controls.Presenters;
    using Perspex.Controls.Primitives;
    using Perspex.Layout;

    public class DropDown : SelectingItemsControl, IContentControl, ILogical
    {
        public static readonly PerspexProperty<object> ContentProperty =
            ContentControl.ContentProperty.AddOwner<DropDown>();

        public static readonly PerspexProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
            ContentControl.HorizontalContentAlignmentProperty.AddOwner<DropDown>();

        public static readonly PerspexProperty<VerticalAlignment> VerticalContentAlignmentProperty =
            ContentControl.VerticalContentAlignmentProperty.AddOwner<DropDown>();

        public static readonly PerspexProperty<bool> IsDropDownOpenProperty =
            PerspexProperty.Register<DropDown, bool>("IsDropDownOpen");

        private SingleItemPerspexList<ILogical> logicalChild = new SingleItemPerspexList<ILogical>();

        private ContentPresenter presenter;

        private IDisposable presenterSubscription;

        public DropDown()
        {
            this.GetObservableWithHistory(ContentProperty).Subscribe(this.SetContentParent);
            this.Bind(ContentProperty, this.GetObservable(DropDown.SelectedItemProperty));
        }

        public object Content
        {
            get { return this.GetValue(ContentProperty); }
            set { this.SetValue(ContentProperty, value); }
        }

        public HorizontalAlignment HorizontalContentAlignment
        {
            get { return this.GetValue(HorizontalContentAlignmentProperty); }
            set { this.SetValue(HorizontalContentAlignmentProperty, value); }
        }

        public VerticalAlignment VerticalContentAlignment
        {
            get { return this.GetValue(VerticalContentAlignmentProperty); }
            set { this.SetValue(VerticalContentAlignmentProperty, value); }
        }

        public bool IsDropDownOpen
        {
            get { return this.GetValue(IsDropDownOpenProperty); }
            set { this.SetValue(IsDropDownOpenProperty, value); }
        }

        IReadOnlyPerspexList<ILogical> ILogical.LogicalChildren
        {
            get { return this.logicalChild; }
        }

        protected override void OnTemplateApplied()
        {
            if (this.presenterSubscription != null)
            {
                this.presenterSubscription.Dispose();
                this.presenterSubscription = null;
            }

            this.presenter = this.FindTemplateChild<ContentPresenter>("contentPresenter");

            if (this.presenter != null)
            {
                this.presenterSubscription = this.presenter.ChildObservable
                    .Subscribe(x => this.logicalChild.SingleItem = x);
            }
        }
        
        private void SetContentParent(Tuple<object, object> change)
        {
            var control1 = change.Item1 as Control;
            var control2 = change.Item2 as Control;

            if (control1 != null)
            {
                control1.Parent = null;
            }

            if (control2 != null)
            {
                control2.Parent = this;
            }
        }
    }
}

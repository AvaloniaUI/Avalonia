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
    using Perspex.Controls.Templates;
    using Perspex.Input;
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

        private PerspexReadOnlyListView<ILogical> logicalChildren = new PerspexReadOnlyListView<ILogical>();

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

        IPerspexReadOnlyList<ILogical> ILogical.LogicalChildren
        {
            get { return this.logicalChildren; }
        }

        protected override void OnPointerPressed(PointerPressEventArgs e)
        {
            if (!this.IsDropDownOpen)
            {
                this.IsDropDownOpen = true;
            }
        }

        protected override void OnTemplateApplied()
        {
            var container = this.GetTemplateChild<Panel>("container");
            this.logicalChildren.Source = ((ILogical)container).LogicalChildren;
        }

        private void SetContentParent(Tuple<object, object> change)
        {
            var control1 = change.Item1 as Control;
            var control2 = change.Item2 as Control;

            if (control1 != null)
            {
                ((ISetLogicalParent)control1).SetParent(null);
            }

            if (control2 != null)
            {
                ((ISetLogicalParent)control2).SetParent(this);
            }
        }
    }
}

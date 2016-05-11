// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls.Mixins;
using Avalonia.Controls.Presenters;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Represents a <see cref="SelectingItemsControl"/> with a related header.
    /// </summary>
    public class HeaderedSelectingItemsControl : SelectingItemsControl
    {
        /// <summary>
        /// Defines the <see cref="Header"/> property.
        /// </summary>
        public static readonly StyledProperty<object> HeaderProperty =
            HeaderedContentControl.HeaderProperty.AddOwner<HeaderedSelectingItemsControl>();

        /// <summary>
        /// Initializes static members of the <see cref="ContentControl"/> class.
        /// </summary>
        static HeaderedSelectingItemsControl()
        {
            ContentControlMixin.Attach<HeaderedSelectingItemsControl>(
                HeaderProperty,
                x => x.LogicalChildren,
                "PART_HeaderPresenter");
        }

        /// <summary>
        /// Gets or sets the content of the control's header.
        /// </summary>
        public object Header
        {
            get { return GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        /// <summary>
        /// Gets the header presenter from the control's template.
        /// </summary>
        public ContentPresenter HeaderPresenter
        {
            get;
            private set;
        }

        /// <inheritdoc/>
        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            base.OnTemplateApplied(e);
            HeaderPresenter = e.NameScope.Find<ContentPresenter>("PART_HeaderPresenter");
        }
    }
}

// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls.Mixins;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// A <see cref="ContentControl"/> with a header.
    /// </summary>
    public class HeaderedContentControl : ContentControl, IHeadered
    {
        /// <summary>
        /// Defines the <see cref="Header"/> property.
        /// </summary>
        public static readonly StyledProperty<object> HeaderProperty =
            AvaloniaProperty.Register<HeaderedContentControl, object>(nameof(Header));

        /// <summary>
        /// Defines the <see cref="HeaderTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate> HeaderTemplateProperty =
            AvaloniaProperty.Register<HeaderedContentControl, IDataTemplate>(nameof(HeaderTemplate));

        /// <summary>
        /// Initializes static members of the <see cref="ContentControl"/> class.
        /// </summary>
        static HeaderedContentControl()
        {
            ContentControlMixin.Attach<HeaderedContentControl>(
                HeaderProperty,
                x => x.LogicalChildren,
                "PART_HeaderPresenter");
        }

        /// <summary>
        /// Gets or sets the header content.
        /// </summary>
        public object Header
        {
            get { return GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        /// <summary>
        /// Gets the header presenter from the control's template.
        /// </summary>
        public IContentPresenter HeaderPresenter
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the data template used to display the header content of the control.
        /// </summary>
        public IDataTemplate HeaderTemplate
        {
            get { return GetValue(HeaderTemplateProperty); }
            set { SetValue(HeaderTemplateProperty, value); }
        }

        /// <inheritdoc/>
        protected override void RegisterContentPresenter(IContentPresenter presenter)
        {
            base.RegisterContentPresenter(presenter);

            if (presenter.Name == "PART_HeaderPresenter")
            {
                HeaderPresenter = presenter;
            }
        }
    }
}

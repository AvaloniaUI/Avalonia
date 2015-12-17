// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Linq;
using Perspex.Controls.Mixins;
using Perspex.Controls.Presenters;
using Perspex.Controls.Primitives;
using Perspex.Controls.Templates;
using Perspex.Layout;
using Perspex.Metadata;

namespace Perspex.Controls
{
    /// <summary>
    /// Displays <see cref="Content"/> according to a <see cref="FuncDataTemplate"/>.
    /// </summary>
    public class ContentControl : TemplatedControl, IContentControl
    {
        /// <summary>
        /// Defines the <see cref="Content"/> property.
        /// </summary>
        public static readonly PerspexProperty<object> ContentProperty =
            PerspexProperty.Register<ContentControl, object>(nameof(Content));

        /// <summary>
        /// Defines the <see cref="HorizontalContentAlignment"/> property.
        /// </summary>
        public static readonly PerspexProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
            PerspexProperty.Register<ContentControl, HorizontalAlignment>(nameof(HorizontalContentAlignment));

        /// <summary>
        /// Defines the <see cref="VerticalContentAlignment"/> property.
        /// </summary>
        public static readonly PerspexProperty<VerticalAlignment> VerticalContentAlignmentProperty =
            PerspexProperty.Register<ContentControl, VerticalAlignment>(nameof(VerticalContentAlignment));

        /// <summary>
        /// Initializes static members of the <see cref="ContentControl"/> class.
        /// </summary>
        static ContentControl()
        {
            ContentControlMixin.Attach<ContentControl>(ContentProperty, x => x.LogicalChildren);
        }

        /// <summary>
        /// Gets or sets the content to display.
        /// </summary>
        [Content]
        public object Content
        {
            get { return GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        /// <summary>
        /// Gets the presenter from the control's template.
        /// </summary>
        public ContentPresenter Presenter
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the horizontal alignment of the content within the control.
        /// </summary>
        public HorizontalAlignment HorizontalContentAlignment
        {
            get { return GetValue(HorizontalContentAlignmentProperty); }
            set { SetValue(HorizontalContentAlignmentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the vertical alignment of the content within the control.
        /// </summary>
        public VerticalAlignment VerticalContentAlignment
        {
            get { return GetValue(VerticalContentAlignmentProperty); }
            set { SetValue(VerticalContentAlignmentProperty, value); }
        }

        /// <inheritdoc/>
        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            base.OnTemplateApplied(e);

            // We allow ContentControls without ContentPresenters in the template. This can be
            // useful for e.g. a simple ToggleButton that displays an image. There's no need to
            // have a ContentPresenter in the visual tree for that.
            Presenter = e.NameScope.Find<ContentPresenter>("PART_ContentPresenter");
        }
    }
}

// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Collections;
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
    public class ContentControl : TemplatedControl, IContentControl, IReparentingHost
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
        /// Initializes static members of the <see cref="Button"/> class.
        /// </summary>
        static ContentControl()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentControl"/> class.
        /// </summary>
        public ContentControl()
        {
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

        /// <summary>
        /// Gets a writeable logical children collection from the host.
        /// </summary>
        IPerspexList<ILogical> IReparentingHost.LogicalChildren => LogicalChildren;

        /// <summary>
        /// Asks the control whether it wants to reparent the logical children of the specified
        /// control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>
        /// True if the control wants to reparent its logical children otherwise false.
        /// </returns>
        bool IReparentingHost.WillReparentChildrenOf(IControl control)
        {
            return control is IContentPresenter && control.TemplatedParent == this;
        }

        /// <inheritdoc/>
        protected override void OnTemplateApplied()
        {
            // We allow ContentControls without ContentPresenters in the template. This can be
            // useful for e.g. a simple ToggleButton that displays an image. There's no need to
            // have a ContentPresenter in the visual tree for that.
            Presenter = this.FindTemplateChild<ContentPresenter>("PART_ContentPresenter");
        }
    }
}

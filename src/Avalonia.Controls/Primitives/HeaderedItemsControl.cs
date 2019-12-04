// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Collections;
using Avalonia.Controls.Mixins;
using Avalonia.Controls.Presenters;
using Avalonia.LogicalTree;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Represents an <see cref="ItemsControl"/> with a related header.
    /// </summary>
    public class HeaderedItemsControl : ItemsControl, IContentPresenterHost
    {
        /// <summary>
        /// Defines the <see cref="Header"/> property.
        /// </summary>
        public static readonly StyledProperty<object> HeaderProperty =
            HeaderedContentControl.HeaderProperty.AddOwner<HeaderedItemsControl>();

        /// <summary>
        /// Initializes static members of the <see cref="ContentControl"/> class.
        /// </summary>
        static HeaderedItemsControl()
        {
            HeaderProperty.Changed.AddClassHandler<HeaderedItemsControl>((x, e) => x.HeaderChanged(e));
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
        public IContentPresenter HeaderPresenter
        {
            get;
            private set;
        }

        /// <inheritdoc/>
        IAvaloniaList<ILogical> IContentPresenterHost.LogicalChildren => LogicalChildren;

        /// <inheritdoc/>
        bool IContentPresenterHost.RegisterContentPresenter(IContentPresenter presenter)
        {
            return RegisterContentPresenter(presenter);
        }

        /// <summary>
        /// Called when an <see cref="IContentPresenter"/> is registered with the control.
        /// </summary>
        /// <param name="presenter">The presenter.</param>
        protected virtual bool RegisterContentPresenter(IContentPresenter presenter)
        {
            if (presenter.Name == "PART_HeaderPresenter")
            {
                HeaderPresenter = presenter;
                return true;
            }

            return false;
        }

        private void HeaderChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.OldValue is ILogical oldChild)
            {
                LogicalChildren.Remove(oldChild);
            }

            if (e.NewValue is ILogical newChild)
            {
                LogicalChildren.Add(newChild);
            }
        }
    }
}

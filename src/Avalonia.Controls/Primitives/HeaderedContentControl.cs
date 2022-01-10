using System;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;

#nullable enable

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
        public static readonly StyledProperty<object?> HeaderProperty =
            AvaloniaProperty.Register<HeaderedContentControl, object?>(nameof(Header));

        /// <summary>
        /// Defines the <see cref="HeaderTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate?> HeaderTemplateProperty =
            AvaloniaProperty.Register<HeaderedContentControl, IDataTemplate?>(nameof(HeaderTemplate));

        private ILogical? _headerChild;

        /// <summary>
        /// Gets or sets the header content.
        /// </summary>
        public object? Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        /// <summary>
        /// Gets the header presenter from the control's template.
        /// </summary>
        public IContentPresenter? HeaderPresenter
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the data template used to display the header content of the control.
        /// </summary>
        public IDataTemplate? HeaderTemplate
        {
            get => GetValue(HeaderTemplateProperty);
            set => SetValue(HeaderTemplateProperty, value);
        }

        protected override int LogicalChildrenCount => base.LogicalChildrenCount + (_headerChild is null ? 0 : 1);

        protected override ILogical GetLogicalChild(int index)
        {
            if (index == base.LogicalChildrenCount && _headerChild is not null)
                return _headerChild;
            return base.GetLogicalChild(index);
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == HeaderProperty)
                SetHeaderChild(change.NewValue.GetValueOrDefault<ILogical>());
        }

        /// <summary>
        /// Called when an <see cref="IContentPresenter"/> is registered with the control.
        /// </summary>
        /// <param name="presenter">The presenter.</param>
        protected override void RegisterContentPresenter(IContentPresenter presenter)
        {
            base.RegisterContentPresenter(presenter);
            if (presenter.Name == "PART_HeaderPresenter")
                HeaderPresenter = presenter;
        }

        /// <summary>
        /// Called when a registered <see cref="IContentPresenter"/>'s logical child changes.
        /// </summary>
        /// <param name="presenter">The presenter.</param>
        /// <param name="child">The new logical child.</param>
        protected override void RegisterLogicalChild(IContentPresenter presenter, ILogical child)
        {
            base.RegisterLogicalChild(presenter, child);
            if (presenter == HeaderPresenter)
                SetHeaderChild(child);
        }

        private void SetHeaderChild(ILogical? child)
        {
            if (_headerChild != child)
            {
                if (_headerChild?.LogicalParent == this)
                    ((ISetLogicalParent)_headerChild).SetParent(null);

                _headerChild = child;

                if (_headerChild is not null && _headerChild.LogicalParent is null)
                    ((ISetLogicalParent)_headerChild).SetParent(this);

                OnLogicalChildrenChanged(EventArgs.Empty);
            }
        }
    }
}

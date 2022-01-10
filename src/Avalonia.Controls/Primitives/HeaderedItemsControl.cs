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

        private ILogical _headerChild;

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

        void IContentPresenterHost.RegisterContentPresenter(IContentPresenter presenter)
        {
            RegisterContentPresenter(presenter);
        }

        void IContentPresenterHost.RegisterLogicalChild(IContentPresenter presenter, ILogical child)
        {
            RegisterLogicalChild(presenter, child);
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
        protected virtual void RegisterContentPresenter(IContentPresenter presenter)
        {
            if (presenter.Name == "PART_HeaderPresenter")
                HeaderPresenter = presenter;
        }

        /// <summary>
        /// Called when a registered <see cref="IContentPresenter"/>'s logical child changes.
        /// </summary>
        /// <param name="presenter">The presenter.</param>
        /// <param name="child">The new logical child.</param>
        protected virtual void RegisterLogicalChild(IContentPresenter presenter, ILogical child)
        {
            if (presenter == HeaderPresenter)
                SetHeaderChild(child);
        }

        private void SetHeaderChild(ILogical child)
        {
            if (_headerChild != child)
            {
                if (_headerChild is not null)
                    LogicalChildren.Remove(_headerChild);
                _headerChild = child;
                if (_headerChild is not null)
                    LogicalChildren.Add(_headerChild);
            }
        }
    }
}

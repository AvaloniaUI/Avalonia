using System;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Metadata;

namespace Avalonia.Controls
{
    /// <summary>
    /// Displays <see cref="Content"/> according to a <see cref="FuncDataTemplate"/>.
    /// </summary>
    public class ContentControl : TemplatedControl, IContentControl, IContentPresenterHost
    {
        /// <summary>
        /// Defines the <see cref="Content"/> property.
        /// </summary>
        public static readonly StyledProperty<object> ContentProperty =
            AvaloniaProperty.Register<ContentControl, object>(nameof(Content));

        /// <summary>
        /// Defines the <see cref="ContentTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate> ContentTemplateProperty =
            AvaloniaProperty.Register<ContentControl, IDataTemplate>(nameof(ContentTemplate));

        /// <summary>
        /// Defines the <see cref="HorizontalContentAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
            AvaloniaProperty.Register<ContentControl, HorizontalAlignment>(nameof(HorizontalContentAlignment));

        /// <summary>
        /// Defines the <see cref="VerticalContentAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<VerticalAlignment> VerticalContentAlignmentProperty =
            AvaloniaProperty.Register<ContentControl, VerticalAlignment>(nameof(VerticalContentAlignment));

        private ILogical _logicalChild;

        /// <summary>
        /// Gets or sets the content to display.
        /// </summary>
        [Content]
        [DependsOn(nameof(ContentTemplate))]
        public object Content
        {
            get { return GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the data template used to display the content of the control.
        /// </summary>
        public IDataTemplate ContentTemplate
        {
            get { return GetValue(ContentTemplateProperty); }
            set { SetValue(ContentTemplateProperty, value); }
        }

        /// <summary>
        /// Gets the presenter from the control's template.
        /// </summary>
        public IContentPresenter Presenter
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

        protected override int LogicalChildrenCount => _logicalChild is null ? 0 : 1;
        protected override event EventHandler LogicalChildrenChanged;

        void IContentPresenterHost.RegisterContentPresenter(IContentPresenter presenter)
        {
            RegisterContentPresenter(presenter);
        }

        void IContentPresenterHost.RegisterLogicalChild(IContentPresenter presenter, ILogical child)
        {
            RegisterLogicalChild(presenter, child);
        }

        protected override ILogical GetLogicalChild(int index)
        {
            return (index == 0 && _logicalChild is not null) ?
                _logicalChild : throw new ArgumentOutOfRangeException(nameof(index));
        }

        protected virtual void OnLogicalChildrenChanged(EventArgs e)
        {
            LogicalChildrenChanged?.Invoke(this, e);
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ContentProperty)
                SetLogicalChild(change.NewValue.GetValueOrDefault<ILogical>());
        }

        /// <summary>
        /// Called when an <see cref="IContentPresenter"/> is registered with the control.
        /// </summary>
        /// <param name="presenter">The presenter.</param>
        protected virtual void RegisterContentPresenter(IContentPresenter presenter)
        {
            if (presenter.Name == "PART_ContentPresenter")
                Presenter = presenter;
        }

        /// <summary>
        /// Called when a registered <see cref="IContentPresenter"/>'s logical child changes.
        /// </summary>
        /// <param name="presenter">The presenter.</param>
        /// <param name="child">The new logical child.</param>
        protected virtual void RegisterLogicalChild(IContentPresenter presenter, ILogical child)
        {
            if (presenter == Presenter)
                SetLogicalChild(child);
        }

        private void SetLogicalChild(ILogical child)
        {
            if (_logicalChild != child)
            {
                if (_logicalChild?.LogicalParent == this)
                    ((ISetLogicalParent)_logicalChild).SetParent(null);

                _logicalChild = child;

                if (_logicalChild is not null && _logicalChild.LogicalParent is null)
                    ((ISetLogicalParent)_logicalChild).SetParent(this);

                OnLogicalChildrenChanged(EventArgs.Empty);
            }
        }
    }
}

using Avalonia.Controls.Primitives;
using Avalonia.Controls.Mixins;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;


namespace Avalonia.Controls
{
    /// <summary>
    /// A WinUi like ToggleSwitch control.
    /// </summary>
    /// 

    public class ToggleSwitch : ToggleButton
    {
        public static readonly StyledProperty<object> OffContentProperty =
         AvaloniaProperty.Register<ToggleSwitch, object>(nameof(OffContent));

        public static readonly StyledProperty<IDataTemplate> OffContentTemplateProperty =
            AvaloniaProperty.Register<ToggleSwitch, IDataTemplate>(nameof(OffContentTemplate));


        public static readonly StyledProperty<object> OnContentProperty =
      AvaloniaProperty.Register<ToggleSwitch, object>(nameof(OnContent));

        public static readonly StyledProperty<IDataTemplate> OnContentTemplateProperty =
            AvaloniaProperty.Register<ToggleSwitch, IDataTemplate>(nameof(OnContentTemplate));

        public object OnContent
        {
            get { return GetValue(OnContentProperty); }
            set { SetValue(OnContentProperty, value); }
        }

        public object OffContent
        {
            get { return GetValue(OffContentProperty); }
            set { SetValue(OffContentProperty, value); }
        }

        public IContentPresenter OffContentPresenter
        {
            get;
            private set;
        }

        public IContentPresenter OnContentPresenter
        {
            get;
            private set;
        }


        public IDataTemplate OffContentTemplate
        {
            get { return GetValue(OffContentTemplateProperty); }
            set { SetValue(OffContentTemplateProperty, value); }
        }

        public IDataTemplate OnContentTemplate
        {
            get { return GetValue(OnContentTemplateProperty); }
            set { SetValue(OnContentTemplateProperty, value); }
        }

        private void OffContentChanged(AvaloniaPropertyChangedEventArgs e)
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

        private void OnContentChanged(AvaloniaPropertyChangedEventArgs e)
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

        static ToggleSwitch()
        {
            OffContentProperty.Changed.AddClassHandler<ToggleSwitch>((x, e) => x.OffContentChanged(e));
            OnContentProperty.Changed.AddClassHandler<ToggleSwitch>((x, e) => x.OnContentChanged(e));
        }


        protected override bool RegisterContentPresenter(IContentPresenter presenter)
        {
            var result = base.RegisterContentPresenter(presenter);

            if (presenter.Name == "Part_OnContentPresenter")
            {
                OnContentPresenter = presenter;
                result = true;
            }
            if (presenter.Name == "PART_OffContentPresenter")
            {
                OffContentPresenter = presenter;
                result = true;
            }

            return result;
        }
    }           
}


using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// A Toggle Switch control.
    /// </summary>
    public class ToggleSwitch : ToggleButton
    {
        static ToggleSwitch()
        {
            OffContentProperty.Changed.AddClassHandler<ToggleSwitch>((x, e) => x.OffContentChanged(e));
            OnContentProperty.Changed.AddClassHandler<ToggleSwitch>((x, e) => x.OnContentChanged(e));
        }

        /// <summary>
        /// Defines the <see cref="OffContent"/> property.
        /// </summary>
        public static readonly StyledProperty<object> OffContentProperty =
         AvaloniaProperty.Register<ToggleSwitch, object>(nameof(OffContent), defaultValue: "Off");

        /// <summary>
        /// Defines the <see cref="OffContentTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate> OffContentTemplateProperty =
            AvaloniaProperty.Register<ToggleSwitch, IDataTemplate>(nameof(OffContentTemplate));

        /// <summary>
        /// Defines the <see cref="OnContent"/> property.
        /// </summary>
        public static readonly StyledProperty<object> OnContentProperty =
            AvaloniaProperty.Register<ToggleSwitch, object>(nameof(OnContent), defaultValue: "On");

        /// <summary>
        /// Defines the <see cref="OnContentTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate> OnContentTemplateProperty =
            AvaloniaProperty.Register<ToggleSwitch, IDataTemplate>(nameof(OnContentTemplate));

        /// <summary>
        /// Gets or Sets the Content that is displayed when in the On State.
        /// </summary>
        public object OnContent
        {
            get { return GetValue(OnContentProperty); }
            set { SetValue(OnContentProperty, value); }
        }

        /// <summary>
        /// Gets or Sets the Content that is displayed when in the Off State.
        /// </summary>
        public object OffContent
        {
            get { return GetValue(OffContentProperty); }
            set { SetValue(OffContentProperty, value); }
        }

        /// <summary>
        /// Gets or Sets the <see cref="IDataTemplate"/> used to display the <see cref="OffContent"/>.
        /// </summary>
        public IDataTemplate OffContentTemplate
        {
            get { return GetValue(OffContentTemplateProperty); }
            set { SetValue(OffContentTemplateProperty, value); }
        }

        /// <summary>
        /// Gets or Sets the <see cref="IDataTemplate"/> used to display the <see cref="OnContent"/>.
        /// </summary>
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
    }
}


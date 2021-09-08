using System;
using Avalonia.Controls.Platform;
using Avalonia.Platform;

#nullable enable

namespace Avalonia.Controls
{
    public class TrayIcon : AvaloniaObject, IDataContextProvider
    {
        private readonly ITrayIconImpl _impl;

        private TrayIcon(ITrayIconImpl impl)
        {
            _impl = impl;
        }

        public TrayIcon () : this(PlatformManager.CreateTrayIcon())
        {
            
        }

        /// <summary>
        /// Defines the <see cref="DataContext"/> property.
        /// </summary>
        public static readonly StyledProperty<object?> DataContextProperty =
            StyledElement.DataContextProperty.AddOwner<Application>();

        /// <summary>
        /// Defines the <see cref="Icon"/> property.
        /// </summary>
        public static readonly StyledProperty<WindowIcon> IconProperty =
            Window.IconProperty.AddOwner<TrayIcon>();


        public static readonly StyledProperty<string?> ToolTipTextProperty =
            AvaloniaProperty.Register<TrayIcon, string?>(nameof(ToolTipText));

        /// <summary>
        /// Defines the <see cref="IsVisibleProperty"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsVisibleProperty =
            Visual.IsVisibleProperty.AddOwner<TrayIcon>();

        /// <summary>
        /// Removes the notify icon from the taskbar notification area.
        /// </summary>
        public void Remove()
        {

        }


        public new ITrayIconImpl PlatformImpl => _impl;


        /// <summary>
        /// Gets or sets the Applications's data context.
        /// </summary>
        /// <remarks>
        /// The data context property specifies the default object that will
        /// be used for data binding.
        /// </remarks>
        public object? DataContext
        {
            get => GetValue(DataContextProperty);
            set => SetValue(DataContextProperty, value);
        }

        /// <summary>
        /// Gets or sets the icon of the TrayIcon.
        /// </summary>
        public WindowIcon Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        /// <summary>
        /// Gets or sets the tooltip text of the TrayIcon.
        /// </summary>
        public string? ToolTipText
        {
            get => GetValue(ToolTipTextProperty);
            set => SetValue(ToolTipTextProperty, value);
        }

        /// <summary>
        /// Gets or sets the visibility of the TrayIcon.
        /// </summary>
        public bool IsVisible
        {
            get => GetValue(IsVisibleProperty);
            set => SetValue(IsVisibleProperty, value);
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);

            if(change.Property == IconProperty)
            {
                _impl.SetIcon(Icon.PlatformImpl);
            }
        }
    }
}

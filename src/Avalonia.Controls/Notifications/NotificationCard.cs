using System;
using System.Linq;
using Avalonia.Reactive;
using Avalonia.Controls.Metadata;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;

namespace Avalonia.Controls.Notifications
{
    /// <summary>
    /// Control that represents and displays a notification.
    /// </summary>
    [PseudoClasses(":error", ":information", ":success", ":warning")]
    public class NotificationCard : ContentControl
    {
        private bool _isClosing;

        static NotificationCard()
        {
            CloseOnClickProperty.Changed.AddClassHandler<Button>(OnCloseOnClickPropertyChanged);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationCard"/> class.
        /// </summary>
        public NotificationCard()
        {
            UpdateNotificationType();
        }

        /// <summary>
        /// Determines if the notification is already closing.
        /// </summary>
        public bool IsClosing
        {
            get => _isClosing;
            private set => SetAndRaise(IsClosingProperty, ref _isClosing, value);
        }

        /// <summary>
        /// Defines the <see cref="IsClosing"/> property.
        /// </summary>
        public static readonly DirectProperty<NotificationCard, bool> IsClosingProperty =
            AvaloniaProperty.RegisterDirect<NotificationCard, bool>(nameof(IsClosing), o => o.IsClosing);

        /// <summary>
        /// Determines if the notification is closed.
        /// </summary>
        public bool IsClosed
        {
            get => GetValue(IsClosedProperty);
            set => SetValue(IsClosedProperty, value);
        }

        /// <summary>
        /// Defines the <see cref="IsClosed"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsClosedProperty =
            AvaloniaProperty.Register<NotificationCard, bool>(nameof(IsClosed));

        /// <summary>
        /// Gets or sets the type of the notification
        /// </summary>
        public NotificationType NotificationType
        {
            get => GetValue(NotificationTypeProperty);
            set => SetValue(NotificationTypeProperty, value);
        }

        /// <summary>
        /// Defines the <see cref="NotificationType" /> property
        /// </summary>
        public static readonly StyledProperty<NotificationType> NotificationTypeProperty =
            AvaloniaProperty.Register<NotificationCard, NotificationType>(nameof(NotificationType));

        /// <summary>
        /// Defines the <see cref="NotificationClosed"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> NotificationClosedEvent =
            RoutedEvent.Register<NotificationCard, RoutedEventArgs>(nameof(NotificationClosed), RoutingStrategies.Bubble);


        /// <summary>
        /// Raised when the <see cref="NotificationCard"/> has closed.
        /// </summary>
        public event EventHandler<RoutedEventArgs>? NotificationClosed
        {
            add => AddHandler(NotificationClosedEvent, value);
            remove => RemoveHandler(NotificationClosedEvent, value);
        }

        public static bool GetCloseOnClick(Button obj)
        {
            _ = obj ?? throw new ArgumentNullException(nameof(obj));
            return (bool)obj.GetValue(CloseOnClickProperty);
        }

        public static void SetCloseOnClick(Button obj, bool value)
        {
            _ = obj ?? throw new ArgumentNullException(nameof(obj));
            obj.SetValue(CloseOnClickProperty, value);
        }

        /// <summary>
        /// Defines the CloseOnClick property.
        /// </summary>
        public static readonly AttachedProperty<bool> CloseOnClickProperty =
          AvaloniaProperty.RegisterAttached<NotificationCard, Button, bool>("CloseOnClick", defaultValue: false);

        private static void OnCloseOnClickPropertyChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            var button = (Button)d;
            var value = (bool)e.NewValue!;
            if (value)
            {
                button.Click += Button_Click;
            }
            else
            {
                button.Click -= Button_Click;
            }
        }

        /// <summary>
        /// Called when a button inside the Notification is clicked.
        /// </summary>
        private static void Button_Click(object? sender, RoutedEventArgs e)
        {
            var btn = sender as ILogical;
            var notification = btn?.GetLogicalAncestors().OfType<NotificationCard>().FirstOrDefault();
            notification?.Close();
        }

        /// <summary>
        /// Closes the <see cref="NotificationCard"/>.
        /// </summary>
        public void Close()
        {
            if (IsClosing)
            {
                return;
            }

            IsClosing = true;
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == ContentProperty && e.NewValue is INotification notification)
            {
                SetValue(NotificationTypeProperty, notification.Type);
            }

            if (e.Property == NotificationTypeProperty)
            {
                UpdateNotificationType();
            }

            if (e.Property == IsClosedProperty)
            {
                if (!IsClosing && !IsClosed)
                {
                    return;
                }

                RaiseEvent(new RoutedEventArgs(NotificationClosedEvent));
            }
        }

        private void UpdateNotificationType()
        {
            switch (NotificationType)
            {
                case NotificationType.Error:
                    PseudoClasses.Add(":error");
                    break;

                case NotificationType.Information:
                    PseudoClasses.Add(":information");
                    break;

                case NotificationType.Success:
                    PseudoClasses.Add(":success");
                    break;

                case NotificationType.Warning:
                    PseudoClasses.Add(":warning");
                    break;
            }
        }
    }
}

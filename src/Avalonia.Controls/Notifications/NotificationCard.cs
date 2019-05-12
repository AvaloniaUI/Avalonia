// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;

namespace Avalonia.Controls.Notifications
{
    /// <summary>
    /// Control that represents and displays a notification.
    /// </summary>
    public class NotificationCard : ContentControl
    {
        private bool _isClosed;
        private bool _isClosing;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationCard"/> class.
        /// </summary>
        public NotificationCard()
        {
            this.GetObservable(IsClosedProperty)
                .Subscribe(x =>
                {
                    if (!IsClosing && !IsClosed)
                    {
                        return;
                    }

                    RaiseEvent(new RoutedEventArgs(NotificationClosedEvent));
                });

            this.GetObservable(ContentProperty)
                .OfType<Notification>()
                .Subscribe(x =>
                {
                    switch (x.Type)
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
                });
        }

        /// <summary>
        /// Determines if the notification is already closing.
        /// </summary>
        public bool IsClosing
        {
            get { return _isClosing; }
            private set { SetAndRaise(IsClosingProperty, ref _isClosing, value); }
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
            get { return _isClosed; }
            set { SetAndRaise(IsClosedProperty, ref _isClosed, value); }
        }

        /// <summary>
        /// Defines the <see cref="IsClosed"/> property.
        /// </summary>
        public static readonly DirectProperty<NotificationCard, bool> IsClosedProperty =
            AvaloniaProperty.RegisterDirect<NotificationCard, bool>(nameof(IsClosed), o => o.IsClosed, (o, v) => o.IsClosed = v);

        /// <summary>
        /// Defines the <see cref="NotificationClosed"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> NotificationClosedEvent =
            RoutedEvent.Register<NotificationCard, RoutedEventArgs>(nameof(NotificationClosed), RoutingStrategies.Bubble);


        /// <summary>
        /// Raised when the <see cref="NotificationCard"/> has closed.
        /// </summary>
        public event EventHandler<RoutedEventArgs> NotificationClosed
        {
            add { AddHandler(NotificationClosedEvent, value); }
            remove { RemoveHandler(NotificationClosedEvent, value); }
        }

        public static bool GetCloseOnClick(Button obj)
        {
            return (bool)obj.GetValue(CloseOnClickProperty);
        }

        public static void SetCloseOnClick(Button obj, bool value)
        {
            obj.SetValue(CloseOnClickProperty, value);
        }

        /// <summary>
        /// Defines the CloseOnClick property.
        /// </summary>
        public static readonly AvaloniaProperty CloseOnClickProperty =
          AvaloniaProperty.RegisterAttached<Button, bool>("CloseOnClick", typeof(NotificationCard), validate: CloseOnClickChanged);

        private static bool CloseOnClickChanged(Button button, bool value)
        {
            if (value)
            {
                button.Click += Button_Click;
            }
            else
            {
                button.Click -= Button_Click;
            }

            return true;
        }

        /// <summary>
        /// Called when a button inside the Notification is clicked.
        /// </summary>
        private static void Button_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as ILogical;
            var notification = btn.GetLogicalAncestors().OfType<NotificationCard>().FirstOrDefault();
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
    }
}

using System;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;

namespace Avalonia.Controls.Notifications
{
    public class Notification : ContentControl
    {
        static Notification()
        {
            IsClosedProperty.Changed.AddClassHandler<Notification>(IsClosedChanged);
        }

        public Notification()
        {
            this.GetObservable(ContentProperty)
                .OfType<NotificationContent>()
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

        private bool _isClosing;

        /// <summary>
        /// Determines if the notification is already closing.
        /// </summary>
        public bool IsClosing
        {
            get { return _isClosing; }
            private set { SetAndRaise(IsClosingProperty, ref _isClosing, value); }
        }

        public static readonly DirectProperty<Notification, bool> IsClosingProperty =
            AvaloniaProperty.RegisterDirect<Notification, bool>(nameof(IsClosing), o => o.IsClosing);

        private bool _isClosed;

        /// <summary>
        /// Determines if the notification is closed.
        /// </summary>
        public bool IsClosed
        {
            get { return _isClosed; }
            set { SetAndRaise(IsClosedProperty, ref _isClosed, value); }
        }

        public static readonly DirectProperty<Notification, bool> IsClosedProperty =
            AvaloniaProperty.RegisterDirect<Notification, bool>(nameof(IsClosed), o => o.IsClosed, (o, v) => o.IsClosed = v);

        /// <summary>
        /// Defines the <see cref="NotificationCloseInvoked"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> NotificationCloseInvokedEvent =
            RoutedEvent.Register<Notification, RoutedEventArgs>(nameof(NotificationCloseInvoked), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="NotificationClosed"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> NotificationClosedEvent =
            RoutedEvent.Register<Notification, RoutedEventArgs>(nameof(NotificationClosed), RoutingStrategies.Bubble);

        /// <summary>
        /// Raised when notification close event is invoked.
        /// </summary>
        public event EventHandler<RoutedEventArgs> NotificationCloseInvoked
        {
            add { AddHandler(NotificationCloseInvokedEvent, value); }
            remove { RemoveHandler(NotificationCloseInvokedEvent, value); }
        }

        public event EventHandler<RoutedEventArgs> NotificationClosed
        {
            add { AddHandler(NotificationClosedEvent, value); }
            remove { RemoveHandler(NotificationClosedEvent, value); }
        }

        public static bool GetCloseOnClick(Notification obj)
        {
            return (bool)obj.GetValue(CloseOnClickProperty);
        }

        public static void SetCloseOnClick(Notification obj, bool value)
        {
            obj.SetValue(CloseOnClickProperty, value);
        }

        public static readonly AvaloniaProperty CloseOnClickProperty =
          AvaloniaProperty.RegisterAttached<Button, bool>("CloseOnClick", typeof(Notification), validate: CloseOnClickChanged);

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

        private static void Button_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as ILogical;
            var notification = btn.GetLogicalAncestors().OfType<Notification>().FirstOrDefault();
            notification?.Close();
        }

        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            base.OnTemplateApplied(e);
            var closeButton = this.FindControl<Button>("PART_CloseButton");
            if (closeButton != null)
                closeButton.Click += OnCloseButtonOnClick;

        }

        private void OnCloseButtonOnClick(object sender, RoutedEventArgs args)
        {
            var button = sender as Button;
            if (button == null)
                return;

            button.Click -= OnCloseButtonOnClick;
            Close();
        }

        public void Close()
        {
            if (IsClosing)
            {
                return;
            }

            IsClosing = true;

            RaiseEvent(new RoutedEventArgs(NotificationCloseInvokedEvent));
        }

        private static void IsClosedChanged(Notification target, AvaloniaPropertyChangedEventArgs arg2)
        {
            if (!target.IsClosing & !target.IsClosed)
            {
                return;
            }

            target.RaiseEvent(new RoutedEventArgs(NotificationClosedEvent));
        }
    }
}

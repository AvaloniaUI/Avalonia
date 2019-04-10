using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace Avalonia.Controls.Notifications
{
    public class Notification : ContentControl
    {
        private TimeSpan _closingAnimationTime = TimeSpan.Zero;

        static Notification()
        {
            //CloseOnClickProperty.Changed.AddClassHandler<Button>(CloseOnClickChanged);
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

        public bool IsClosing { get; set; }

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

        /*public static bool GetCloseOnClick(Notification obj)
        {
            return (bool)obj.GetValue(CloseOnClickProperty);
        }

        public static void SetCloseOnClick(Notification obj, bool value)
        {
            obj.SetValue(CloseOnClickProperty, value);
        }*/

        //public static readonly AvaloniaProperty CloseOnClickProperty =
          //  AvaloniaProperty.RegisterDirect<Notification, bool>("CloseOnClick", GetCloseOnClick, SetCloseOnClick);

        private static void CloseOnClickChanged(Button dependencyObject, AvaloniaPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var button = dependencyObject as Button;
            if (button == null)
            {
                return;
            }

            var value = (bool)dependencyPropertyChangedEventArgs.NewValue;

            if (value)
            {
                button.Click += (sender, args) =>
                {
                    var notification = button.Parent as Notification;
                    notification?.Close();
                };
            }
        }

        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            base.OnTemplateApplied(e);
            var closeButton = this.FindControl<Button>("PART_CloseButton");
            if (closeButton != null)
                closeButton.Click += OnCloseButtonOnClick;

            //var storyboards = Template.Triggers.OfType<EventTrigger>().FirstOrDefault(t => t.RoutedEvent == NotificationCloseInvokedEvent)?.Actions.OfType<BeginStoryboard>().Select(a => a.Storyboard);
            //_closingAnimationTime = new TimeSpan(storyboards?.Max(s => Math.Min((s.Duration.HasTimeSpan ? s.Duration.TimeSpan + (s.BeginTime ?? TimeSpan.Zero) : TimeSpan.MaxValue).Ticks, s.Children.Select(ch => ch.Duration.TimeSpan + (s.BeginTime ?? TimeSpan.Zero)).Max().Ticks)) ?? 0);

        }

        private void OnCloseButtonOnClick(object sender, RoutedEventArgs args)
        {
            var button = sender as Button;
            if (button == null)
                return;

            button.Click -= OnCloseButtonOnClick;
            Close();
        }

        public async void Close()
        {
            if (IsClosing)
            {
                return;
            }

            IsClosing = true;

            RaiseEvent(new RoutedEventArgs(NotificationCloseInvokedEvent));
            await Task.Delay(_closingAnimationTime);
            RaiseEvent(new RoutedEventArgs(NotificationClosedEvent));
        }
    }
}

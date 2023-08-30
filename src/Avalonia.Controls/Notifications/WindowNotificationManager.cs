using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Notifications
{
    /// <summary>
    /// An <see cref="INotificationManager"/> that displays notifications in a <see cref="Window"/>.
    /// </summary>
    [TemplatePart("PART_Items", typeof(Panel))]
    [PseudoClasses(":topleft", ":topright", ":bottomleft", ":bottomright")]
    public class WindowNotificationManager : TemplatedControl, IManagedNotificationManager
    {
        private IList? _items;
        private AdornerLayer? adornerLayer;

        /// <summary>
        /// Defines the <see cref="Position"/> property.
        /// </summary>
        public static readonly StyledProperty<NotificationPosition> PositionProperty =
          AvaloniaProperty.Register<WindowNotificationManager, NotificationPosition>(nameof(Position), NotificationPosition.TopRight);

        /// <summary>
        /// Defines which corner of the screen notifications can be displayed in.
        /// </summary>
        /// <seealso cref="NotificationPosition"/>
        public NotificationPosition Position
        {
            get { return GetValue(PositionProperty); }
            set { SetValue(PositionProperty, value); }
        }

        /// <summary>
        /// Defines the <see cref="MaxItems"/> property.
        /// </summary>
        public static readonly StyledProperty<int> MaxItemsProperty =
          AvaloniaProperty.Register<WindowNotificationManager, int>(nameof(MaxItems), 5);

        /// <summary>
        /// Defines the maximum number of notifications visible at once.
        /// </summary>
        public int MaxItems
        {
            get { return GetValue(MaxItemsProperty); }
            set { SetValue(MaxItemsProperty, value); }
        }

        /// <summary>
        /// Defines the <see cref="Host" /> property
        /// </summary>
        public static readonly DirectProperty<WindowNotificationManager, Visual?> HostProperty =
            AvaloniaProperty.RegisterDirect<WindowNotificationManager, Visual?>(
                nameof(Host),
                o => o.Host,
                (o, v) => o.Host = v);

        private Visual? _Host;

        /// <summary>
        /// The Host that this NotificationManger should register to. If the Host is null, the Parent will be used.
        /// </summary>
        public Visual? Host
        {
            get { return _Host; }
            set { SetAndRaise(HostProperty, ref _Host, value); }
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="WindowNotificationManager"/> class.
        /// </summary>
        /// <param name="host">The TopLevel that will host the control.</param>
        public WindowNotificationManager(TopLevel? host) : this()
        {
            Host = host;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowNotificationManager"/> class.
        /// </summary>
        public WindowNotificationManager()
        {
            UpdatePseudoClasses(Position);
        }

        static WindowNotificationManager()
        {
            HorizontalAlignmentProperty.OverrideDefaultValue<WindowNotificationManager>(Layout.HorizontalAlignment.Stretch);
            VerticalAlignmentProperty.OverrideDefaultValue<WindowNotificationManager>(Layout.VerticalAlignment.Stretch);
        }

        /// <inheritdoc/>
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            
            var itemsControl = e.NameScope.Find<Panel>("PART_Items");
            _items = itemsControl?.Children;
        }

        /// <inheritdoc/>
        public void Show(INotification content)
        {
            Show(content, content.Type, content.Expiration, content.OnClick, content.OnClose);
        }

        /// <inheritdoc/>
        public async void Show(object content)
        {
            if (content is INotification notification)
            {
                Show(notification, notification.Type, notification.Expiration, notification.OnClick, notification.OnClose);
            }
            else
            {
                Show(content, NotificationType.Information);
            }
        }
        
        /// <summary>
        /// Shows a Notification
        /// </summary>
        /// <param name="content">the content of the notification</param>
        /// <param name="type">the type of the notification</param>
        /// <param name="expiration">the expiration time of the notification after which it will automatically close. If the value is Zero then the notification will remain open until the user closes it</param>
        /// <param name="onClick">an Action to be run when the notification is clicked</param>
        /// <param name="onClose">an Action to be run when the notification is closed</param>
        /// <param name="classes">style classes to apply</param>
        public async void Show(object content, 
            NotificationType type, 
            TimeSpan? expiration = null,
            Action? onClick = null, 
            Action? onClose = null, 
            string[]? classes = null)
        {
            var notificationControl = new NotificationCard
            {
                Content = content,
                NotificationType = type
            };

            // Add style classes if any
            if (classes != null)
            {
                foreach (var @class in classes)
                {
                    notificationControl.Classes.Add(@class);
                }
            }
            
            notificationControl.NotificationClosed += (sender, args) =>
            {
                onClose?.Invoke();

                _items?.Remove(sender);
            };

            notificationControl.PointerPressed += (sender, args) =>
            {
                onClick?.Invoke();

                (sender as NotificationCard)?.Close();
            };

            Dispatcher.UIThread.Post(() =>
            {
                _items?.Add(notificationControl);

                if (_items?.OfType<NotificationCard>().Count(i => !i.IsClosing) > MaxItems)
                {
                    _items.OfType<NotificationCard>().First(i => !i.IsClosing).Close();
                }
            });

            if (expiration == TimeSpan.Zero)
            {
                return;
            }

            await Task.Delay(expiration ?? TimeSpan.FromSeconds(5));

            notificationControl.Close();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == PositionProperty)
            {
                UpdatePseudoClasses(change.GetNewValue<NotificationPosition>());
            }

            if (change.Property == HostProperty)
            {
                Install();
            }
        }

        /// <summary>
        /// Installs the <see cref="WindowNotificationManager"/> within the <see cref="AdornerLayer"/>
        /// </summary>
        private void Install()
        {
            // unregister from AdornerLayer if this control was already installed
            if (adornerLayer is not null && !adornerLayer.Children.Contains(this))
            {
                adornerLayer.Children.Remove(this);
            }
            
            // Try to get the host. If host was null, use the TopLevel instead.
            var host = Host ?? Parent as Visual;

            if (host is null) throw new InvalidOperationException("NotificationControl cannot be installed. Host was not found.");

            adornerLayer = host is TopLevel 
                ? host.FindDescendantOfType<VisualLayerManager>()?.AdornerLayer 
                : AdornerLayer.GetAdornerLayer(host);

            if (adornerLayer is not null)
            {
                adornerLayer.Children.Add(this);
                AdornerLayer.SetAdornedElement(this, adornerLayer);
            }
        }

        private void UpdatePseudoClasses(NotificationPosition position)
        {
            PseudoClasses.Set(":topleft", position == NotificationPosition.TopLeft);
            PseudoClasses.Set(":topright", position == NotificationPosition.TopRight);
            PseudoClasses.Set(":bottomleft", position == NotificationPosition.BottomLeft);
            PseudoClasses.Set(":bottomright", position == NotificationPosition.BottomRight);
        }
    }
}

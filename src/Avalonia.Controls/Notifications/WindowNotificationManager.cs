using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
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
    [PseudoClasses(":topleft", ":topright", ":bottomleft", ":bottomright", ":topcenter", ":bottomcenter")]
    public class WindowNotificationManager : TemplatedControl, IManagedNotificationManager
    {
        private IList? _items;
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
            get => GetValue(PositionProperty);
            set => SetValue(PositionProperty, value);
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
            get => GetValue(MaxItemsProperty);
            set => SetValue(MaxItemsProperty, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowNotificationManager"/> class.
        /// </summary>
        /// <param name="host">The TopLevel that will host the control.</param>
        public WindowNotificationManager(TopLevel? host) : this()
        {
            if (host is not null)
            {
                InstallFromTopLevel(host);
            }
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
        public void Show(object content)
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
            Dispatcher.UIThread.VerifyAccess();
            
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
        }

        /// <summary>
        /// Installs the <see cref="WindowNotificationManager"/> within the <see cref="AdornerLayer"/>
        /// </summary>
        private void InstallFromTopLevel(TopLevel topLevel)
        {
            topLevel.TemplateApplied += TopLevelOnTemplateApplied;
            var adorner = topLevel.FindDescendantOfType<VisualLayerManager>()?.AdornerLayer;
            if (adorner is not null)
            {
                adorner.Children.Add(this);
                AdornerLayer.SetAdornedElement(this, adorner);
            }
        }

        private void TopLevelOnTemplateApplied(object? sender, TemplateAppliedEventArgs e)
        {
            if (Parent is AdornerLayer adornerLayer)
            {
                adornerLayer.Children.Remove(this);
                AdornerLayer.SetAdornedElement(this, null);
            }
            
            // Reinstall notification manager on template reapplied.
            var topLevel = (TopLevel)sender!;
            topLevel.TemplateApplied -= TopLevelOnTemplateApplied;
            InstallFromTopLevel(topLevel);
        }

        private void UpdatePseudoClasses(NotificationPosition position)
        {
            PseudoClasses.Set(":topleft", position == NotificationPosition.TopLeft);
            PseudoClasses.Set(":topright", position == NotificationPosition.TopRight);
            PseudoClasses.Set(":bottomleft", position == NotificationPosition.BottomLeft);
            PseudoClasses.Set(":bottomright", position == NotificationPosition.BottomRight);
            PseudoClasses.Set(":topcenter", position == NotificationPosition.TopCenter);
            PseudoClasses.Set(":bottomcenter", position == NotificationPosition.BottomCenter);
        }
    }
}

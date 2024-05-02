using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Avalonia.Collections;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Platform;
using Avalonia.Platform;
using Avalonia.Reactive;

namespace Avalonia.Controls
{
    public sealed class TrayIcons : AvaloniaList<TrayIcon>
    {
    }

    public class TrayIcon : AvaloniaObject, INativeMenuExporterProvider, IDisposable
    {
        private readonly ITrayIconImpl? _impl;

        private TrayIcon(ITrayIconImpl? impl)
        {
            if (impl != null)
            {
                _impl = impl;

                _impl.SetIsVisible(IsVisible);

                _impl.OnClicked = () =>
                {
                    Clicked?.Invoke(this, EventArgs.Empty);
                    
                    if (Command?.CanExecute(CommandParameter) == true)
                    {
                        Command.Execute(CommandParameter);
                    }
                };
            }
        }

        public TrayIcon() : this(PlatformManager.CreateTrayIcon())
        {
        }

        static TrayIcon()
        {
            IconsProperty.Changed.Subscribe(args =>
            {
                if (args.Sender is Application)
                {
                    if (args.OldValue.Value != null)
                    {
                        RemoveIcons(args.OldValue.Value);
                    }

                    if (args.NewValue.Value != null)
                    {
                        args.NewValue.Value.CollectionChanged += Icons_CollectionChanged;
                    }
                }
                else
                {
                    throw new InvalidOperationException("TrayIcon.Icons must be set on the Application.");
                }
            });

            var app = Application.Current ?? throw new InvalidOperationException("Application not yet initialized.");

            if (app.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            {
                lifetime.Exit += Lifetime_Exit;
            }
        }

        /// <summary>
        /// Raised when the TrayIcon is clicked.
        /// Note, this is only supported on Win32 and some Linux DEs, 
        /// on OSX this event is not raised.
        /// </summary>
        public event EventHandler? Clicked;
        
        /// <summary>
        /// Defines the <see cref="Command"/> property.
        /// </summary>
        public static readonly StyledProperty<ICommand?> CommandProperty =
            Button.CommandProperty.AddOwner<TrayIcon>(new(enableDataValidation: true));

        /// <summary>
        /// Defines the <see cref="CommandParameter"/> property.
        /// </summary>
        public static readonly StyledProperty<object?> CommandParameterProperty =
            Button.CommandParameterProperty.AddOwner<TrayIcon>();

        /// <summary>
        /// Defines the <see cref="TrayIcons"/> attached property.
        /// </summary>
        public static readonly AttachedProperty<TrayIcons?> IconsProperty
            = AvaloniaProperty.RegisterAttached<TrayIcon, Application, TrayIcons?>("Icons");

        /// <summary>
        /// Defines the <see cref="Menu"/> property.
        /// </summary>
        public static readonly StyledProperty<NativeMenu?> MenuProperty
            = AvaloniaProperty.Register<TrayIcon, NativeMenu?>(nameof(Menu));

        /// <summary>
        /// Defines the <see cref="Icon"/> property.
        /// </summary>
        public static readonly StyledProperty<WindowIcon?> IconProperty =
            Window.IconProperty.AddOwner<TrayIcon>();

        /// <summary>
        /// Defines the <see cref="ToolTipText"/> property.
        /// </summary>
        public static readonly StyledProperty<string?> ToolTipTextProperty =
            AvaloniaProperty.Register<TrayIcon, string?>(nameof(ToolTipText));

        /// <summary>
        /// Defines the <see cref="IsVisible"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsVisibleProperty =
            Visual.IsVisibleProperty.AddOwner<TrayIcon>();

        public static void SetIcons(Application o, TrayIcons? trayIcons) => o.SetValue(IconsProperty, trayIcons);

        public static TrayIcons? GetIcons(Application o) => o.GetValue(IconsProperty);
        
        /// <summary>
        /// Gets or sets the <see cref="Command"/> property of a TrayIcon.
        /// </summary>
        public ICommand? Command
        {
            get => GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        /// <summary>
        /// Gets or sets the parameter to pass to the <see cref="Command"/> property of a
        /// <see cref="TrayIcon"/>.
        /// </summary>
        public object? CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        /// <summary>
        /// Gets or sets the Menu of the TrayIcon.
        /// </summary>
        public NativeMenu? Menu
        {
            get => GetValue(MenuProperty);
            set => SetValue(MenuProperty, value);
        }

        /// <summary>
        /// Gets or sets the icon of the TrayIcon.
        /// </summary>
        public WindowIcon? Icon
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

        public INativeMenuExporter? NativeMenuExporter => _impl?.MenuExporter;

        internal ITrayIconImpl? Impl => _impl;
        
        private static void Lifetime_Exit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
        {
            var app = Application.Current ?? throw new InvalidOperationException("Application not yet initialized.");
            var trayIcons = GetIcons(app);

            if (trayIcons != null)
            {
                RemoveIcons(trayIcons);
            }
        }

        private static void Icons_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems is not null)
                RemoveIcons(e.OldItems.Cast<TrayIcon>());
        }

        private static void RemoveIcons(IEnumerable<TrayIcon> icons)
        {
            foreach (var icon in icons)
            {
                icon.Dispose();
            }
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == IconProperty)
            {
                _impl?.SetIcon(Icon?.PlatformImpl);
            }
            else if (change.Property == IsVisibleProperty)
            {
                _impl?.SetIsVisible(change.GetNewValue<bool>());
            }
            else if (change.Property == ToolTipTextProperty)
            {
                _impl?.SetToolTipText(change.GetNewValue<string?>());
            }
            else if (change.Property == MenuProperty)
            {
                _impl?.MenuExporter?.SetNativeMenu(change.GetNewValue<NativeMenu?>());
            }
        }

        /// <summary>
        /// Disposes the tray icon (removing it from the tray area).
        /// </summary>
        public void Dispose() => _impl?.Dispose();
    }
}

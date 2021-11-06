﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Avalonia.Collections;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Platform;
using Avalonia.Platform;
using Avalonia.Utilities;

#nullable enable

namespace Avalonia.Controls
{
    public sealed class TrayIcons : AvaloniaList<TrayIcon>
    {
    }
    
    

    public class TrayIcon : AvaloniaObject, INativeMenuExporterProvider, IDisposable
    {
        private readonly ITrayIconImpl? _impl;
        private ICommand? _command;

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
            });

            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
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
        public static readonly DirectProperty<TrayIcon, ICommand?> CommandProperty =
            Button.CommandProperty.AddOwner<TrayIcon>(
                trayIcon => trayIcon.Command,
                (trayIcon, command) => trayIcon.Command = command,
                enableDataValidation: true);

        /// <summary>
        /// Defines the <see cref="CommandParameter"/> property.
        /// </summary>
        public static readonly StyledProperty<object?> CommandParameterProperty =
            Button.CommandParameterProperty.AddOwner<MenuItem>();

        /// <summary>
        /// Defines the <see cref="TrayIcons"/> attached property.
        /// </summary>
        public static readonly AttachedProperty<TrayIcons> IconsProperty
            = AvaloniaProperty.RegisterAttached<TrayIcon, Application, TrayIcons>("Icons");

        /// <summary>
        /// Defines the <see cref="Menu"/> property.
        /// </summary>
        public static readonly StyledProperty<NativeMenu?> MenuProperty
            = AvaloniaProperty.Register<TrayIcon, NativeMenu?>(nameof(Menu));

        /// <summary>
        /// Defines the <see cref="Icon"/> property.
        /// </summary>
        public static readonly StyledProperty<WindowIcon> IconProperty =
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

        public static void SetIcons(AvaloniaObject o, TrayIcons trayIcons) => o.SetValue(IconsProperty, trayIcons);

        public static TrayIcons GetIcons(AvaloniaObject o) => o.GetValue(IconsProperty);
        
        /// <summary>
        /// Gets or sets the <see cref="Command"/> property of a TrayIcon.
        /// </summary>
        public ICommand? Command
        {
            get => _command;
            set => SetAndRaise(CommandProperty, ref _command, value);
        }

        /// <summary>
        /// Gets or sets the parameter to pass to the <see cref="Command"/> property of a
        /// <see cref="TrayIcon"/>.
        /// </summary>
        public object CommandParameter
        {
            get { return GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
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

        public INativeMenuExporter? NativeMenuExporter => _impl?.MenuExporter;

        private static void Lifetime_Exit(object sender, ControlledApplicationLifetimeExitEventArgs e)
        {
            var trayIcons = GetIcons(Application.Current);

            RemoveIcons(trayIcons);
        }

        private static void Icons_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RemoveIcons(e.OldItems.Cast<TrayIcon>());
        }

        private static void RemoveIcons(IEnumerable<TrayIcon> icons)
        {
            foreach (var icon in icons)
            {
                icon.Dispose();
            }
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == IconProperty)
            {
                _impl?.SetIcon(Icon.PlatformImpl);
            }
            else if (change.Property == IsVisibleProperty)
            {
                _impl?.SetIsVisible(change.NewValue.GetValueOrDefault<bool>());
            }
            else if (change.Property == ToolTipTextProperty)
            {
                _impl?.SetToolTipText(change.NewValue.GetValueOrDefault<string?>());
            }
            else if (change.Property == MenuProperty)
            {
                _impl?.MenuExporter?.SetNativeMenu(change.NewValue.GetValueOrDefault<NativeMenu>());
            }
        }

        /// <summary>
        /// Disposes the tray icon (removing it from the tray area).
        /// </summary>
        public void Dispose() => _impl?.Dispose();
    }
}

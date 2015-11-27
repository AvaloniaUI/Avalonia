// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using System.Threading;
using Perspex.Controls.Platform;
using Perspex.Input.Platform;
using Perspex.Input;
using Perspex.Platform;
using Perspex.Shared.PlatformSupport;

namespace Perspex.Gtk
{
    using Gtk = global::Gtk;

    public class GtkPlatform : IPlatformThreadingInterface, IPlatformSettings, IWindowingPlatform
    {
        private static readonly GtkPlatform s_instance = new GtkPlatform();
        private static Thread _uiThread;

        public GtkPlatform()
        {
            Gtk.Application.Init();
        }

        public Size DoubleClickSize => new Size(4, 4);

        public TimeSpan DoubleClickTime => TimeSpan.FromMilliseconds(Gtk.Settings.Default.DoubleClickTime);
        public double RenderScalingFactor { get; } = 1;
        public double LayoutScalingFactor { get; } = 1;

        public static void Initialize()
        {
            PerspexLocator.CurrentMutable
                .Bind<IWindowingPlatform>().ToConstant(s_instance)
                .Bind<IClipboard>().ToSingleton<ClipboardImpl>()
                .Bind<IStandardCursorFactory>().ToConstant(CursorFactory.Instance)
                .Bind<IKeyboardDevice>().ToConstant(GtkKeyboardDevice.Instance)
                .Bind<IMouseDevice>().ToConstant(GtkMouseDevice.Instance)
                .Bind<IPlatformSettings>().ToConstant(s_instance)
                .Bind<IPlatformThreadingInterface>().ToConstant(s_instance)
                .Bind<ISystemDialogImpl>().ToSingleton<SystemDialogImpl>();
            SharedPlatform.Register();
            _uiThread = Thread.CurrentThread;
        }

        public bool HasMessages()
        {
            return Gtk.Application.EventsPending();
        }

        public void ProcessMessage()
        {
            Gtk.Application.RunIteration();
        }

        public void RunLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
                Gtk.Application.RunIteration();
        }

        public IDisposable StartTimer(TimeSpan interval, Action tick)
        {
            var result = true;
            var handle = GLib.Timeout.Add(
                (uint)interval.TotalMilliseconds,
                () =>
                {
                    tick();
                    return result;
                });

            return Disposable.Create(() => result = false);
        }



        public void Signal()
        {
            Gtk.Application.Invoke(delegate { Signaled?.Invoke(); });
        }

        public bool CurrentThreadIsLoopThread => Thread.CurrentThread == _uiThread;

        public event Action Signaled;
        public IWindowImpl CreateWindow()
        {
            return new WindowImpl();
        }

        public IWindowImpl CreateEmbeddableWindow()
        {
            throw new NotSupportedException();
        }

        public IPopupImpl CreatePopup()
        {
            return new PopupImpl();
        }
    }
}
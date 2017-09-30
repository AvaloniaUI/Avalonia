// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using System.Threading;
using Avalonia.Controls.Platform;
using Avalonia.Input.Platform;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Avalonia
{
    public static class GtkApplicationExtensions
    {
        public static T UseGtk<T>(this T builder) where T : AppBuilderBase<T>, new()
        {
            builder.UseWindowingSubsystem(Gtk.GtkPlatform.Initialize, "Gtk");
            return builder;
        }
    }
}

namespace Avalonia.Gtk
{
    using System.IO;
    using Rendering;
    using Gtk = global::Gtk;

    public class GtkPlatform : IPlatformThreadingInterface, IPlatformSettings, IWindowingPlatform, IPlatformIconLoader
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
            AvaloniaLocator.CurrentMutable
                .Bind<IWindowingPlatform>().ToConstant(s_instance)
                .Bind<IClipboard>().ToSingleton<ClipboardImpl>()
                .Bind<IStandardCursorFactory>().ToConstant(CursorFactory.Instance)
                .Bind<IKeyboardDevice>().ToConstant(GtkKeyboardDevice.Instance)
                .Bind<IPlatformSettings>().ToConstant(s_instance)
                .Bind<IPlatformThreadingInterface>().ToConstant(s_instance)
                .Bind<IRenderLoop>().ToConstant(new DefaultRenderLoop(60))
                .Bind<ISystemDialogImpl>().ToSingleton<SystemDialogImpl>()
                .Bind<IPlatformIconLoader>().ToConstant(s_instance);
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



        public void Signal(DispatcherPriority prio)
        {
            Gtk.Application.Invoke(delegate { Signaled?.Invoke(null); });
        }

        public bool CurrentThreadIsLoopThread => Thread.CurrentThread == _uiThread;

        public event Action<DispatcherPriority?> Signaled;
        public IWindowImpl CreateWindow()
        {
            return new WindowImpl();
        }

        public IEmbeddableWindowImpl CreateEmbeddableWindow() => new EmbeddableImpl();

        public IPopupImpl CreatePopup()
        {
            return new PopupImpl();
        }

        public IWindowIconImpl LoadIcon(string fileName)
        {
            return new IconImpl(new Gdk.Pixbuf(fileName));
        }

        public IWindowIconImpl LoadIcon(Stream stream)
        {
            return new IconImpl(new Gdk.Pixbuf(stream));
        }

        public IWindowIconImpl LoadIcon(IBitmapImpl bitmap)
        {
            if (bitmap is Gdk.Pixbuf)
            {
                return new IconImpl((Gdk.Pixbuf)bitmap);
            }
            else
            {
                using (var memoryStream = new MemoryStream())
                {
                    bitmap.Save(memoryStream);
                    return new IconImpl(new Gdk.Pixbuf(memoryStream));
                } 
            }
        }
    }
}
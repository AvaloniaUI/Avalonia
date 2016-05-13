// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Input;
using System;

namespace Avalonia.Controls
{
    public class AppBuilder
    {
        public Application Instance { get; set; }

        public Action WindowingSubsystem { get; set; }

        public Action RenderingSubsystem { get; set; }

        public Action<AppBuilder> BeforeStartCallback { get; set; }

        public static AppBuilder Configure<TApp>()
            where TApp : Application, new()
        {
            return Configure(new TApp());
        }

        public static AppBuilder Configure(Application app)
        {
            AvaloniaLocator.CurrentMutable.BindToSelf(app);

            return new AppBuilder()
            {
                Instance = app,
            };
        }

        public AppBuilder BeforeStarting(Action<AppBuilder> callback)
        {
            BeforeStartCallback = callback;
            return this;
        }

        public void Start<TMainWindow>()
            where TMainWindow : Window, new()
        {
            Setup();
            BeforeStartCallback?.Invoke(this);

            var window = new TMainWindow();
            window.Show();
            Instance.Run(window);
        }

        public AppBuilder SetupWithoutStarting()
        {
            Setup();
            return this;
        }

        public AppBuilder WithWindowingSubsystem(Action initializer)
        {
            WindowingSubsystem = initializer;
            return this;
        }

        public AppBuilder WithRenderingSubsystem(Action initializer)
        {
            RenderingSubsystem = initializer;
            return this;
        }

        public void Setup()
        {
            if (Instance == null)
            {
                throw new InvalidOperationException("No App instance configured.");
            }

            if (WindowingSubsystem == null)
            {
                throw new InvalidOperationException("No windowing system configured.");
            }

            if (RenderingSubsystem == null)
            {
                throw new InvalidOperationException("No rendering system configured.");
            }
            Instance.RegisterServices();
            WindowingSubsystem();
            RenderingSubsystem();
            Instance.Initialize();
        }
    }
}

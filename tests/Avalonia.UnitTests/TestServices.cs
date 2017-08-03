// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Moq;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Shared.PlatformSupport;
using Avalonia.Styling;
using Avalonia.Themes.Default;
using Avalonia.Rendering;
using System.Reactive.Concurrency;
using System.Collections.Generic;

namespace Avalonia.UnitTests
{
    public class TestServices
    {
        public static readonly TestServices StyledWindow = new TestServices(
            assetLoader: new AssetLoader(),
            layoutManager: new LayoutManager(),
            platform: new AppBuilder().RuntimePlatform,
            renderInterface: new MockPlatformRenderInterface(),
            standardCursorFactory: Mock.Of<IStandardCursorFactory>(),
            styler: new Styler(),
            theme: () => CreateDefaultTheme(),
            threadingInterface: Mock.Of<IPlatformThreadingInterface>(x => x.CurrentThreadIsLoopThread == true),
            windowingPlatform: new MockWindowingPlatform());

        public static readonly TestServices MockPlatformRenderInterface = new TestServices(
            renderInterface: new MockPlatformRenderInterface());

        public static readonly TestServices MockPlatformWrapper = new TestServices(
            platform: Mock.Of<IRuntimePlatform>());

        public static readonly TestServices MockStyler = new TestServices(
            styler: Mock.Of<IStyler>());

        public static readonly TestServices MockThreadingInterface = new TestServices(
            threadingInterface: Mock.Of<IPlatformThreadingInterface>(x => x.CurrentThreadIsLoopThread == true));

        public static readonly TestServices MockWindowingPlatform = new TestServices(
            windowingPlatform: new MockWindowingPlatform());

        public static readonly TestServices RealFocus = new TestServices(
            focusManager: new FocusManager(),
            keyboardDevice: () => new KeyboardDevice(),
            keyboardNavigation: new KeyboardNavigationHandler(),
            inputManager: new InputManager());

        public static readonly TestServices RealLayoutManager = new TestServices(
            layoutManager: new LayoutManager());

        public static readonly TestServices RealStyler = new TestServices(
            styler: new Styler());

        public TestServices(
            IAssetLoader assetLoader = null,
            IFocusManager focusManager = null,
            IInputManager inputManager = null,
            Func<IKeyboardDevice> keyboardDevice = null,
            IKeyboardNavigationHandler keyboardNavigation = null,
            ILayoutManager layoutManager = null,
            Func<IMouseDevice> mouseDevice = null,
            IRuntimePlatform platform = null,
            IPlatformRenderInterface renderInterface = null,
            IRenderLoop renderLoop = null,
            IScheduler scheduler = null,
            IStandardCursorFactory standardCursorFactory = null,
            IStyler styler = null,
            Func<Styles> theme = null,
            IPlatformThreadingInterface threadingInterface = null,
            IWindowImpl windowImpl = null,
            IWindowingPlatform windowingPlatform = null)
        {
            AssetLoader = assetLoader;
            FocusManager = focusManager;
            InputManager = inputManager;
            KeyboardDevice = keyboardDevice;
            KeyboardNavigation = keyboardNavigation;
            LayoutManager = layoutManager;
            MouseDevice = mouseDevice;
            Platform = platform;
            RenderInterface = renderInterface;
            Scheduler = scheduler;
            StandardCursorFactory = standardCursorFactory;
            Styler = styler;
            Theme = theme;
            ThreadingInterface = threadingInterface;
            WindowImpl = windowImpl;
            WindowingPlatform = windowingPlatform;
        }

        public IAssetLoader AssetLoader { get; }
        public IInputManager InputManager { get; }
        public IFocusManager FocusManager { get; }
        public Func<IKeyboardDevice> KeyboardDevice { get; }
        public IKeyboardNavigationHandler KeyboardNavigation { get; }
        public ILayoutManager LayoutManager { get; }
        public Func<IMouseDevice> MouseDevice { get; }
        public IRuntimePlatform Platform { get; }
        public IPlatformRenderInterface RenderInterface { get; }
        public IScheduler Scheduler { get; }
        public IStandardCursorFactory StandardCursorFactory { get; }
        public IStyler Styler { get; }
        public Func<Styles> Theme { get; }
        public IPlatformThreadingInterface ThreadingInterface { get; }
        public IWindowImpl WindowImpl { get; }
        public IWindowingPlatform WindowingPlatform { get; }

        public TestServices With(
            IAssetLoader assetLoader = null,
            IFocusManager focusManager = null,
            IInputManager inputManager = null,
            Func<IKeyboardDevice> keyboardDevice = null,
            IKeyboardNavigationHandler keyboardNavigation = null,
            ILayoutManager layoutManager = null,
            Func<IMouseDevice> mouseDevice = null,
            IRuntimePlatform platform = null,
            IPlatformRenderInterface renderInterface = null,
            IRenderLoop renderLoop = null,
            IScheduler scheduler = null,
            IStandardCursorFactory standardCursorFactory = null,
            IStyler styler = null,
            Func<Styles> theme = null,
            IPlatformThreadingInterface threadingInterface = null,
            IWindowImpl windowImpl = null,
            IWindowingPlatform windowingPlatform = null)
        {
            return new TestServices(
                assetLoader: assetLoader ?? AssetLoader,
                focusManager: focusManager ?? FocusManager,
                inputManager: inputManager ?? InputManager,
                keyboardDevice: keyboardDevice ?? KeyboardDevice,
                keyboardNavigation: keyboardNavigation ?? KeyboardNavigation,
                layoutManager: layoutManager ?? LayoutManager,
                mouseDevice: mouseDevice ?? MouseDevice,
                platform: platform ?? Platform,
                renderInterface: renderInterface ?? RenderInterface,
                scheduler: scheduler ?? Scheduler,
                standardCursorFactory: standardCursorFactory ?? StandardCursorFactory,
                styler: styler ?? Styler,
                theme: theme ?? Theme,
                threadingInterface: threadingInterface ?? ThreadingInterface,
                windowingPlatform: windowingPlatform ?? WindowingPlatform,
                windowImpl: windowImpl ?? WindowImpl);
        }

        private static Styles CreateDefaultTheme()
        {
            var result = new Styles
            {
                new DefaultTheme(),
            };

            var loader = new AvaloniaXamlLoader();
            var baseLight = (IStyle)loader.Load(
                new Uri("resm:Avalonia.Themes.Default.Accents.BaseLight.xaml?assembly=Avalonia.Themes.Default"));
            result.Add(baseLight);

            return result;
        }

        private static IPlatformRenderInterface CreateRenderInterfaceMock()
        {
            return Mock.Of<IPlatformRenderInterface>(x => 
                x.CreateFormattedText(
                    It.IsAny<string>(),
                    It.IsAny<Typeface>(),
                    It.IsAny<TextAlignment>(),
                    It.IsAny<TextWrapping>(),
                    It.IsAny<Size>(),
                    It.IsAny<IReadOnlyList<FormattedTextStyleSpan>>()) == Mock.Of<IFormattedTextImpl>() &&
                x.CreateStreamGeometry() == Mock.Of<IStreamGeometryImpl>(
                    y => y.Open() == Mock.Of<IStreamGeometryContextImpl>()));
        }
    }
}

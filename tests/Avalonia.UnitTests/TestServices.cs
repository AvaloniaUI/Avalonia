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

namespace Avalonia.UnitTests
{
    public class TestServices
    {
        public static readonly TestServices StyledWindow = new TestServices(
            assetLoader: new AssetLoader(),
            layoutManager: new LayoutManager(),
            platform: new StandardRuntimePlatform(),
            renderInterface: CreateRenderInterfaceMock(),
            standardCursorFactory: Mock.Of<IStandardCursorFactory>(),
            styler: new Styler(),
            theme: () => CreateDefaultTheme(),
            threadingInterface: Mock.Of<IPlatformThreadingInterface>(x => x.CurrentThreadIsLoopThread == true),
            windowingPlatform: new MockWindowingPlatform());

        public static readonly TestServices MockPlatformRenderInterface = new TestServices(
            renderInterface: CreateRenderInterfaceMock());

        public static readonly TestServices MockPlatformWrapper = new TestServices(
            platform: Mock.Of<IRuntimePlatform>());

        public static readonly TestServices MockStyler = new TestServices(
            styler: Mock.Of<IStyler>());

        public static readonly TestServices MockThreadingInterface = new TestServices(
            threadingInterface: Mock.Of<IPlatformThreadingInterface>(x => x.CurrentThreadIsLoopThread == true));

        public static readonly TestServices RealFocus = new TestServices(
            focusManager: new FocusManager(),
            keyboardDevice: () => new KeyboardDevice(),
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
            ILayoutManager layoutManager = null,
            IRuntimePlatform platform = null,
            IPlatformRenderInterface renderInterface = null,
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
            LayoutManager = layoutManager;
            Platform = platform;
            RenderInterface = renderInterface;
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
        public ILayoutManager LayoutManager { get; }
        public IRuntimePlatform Platform { get; }
        public IPlatformRenderInterface RenderInterface { get; }
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
            ILayoutManager layoutManager = null,
            IRuntimePlatform platform = null,
            IPlatformRenderInterface renderInterface = null,
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
                layoutManager: layoutManager ?? LayoutManager,
                platform: platform ?? Platform,
                renderInterface: renderInterface ?? RenderInterface,
                standardCursorFactory: standardCursorFactory ?? StandardCursorFactory,
                styler: styler ?? Styler,
                theme: theme ?? Theme,
                threadingInterface: threadingInterface ?? ThreadingInterface,
                windowImpl: windowImpl ?? WindowImpl,
                windowingPlatform: windowingPlatform ?? WindowingPlatform);
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
                    It.IsAny<string>(),
                    It.IsAny<double>(),
                    It.IsAny<FontStyle>(),
                    It.IsAny<TextAlignment>(),
                    It.IsAny<FontWeight>(),
                    It.IsAny<TextWrapping>()) == Mock.Of<IFormattedTextImpl>() &&
                x.CreateStreamGeometry() == Mock.Of<IStreamGeometryImpl>(
                    y => y.Open() == Mock.Of<IStreamGeometryContextImpl>()));
        }
    }
}

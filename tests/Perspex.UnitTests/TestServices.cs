// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Moq;
using Perspex.Input;
using Perspex.Layout;
using Perspex.Markup.Xaml;
using Perspex.Media;
using Perspex.Platform;
using Perspex.Shared.PlatformSupport;
using Perspex.Styling;
using Perspex.Themes.Default;

namespace Perspex.UnitTests
{
    public class TestServices
    {
        public static readonly TestServices StyledWindow = new TestServices(
            assetLoader: new AssetLoader(),
            layoutManager: new LayoutManager(),
            platformWrapper: new PclPlatformWrapper(),
            renderInterface: CreateRenderInterfaceMock(),
            standardCursorFactory: Mock.Of<IStandardCursorFactory>(),
            styler: new Styler(),
            theme: () => CreateDefaultTheme(),
            threadingInterface: Mock.Of<IPlatformThreadingInterface>(x => x.CurrentThreadIsLoopThread == true),
            windowingPlatform: new MockWindowingPlatform());

        public static readonly TestServices MockPlatformWrapper = new TestServices(
            platformWrapper: Mock.Of<IPclPlatformWrapper>());

        public static readonly TestServices MockStyler = new TestServices(
            styler: Mock.Of<IStyler>());

        public static readonly TestServices MockThreadingInterface = new TestServices(
            threadingInterface: Mock.Of<IPlatformThreadingInterface>(x => x.CurrentThreadIsLoopThread == true));

        public static readonly TestServices RealFocus = new TestServices(
            focusManager: new FocusManager(),
            keyboardDevice: () => new KeyboardDevice(),
            inputManager: new InputManager());

        public static readonly TestServices RealStyler = new TestServices(
            styler: new Styler());

        public TestServices(
            IAssetLoader assetLoader = null,
            IFocusManager focusManager = null,
            IInputManager inputManager = null,
            Func<IKeyboardDevice> keyboardDevice = null,
            ILayoutManager layoutManager = null,
            IPclPlatformWrapper platformWrapper = null,
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
            PlatformWrapper = platformWrapper;
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
        public IPclPlatformWrapper PlatformWrapper { get; }
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
            IPclPlatformWrapper platformWrapper = null,
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
                platformWrapper: platformWrapper ?? PlatformWrapper,
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

            var loader = new PerspexXamlLoader();
            var baseLight = (IStyle)loader.Load(
                new Uri("resm:Perspex.Themes.Default.Accents.BaseLight.xaml?assembly=Perspex.Themes.Default"));
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
                    It.IsAny<FontWeight>()) == Mock.Of<IFormattedTextImpl>() &&
                x.CreateStreamGeometry() == Mock.Of<IStreamGeometryImpl>(
                    y => y.Open() == Mock.Of<IStreamGeometryContextImpl>()));
        }
    }
}

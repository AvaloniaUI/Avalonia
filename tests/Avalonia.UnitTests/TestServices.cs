using System;
using System.Reactive.Concurrency;
using Avalonia.Animation;
using Avalonia.Harfbuzz;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.Threading;
using Moq;

namespace Avalonia.UnitTests
{
    public class TestServices
    {
        public static readonly TestServices StyledWindow = new TestServices(
            assetLoader: new StandardAssetLoader(),
            platform: new StandardRuntimePlatform(),
            renderInterface: new HeadlessPlatformRenderInterface(),
            standardCursorFactory: new HeadlessCursorFactoryStub(),
            theme: () => CreateSimpleTheme(),
            dispatcherImpl: new NullDispatcherImpl(),
            fontManagerImpl: new TestFontManager(),
            textShaperImpl: new HarfBuzzTextShaper(),
            windowingPlatform: new MockWindowingPlatform());

        public static readonly TestServices MockPlatformRenderInterface = new TestServices(
            assetLoader: new StandardAssetLoader(),
            renderInterface: new HeadlessPlatformRenderInterface(),
            fontManagerImpl: new TestFontManager(),
            textShaperImpl: new HarfBuzzTextShaper());

        public static readonly TestServices MockPlatformWrapper = new TestServices(
            platform: Mock.Of<IRuntimePlatform>());

        public static readonly TestServices MockThreadingInterface = new TestServices(
            dispatcherImpl: new NullDispatcherImpl(),
            assetLoader: new StandardAssetLoader());

        public static readonly TestServices MockWindowingPlatform = new TestServices(
            windowingPlatform: new MockWindowingPlatform());

        public static readonly TestServices RealFocus = new TestServices(
            keyboardDevice: () => new KeyboardDevice(),
            keyboardNavigation: () => new KeyboardNavigationHandler(),
            inputManager: new InputManager(),
            assetLoader: new StandardAssetLoader(),
            renderInterface: new HeadlessPlatformRenderInterface(),
            fontManagerImpl: new TestFontManager(),
            textShaperImpl: new HarfBuzzTextShaper());

        public static readonly TestServices FocusableWindow = new TestServices(
            keyboardDevice: () => new KeyboardDevice(),
            keyboardNavigation: () => new KeyboardNavigationHandler(),
            inputManager: new InputManager(),
            assetLoader: new StandardAssetLoader(),
            platform: new StandardRuntimePlatform(),
            renderInterface: new HeadlessPlatformRenderInterface(),
            standardCursorFactory: new HeadlessCursorFactoryStub(),
            theme: () => CreateSimpleTheme(),
            dispatcherImpl: new NullDispatcherImpl(),
            fontManagerImpl: new TestFontManager(),
            textShaperImpl: new HarfBuzzTextShaper(),
            windowingPlatform: new MockWindowingPlatform());

        public static readonly TestServices TextServices = new TestServices(
            assetLoader: new StandardAssetLoader(),
            renderInterface: new HeadlessPlatformRenderInterface(),
            fontManagerImpl: new TestFontManager(),
            textShaperImpl: new HarfBuzzTextShaper());

        internal TestServices(
            IAssetLoader? assetLoader = null,
            IInputManager? inputManager = null,
            IGlobalClock? globalClock = null,
            Func<IKeyboardDevice?>? keyboardDevice = null,
            Func<IKeyboardNavigationHandler?>? keyboardNavigation = null,
            Func<IMouseDevice?>? mouseDevice = null,
            IRuntimePlatform? platform = null,
            IPlatformRenderInterface? renderInterface = null,
            ICursorFactory? standardCursorFactory = null,
            Func<IStyle>? theme = null,
            IDispatcherImpl? dispatcherImpl = null,
            IFontManagerImpl? fontManagerImpl = null,
            ITextShaperImpl? textShaperImpl = null,
            IWindowImpl? windowImpl = null,
            IWindowingPlatform? windowingPlatform = null,
            IAccessKeyHandler? accessKeyHandler = null)
        {
            AssetLoader = assetLoader;
            InputManager = inputManager;
            GlobalClock = globalClock;
            AccessKeyHandler = accessKeyHandler;
            KeyboardDevice = keyboardDevice;
            KeyboardNavigation = keyboardNavigation;
            MouseDevice = mouseDevice;
            Platform = platform;
            RenderInterface = renderInterface;
            FontManagerImpl = fontManagerImpl;
            TextShaperImpl = textShaperImpl;
            StandardCursorFactory = standardCursorFactory;
            Theme = theme;
            DispatcherImpl = dispatcherImpl;
            WindowImpl = windowImpl;
            WindowingPlatform = windowingPlatform;
        }

        public IAssetLoader? AssetLoader { get; }
        public IInputManager? InputManager { get; }
        internal IGlobalClock? GlobalClock { get; set; }
        internal IAccessKeyHandler? AccessKeyHandler { get; }
        public Func<IKeyboardDevice?>? KeyboardDevice { get; }
        public Func<IKeyboardNavigationHandler?>? KeyboardNavigation { get; }
        public Func<IMouseDevice?>? MouseDevice { get; }
        public IRuntimePlatform? Platform { get; }
        public IPlatformRenderInterface? RenderInterface { get; }
        public IFontManagerImpl? FontManagerImpl { get; }
        public ITextShaperImpl? TextShaperImpl { get; }
        public ICursorFactory? StandardCursorFactory { get; }
        public Func<IStyle>? Theme { get; }
        public IDispatcherImpl? DispatcherImpl { get; }
        public IWindowImpl? WindowImpl { get; }
        public IWindowingPlatform? WindowingPlatform { get; }

        internal TestServices With(
            IAssetLoader? assetLoader = null,
            IInputManager? inputManager = null,
            IGlobalClock? globalClock = null,
            IAccessKeyHandler? accessKeyHandler = null,
            Func<IKeyboardDevice?>? keyboardDevice = null,
            Func<IKeyboardNavigationHandler?>? keyboardNavigation = null,
            Func<IMouseDevice?>? mouseDevice = null,
            IRuntimePlatform? platform = null,
            IPlatformRenderInterface? renderInterface = null,
            IRenderTimer? renderLoop = null,
            IScheduler? scheduler = null,
            ICursorFactory? standardCursorFactory = null,
            Func<IStyle>? theme = null,
            IDispatcherImpl? dispatcherImpl = null,
            IFontManagerImpl? fontManagerImpl = null,
            ITextShaperImpl? textShaperImpl = null,
            IWindowImpl? windowImpl = null,
            IWindowingPlatform? windowingPlatform = null)
        {
            return new TestServices(
                assetLoader: assetLoader ?? AssetLoader,
                inputManager: inputManager ?? InputManager,
                globalClock: globalClock ?? GlobalClock,
                accessKeyHandler: accessKeyHandler ?? AccessKeyHandler,
                keyboardDevice: keyboardDevice ?? KeyboardDevice,
                keyboardNavigation: keyboardNavigation ?? KeyboardNavigation,
                mouseDevice: mouseDevice ?? MouseDevice,
                platform: platform ?? Platform,
                renderInterface: renderInterface ?? RenderInterface,
                fontManagerImpl: fontManagerImpl ?? FontManagerImpl,
                textShaperImpl: textShaperImpl ?? TextShaperImpl,
                standardCursorFactory: standardCursorFactory ?? StandardCursorFactory,
                theme: theme ?? Theme,
                dispatcherImpl: dispatcherImpl ?? DispatcherImpl,
                windowingPlatform: windowingPlatform ?? WindowingPlatform,
                windowImpl: windowImpl ?? WindowImpl);
        }

        private static IStyle CreateSimpleTheme()
        {
            return new SimpleTheme();
        }
    }
}

using System;
using Moq;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.Rendering;
using System.Reactive.Concurrency;
using System.Collections.Generic;
using Avalonia.Controls;
using System.Reflection;
using Avalonia.Animation;
using Avalonia.Headless;
using Avalonia.Threading;

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
            fontManagerImpl: new HeadlessFontManagerStub(),
            textShaperImpl: new HeadlessTextShaperStub(),
            windowingPlatform: new MockWindowingPlatform());

        public static readonly TestServices MockPlatformRenderInterface = new TestServices(
            assetLoader: new StandardAssetLoader(),
            renderInterface: new HeadlessPlatformRenderInterface(),
            fontManagerImpl: new HeadlessFontManagerStub(),
            textShaperImpl: new HeadlessTextShaperStub());

        public static readonly TestServices MockPlatformWrapper = new TestServices(
            platform: Mock.Of<IRuntimePlatform>());

        public static readonly TestServices MockThreadingInterface = new TestServices(
            dispatcherImpl: new NullDispatcherImpl());

        public static readonly TestServices MockWindowingPlatform = new TestServices(
            windowingPlatform: new MockWindowingPlatform());

        public static readonly TestServices RealFocus = new TestServices(
            focusManager: new FocusManager(),
            keyboardDevice: () => new KeyboardDevice(),
            keyboardNavigation: () => new KeyboardNavigationHandler(),
            inputManager: new InputManager(),
            assetLoader: new StandardAssetLoader(),
            renderInterface: new HeadlessPlatformRenderInterface(),
            fontManagerImpl: new HeadlessFontManagerStub(),
            textShaperImpl: new HeadlessTextShaperStub());

        public static readonly TestServices FocusableWindow = new TestServices(
            focusManager: new FocusManager(),
            keyboardDevice: () => new KeyboardDevice(),
            keyboardNavigation: () => new KeyboardNavigationHandler(),
            inputManager: new InputManager(),
            assetLoader: new StandardAssetLoader(),
            platform: new StandardRuntimePlatform(),
            renderInterface: new HeadlessPlatformRenderInterface(),
            standardCursorFactory: new HeadlessCursorFactoryStub(),
            theme: () => CreateSimpleTheme(),
            dispatcherImpl: new NullDispatcherImpl(),
            fontManagerImpl: new HeadlessFontManagerStub(),
            textShaperImpl: new HeadlessTextShaperStub(),
            windowingPlatform: new MockWindowingPlatform());
        
        public static readonly TestServices TextServices = new TestServices(
            assetLoader: new StandardAssetLoader(),
            renderInterface: new HeadlessPlatformRenderInterface(),
            fontManagerImpl: new HarfBuzzFontManagerImpl(),
            textShaperImpl: new HarfBuzzTextShaperImpl());
        
        public TestServices(
            IAssetLoader assetLoader = null,
            IFocusManager focusManager = null,
            IInputManager inputManager = null,
            Func<IKeyboardDevice> keyboardDevice = null,
            Func<IKeyboardNavigationHandler> keyboardNavigation = null,
            Func<IMouseDevice> mouseDevice = null,
            IRuntimePlatform platform = null,
            IPlatformRenderInterface renderInterface = null,
            IRenderTimer renderLoop = null,
            ICursorFactory standardCursorFactory = null,
            Func<IStyle> theme = null,
            IDispatcherImpl dispatcherImpl = null,
            IFontManagerImpl fontManagerImpl = null,
            ITextShaperImpl textShaperImpl = null,
            IWindowImpl windowImpl = null,
            IWindowingPlatform windowingPlatform = null)
        {
            AssetLoader = assetLoader;
            FocusManager = focusManager;
            InputManager = inputManager;
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

        internal TestServices(
            IGlobalClock globalClock,
            IAssetLoader assetLoader = null,
            IFocusManager focusManager = null,
            IInputManager inputManager = null,
            Func<IKeyboardDevice> keyboardDevice = null,
            Func<IKeyboardNavigationHandler> keyboardNavigation = null,
            Func<IMouseDevice> mouseDevice = null,
            IRuntimePlatform platform = null,
            IPlatformRenderInterface renderInterface = null,
            IRenderTimer renderLoop = null,
            ICursorFactory standardCursorFactory = null,
            Func<IStyle> theme = null,
            IDispatcherImpl dispatcherImpl = null,
            IFontManagerImpl fontManagerImpl = null,
            ITextShaperImpl textShaperImpl = null,
            IWindowImpl windowImpl = null,
            IWindowingPlatform windowingPlatform = null) : this(assetLoader, focusManager, inputManager, keyboardDevice,
            keyboardNavigation,
            mouseDevice, platform, renderInterface, renderLoop, standardCursorFactory, theme,
            dispatcherImpl, fontManagerImpl, textShaperImpl, windowImpl, windowingPlatform)
        {
            GlobalClock = globalClock;
        }

        public IAssetLoader AssetLoader { get; }
        public IInputManager InputManager { get; }
        public IFocusManager FocusManager { get; }
        internal IGlobalClock GlobalClock { get; set; }
        public Func<IKeyboardDevice> KeyboardDevice { get; }
        public Func<IKeyboardNavigationHandler> KeyboardNavigation { get; }
        public Func<IMouseDevice> MouseDevice { get; }
        public IRuntimePlatform Platform { get; }
        public IPlatformRenderInterface RenderInterface { get; }
        public IFontManagerImpl FontManagerImpl { get; }
        public ITextShaperImpl TextShaperImpl { get; }
        public ICursorFactory StandardCursorFactory { get; }
        public Func<IStyle> Theme { get; }
        public IDispatcherImpl DispatcherImpl { get; }
        public IWindowImpl WindowImpl { get; }
        public IWindowingPlatform WindowingPlatform { get; }

        internal TestServices With(
            IAssetLoader assetLoader = null,
            IFocusManager focusManager = null,
            IInputManager inputManager = null,
            Func<IKeyboardDevice> keyboardDevice = null,
            Func<IKeyboardNavigationHandler> keyboardNavigation = null,
            Func<IMouseDevice> mouseDevice = null,
            IRuntimePlatform platform = null,
            IPlatformRenderInterface renderInterface = null,
            IRenderTimer renderLoop = null,
            IScheduler scheduler = null,
            ICursorFactory standardCursorFactory = null,
            Func<IStyle> theme = null,
            IDispatcherImpl dispatcherImpl = null,
            IFontManagerImpl fontManagerImpl = null,
            ITextShaperImpl textShaperImpl = null,
            IWindowImpl windowImpl = null,
            IWindowingPlatform windowingPlatform = null,
            IGlobalClock globalClock = null)
        {
            return new TestServices(
                globalClock ?? GlobalClock,
                assetLoader: assetLoader ?? AssetLoader,
                focusManager: focusManager ?? FocusManager,
                inputManager: inputManager ?? InputManager,
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

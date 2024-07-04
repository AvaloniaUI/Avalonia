using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.Shapes;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Avalonia.Utilities;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests;

public class WindowTestsSizeAndPosition
{
    [Flags]
    public enum PlatformImplBehavior
    {
        ResizeTriggersResized = 1,
        MoveTriggersPositionChanged = 2,
        WrongScalingBeforeSettingPosition = 4
    }

    public enum ShowMode
    {
        Modal,
        WithOwner,
        NoOwner
    }

    public enum ScreenLayout
    {
        UnixLike,
        WindowsLike
    }

    public enum ScreenId
    {
        Main = Screen21,
        Screen00 = 0,
        Screen01 = 1,
        Screen02 = 2,
        Screen10 = 3,
        Screen11 = 4,
        Screen12 = 5,
        Screen20 = 6,
        Screen21 = 7,
        Screen22 = 8
    }

    public class SizeAndPositionTestData
    {
        public PixelPoint ExpectedPosition;
        public Size ExpectedWidthAndHeight;
        public Size ExpectedClientSize;

        public PixelPoint OwnerPosition;
        public Size OwnerSize;

        public ScreenLayout ScreenLayout;
        public bool PlatformHasFrameSize;

        public ShowMode Mode;

        public WindowStartupLocation StartupLocation;
        public SizeToContent SizeToContent;
        public Size? ContentSize;
        public double? Width;
        public double? Height;
        public PixelPoint? Position;
        public double? MinWidth;
        public double? MinHeight;
        public double? MaxWidth;
        public double? MaxHeight;

        public ScreenId TargetScreen;
        public ScreenId OwnerTargetScreen;
        public PlatformImplBehavior PlatformImplBehavior;

        private Func<Dictionary<ScreenId, Screen>, PixelPoint> _positionFactory;
        private Func<Dictionary<ScreenId, Screen>, PixelPoint> _expectedPositionFactory;
        private Func<Dictionary<ScreenId, Screen>, PixelPoint> _ownerPositionFactory;

        public string Config = $"{nameof(SizeAndPositionTestData)}.{nameof(Define)}()";

        public void Calculate(Dictionary<ScreenId, Screen> screenLayout)
        {
            if (_positionFactory != null)
                Position = _positionFactory(screenLayout);
            if (_expectedPositionFactory != null)
                ExpectedPosition = _expectedPositionFactory(screenLayout);
            if (_ownerPositionFactory != null)
                OwnerPosition = _ownerPositionFactory(screenLayout);
        }

        public static SizeAndPositionTestData Define() => new SizeAndPositionTestData().WithOwnerOnScreen(ScreenId.Main);

        public SizeAndPositionTestData WithScreenLayout(ScreenLayout screenLayout)
        {
            ScreenLayout = screenLayout;
            Config += $"{Environment.NewLine}.{nameof(WithScreenLayout)}(ScreenLayout.{screenLayout})";
            return this;
        }

        public SizeAndPositionTestData WithPlatformBehavior(PlatformImplBehavior platformBehavior)
        {
            PlatformImplBehavior = platformBehavior;
            Config += $"{Environment.NewLine}.{nameof(WithPlatformBehavior)}(PlatformImplBehavior.{platformBehavior})";
            return this;
        }

        public SizeAndPositionTestData WithDialogMode(ShowMode mode)
        {
            Mode = mode;
            Config += $"{Environment.NewLine}.{nameof(WithDialogMode)}(ShowMode.{mode})";
            return this;
        }

        public SizeAndPositionTestData WithOwnerOnScreen(ScreenId targetScreen)
        {
            _ownerPositionFactory = x => x[targetScreen].Bounds.Position + new PixelPoint(350, 100);
            OwnerSize = new Size(1200, 700);
            OwnerTargetScreen = targetScreen;
            Config += $"{Environment.NewLine}.{nameof(WithOwnerOnScreen)}(ScreenId.{targetScreen})";
            return this;
        }

        public SizeAndPositionTestData WithPredefinedSize_400_300()
        {
            Width = 400;
            Height = 300;
            ExpectedWidthAndHeight = new Size(400, 300);
            ExpectedClientSize = new Size(400, 300);
            Config += $"{Environment.NewLine}.{nameof(WithPredefinedSize_400_300)}()";
            return this;
        }

        public SizeAndPositionTestData WithPredefinedSizeLargerThanScreen_10k_10k()
        {
            Width = 10000;
            Height = 10000;
            ExpectedWidthAndHeight = new Size(10000, 10000);
            ExpectedClientSize = new Size(10000, 10000);
            Config += $"{Environment.NewLine}.{nameof(WithPredefinedSizeLargerThanScreen_10k_10k)}()";
            return this;
        }

        public SizeAndPositionTestData WithAutoSizeAndPredefinedSize_800_600()
        {
            Width = 1200;
            Height = 300;
            ContentSize = new Size(800, 600);
            ExpectedWidthAndHeight = new Size(800, 600);
            ExpectedClientSize = new Size(800, 600);
            SizeToContent = SizeToContent.WidthAndHeight;
            Config += $"{Environment.NewLine}.{nameof(WithAutoSizeAndPredefinedSize_800_600)}()";
            return this;
        }

        public SizeAndPositionTestData WithAutoSizeAndWithoutPredefinedSize_800_600()
        {
            ContentSize = new Size(800, 600);
            ExpectedWidthAndHeight = new Size(double.NaN, double.NaN);
            ExpectedClientSize = new Size(800, 600);
            SizeToContent = SizeToContent.WidthAndHeight;
            Config += $"{Environment.NewLine}.{nameof(WithAutoSizeAndWithoutPredefinedSize_800_600)}()";
            return this;
        }

        public SizeAndPositionTestData WithAutoSizeWidthAndPredefinedHeight_800_300()
        {
            Height = 300;
            ContentSize = new Size(800, 600);
            ExpectedWidthAndHeight = new Size(double.NaN, 300);
            ExpectedClientSize = new Size(800, 300);
            SizeToContent = SizeToContent.Width;
            Config += $"{Environment.NewLine}.{nameof(WithAutoSizeWidthAndPredefinedHeight_800_300)}()";
            return this;
        }

        public SizeAndPositionTestData WithAutoSizeHeightAndPredefinedWidth_1200_600()
        {
            Width = 1200;
            ContentSize = new Size(800, 600);
            ExpectedWidthAndHeight = new Size(1200, double.NaN);
            ExpectedClientSize = new Size(1200, 600);
            SizeToContent = SizeToContent.Height;
            Config += $"{Environment.NewLine}.{nameof(WithAutoSizeHeightAndPredefinedWidth_1200_600)}()";
            return this;
        }

        public SizeAndPositionTestData WithLocationWithoutPosition(WindowStartupLocation startupLocation, ScreenId targetScreen)
        {
            StartupLocation = startupLocation;
            TargetScreen = targetScreen;

            if (startupLocation == WindowStartupLocation.Manual)
            {
                _expectedPositionFactory = _positionFactory ?? (_ => new PixelPoint(0, 0));
            }

            if (startupLocation == WindowStartupLocation.CenterScreen)
            {
                _expectedPositionFactory = x =>
                {
                    // _positionFactory

                    var screen = targetScreen;
                    if (Mode != ShowMode.NoOwner)
                        screen = OwnerTargetScreen;
                    else if (_positionFactory == null) // Either screen at position 0:0 or primary screen if 0:0 is outside any screens
                    {
                        var scr = ScreenHelper.ScreenFromPoint(default, x.Values.ToArray()) ?? x.Values.Single(y => y.IsPrimary);
                        screen = x.Single(y => y.Value == scr).Key;
                    }

                    var windowSize = ExpectedClientSize + (PlatformHasFrameSize ? SizeAndPositionTestDataProvider.FrameTotalSize : default);
                    var windowPixelSize = PixelSize.FromSize(windowSize, x[screen].Scaling);
                    return x[screen].WorkingArea.CenterRect(new PixelRect(windowPixelSize)).Position;
                };
            }

            if (startupLocation == WindowStartupLocation.CenterOwner)
            {
                _expectedPositionFactory = x =>
                {
                    var screen = OwnerTargetScreen;
                    if (Mode == ShowMode.NoOwner) // CenterScreen
                    {
                        screen = targetScreen;
                        if (_positionFactory == null) // Either screen at position 0:0 or primary screen if 0:0 is outside any screens
                        {
                            var scr = ScreenHelper.ScreenFromPoint(default, x.Values.ToArray()) ?? x.Values.Single(y => y.IsPrimary);
                            screen = x.Single(y => y.Value == scr).Key;
                        }
                    }

                    var windowSize = ExpectedClientSize + (PlatformHasFrameSize ? SizeAndPositionTestDataProvider.FrameTotalSize : default);
                    var windowPixelSize = PixelSize.FromSize(windowSize, x[screen].Scaling);

                    if (Mode == ShowMode.NoOwner) // CenterScreen
                        return x[screen].WorkingArea.CenterRect(new PixelRect(windowPixelSize)).Position;

                    var ownerPixelSize = PixelSize.FromSize(OwnerSize + (PlatformHasFrameSize ? SizeAndPositionTestDataProvider.FrameTotalSize : default), x[OwnerTargetScreen].Scaling);
                    return new PixelRect(_ownerPositionFactory(x), ownerPixelSize).CenterRect(new PixelRect(windowPixelSize)).Position;
                };
            }

            Config += $"{Environment.NewLine}.{nameof(WithLocationWithoutPosition)}(WindowStartupLocation.{startupLocation}, ScreenId.{targetScreen})";
            return this;
        }

        public SizeAndPositionTestData WithLocationWithPosition_250_450(WindowStartupLocation startupLocation, ScreenId targetScreen)
        {
            _positionFactory = x => x[targetScreen].WorkingArea.Position + new PixelPoint(250, 450);
            WithLocationWithoutPosition(startupLocation, targetScreen);
            Config += $"{Environment.NewLine}.{nameof(WithLocationWithPosition_250_450)}()";
            return this;
        }
    }

    public class SizeAndPositionTestDataProvider : IEnumerable<object[]>
    {
        public Dictionary<ScreenLayout, Screen[]> ScreenLayouts { get; }

        public SizeAndPositionTestDataProvider()
        {
            var screen00_1 = new Mock<Screen>(1.25, new PixelRect(-4000, -9000, 4000, 3000), new PixelRect(-4000, -9000, 4000, 3000), false);
            var screen01_1 = new Mock<Screen>(2.5, new PixelRect(0, -12000, 8000, 6000), new PixelRect(0, -12000, 8000, 6000), false);
            var screen02_1 = new Mock<Screen>(1.25, new PixelRect(8000, -9000, 4000, 3000), new PixelRect(8000, -9000, 4000, 3000), false);
            var screen10_1 = new Mock<Screen>(2.5, new PixelRect(-8000, -6000, 8000, 6000), new PixelRect(-8000, -6000, 8000, 6000), false);
            var screen11_1 = new Mock<Screen>(2.5, new PixelRect(0, -6000, 8000, 6000), new PixelRect(0, -6000, 8000, 6000), false);
            var screen12_1 = new Mock<Screen>(2.5, new PixelRect(8000, -6000, 8000, 6000), new PixelRect(8000, -6000, 8000, 6000), false);
            var screen20_1 = new Mock<Screen>(1.25, new PixelRect(-4000, 0, 4000, 3000), new PixelRect(-4000, 0, 4000, 3000), false);
            var screen21_1 = new Mock<Screen>(2.5, new PixelRect(0, 0, 8000, 6000), new PixelRect(0, 0, 8000, 5900), true);
            var screen22_1 = new Mock<Screen>(1.25, new PixelRect(8000, 0, 4000, 3000), new PixelRect(8000, 0, 4000, 3000), false);

            var screen00_2 = new Mock<Screen>(2, new PixelRect(4000, 3000, 4000, 3000), new PixelRect(4000, 3000, 4000, 3000), false);
            var screen01_2 = new Mock<Screen>(2, new PixelRect(4000, 0, 8000, 6000), new PixelRect(4000, 0, 8000, 6000), false);
            var screen02_2 = new Mock<Screen>(2, new PixelRect(16000, 3000, 4000, 3000), new PixelRect(16000, 3000, 4000, 3000), false);
            var screen10_2 = new Mock<Screen>(2, new PixelRect(0, 6000, 8000, 6000), new PixelRect(0, 6000, 8000, 6000), false);
            var screen11_2 = new Mock<Screen>(2, new PixelRect(8000, 6000, 8000, 6000), new PixelRect(8000, 6000, 8000, 6000), false);
            var screen12_2 = new Mock<Screen>(2, new PixelRect(16000, 6000, 8000, 6000), new PixelRect(16000, 6000, 8000, 6000), false);
            var screen20_2 = new Mock<Screen>(2, new PixelRect(4000, 12000, 4000, 3000), new PixelRect(4000, 12000, 4000, 3000), false);
            var screen21_2 = new Mock<Screen>(2, new PixelRect(8000, 12000, 8000, 6000), new PixelRect(8000, 12000, 8000, 5900), true);
            var screen22_2 = new Mock<Screen>(2, new PixelRect(16000, 12000, 4000, 3000), new PixelRect(16000, 12000, 4000, 3000), false);

            ScreenLayouts = new Dictionary<ScreenLayout, Screen[]>
            {
                {
                    ScreenLayout.WindowsLike, [
                        screen00_1.Object, screen01_1.Object, screen02_1.Object,
                        screen10_1.Object, screen11_1.Object, screen12_1.Object,
                        screen20_1.Object, screen21_1.Object, screen22_1.Object
                    ]
                },
                {
                    ScreenLayout.UnixLike, [
                        screen00_2.Object, screen01_2.Object, screen02_2.Object,
                        screen10_2.Object, screen11_2.Object, screen12_2.Object,
                        screen20_2.Object, screen21_2.Object, screen22_2.Object
                    ]
                }
            };
        }

        public static readonly Size FrameTotalSize = new(10, 50);

        public IEnumerable<SizeAndPositionTestData> Parameters()
        {
            var platformBehaviors = Enumerable
                .Range(0, (int)Enum.GetValues<PlatformImplBehavior>().Aggregate((x, y) => x | y) + 1)
                .Select(x => (PlatformImplBehavior)x).ToArray();

            var sizeCases = new Func<SizeAndPositionTestData, SizeAndPositionTestData>[] 
            { 
                x => x.WithPredefinedSize_400_300(), 
                x => x.WithAutoSizeAndPredefinedSize_800_600(), 
                x => x.WithAutoSizeAndWithoutPredefinedSize_800_600(), 
                x => x.WithAutoSizeWidthAndPredefinedHeight_800_300(), 
                x => x.WithAutoSizeHeightAndPredefinedWidth_1200_600() 
            };

            var locationCases = new Func<SizeAndPositionTestData, WindowStartupLocation, ScreenId, SizeAndPositionTestData>[]
            {
                (x, l, s) => x.WithLocationWithoutPosition(l, s), 
                (x, l, s) => x.WithLocationWithPosition_250_450(l, s)
            };

            var ownerScreens = new[] { ScreenId.Main, ScreenId.Screen00, ScreenId.Screen11 };
            var windowScreens = new[] { ScreenId.Main, ScreenId.Screen00, ScreenId.Screen11 };

            var startupLocations = Enum.GetValues<WindowStartupLocation>();


            foreach (var platformBehavior in platformBehaviors)
            {
                foreach (var screenLayout in Enum.GetValues<ScreenLayout>())
                {
                    foreach (var mode in Enum.GetValues<ShowMode>())
                    {
                        foreach (var startupLocation in Enum.GetValues<WindowStartupLocation>())
                        {
                            // with position
                            yield return SizeAndPositionTestData.Define().WithPlatformBehavior(platformBehavior).WithScreenLayout(screenLayout).WithDialogMode(mode)
                                .WithOwnerOnScreen(ScreenId.Main)
                                .WithLocationWithPosition_250_450(startupLocation, ScreenId.Main)
                                .WithPredefinedSize_400_300();

                            // with fixed size
                            yield return SizeAndPositionTestData.Define().WithPlatformBehavior(platformBehavior).WithScreenLayout(screenLayout).WithDialogMode(mode)
                                .WithOwnerOnScreen(ScreenId.Main)
                                .WithLocationWithoutPosition(startupLocation, ScreenId.Main)
                                .WithPredefinedSize_400_300();

                            // another screen
                            yield return SizeAndPositionTestData.Define().WithPlatformBehavior(platformBehavior).WithScreenLayout(screenLayout).WithDialogMode(mode)
                                .WithOwnerOnScreen(ScreenId.Screen22)
                                .WithLocationWithoutPosition(startupLocation, ScreenId.Main)
                                .WithPredefinedSize_400_300();

                            // with autosize
                            yield return SizeAndPositionTestData.Define().WithPlatformBehavior(platformBehavior).WithScreenLayout(screenLayout).WithDialogMode(mode)
                                .WithOwnerOnScreen(ScreenId.Main)
                                .WithLocationWithoutPosition(startupLocation, ScreenId.Main)
                                .WithAutoSizeAndWithoutPredefinedSize_800_600();

                            // autosize should override predefined size
                            yield return SizeAndPositionTestData.Define().WithPlatformBehavior(platformBehavior).WithScreenLayout(screenLayout).WithDialogMode(mode)
                                .WithOwnerOnScreen(ScreenId.Main)
                                .WithLocationWithoutPosition(startupLocation, ScreenId.Main)
                                .WithAutoSizeAndPredefinedSize_800_600();
                        }
                    }
                }
            }

            /* all cases, may take a while to execute
           
            foreach (var platformBehavior in platformBehaviors)
            {
                foreach (var screenLayout in Enum.GetValues<ScreenLayout>())
                {
                    foreach (var mode in Enum.GetValues<ShowMode>())
                    {
                        foreach (var sizeCase in sizeCases)
                        {
                            foreach (var locationCase in locationCases)
                            {
                                foreach (var ownerScreen in ownerScreens)
                                {
                                    foreach (var windowScreen in windowScreens)
                                    {
                                        foreach (var startupLocation in startupLocations)
                                        {
                                            var data = SizeAndPositionTestData.Define()
                                                .WithPlatformBehavior(platformBehavior)
                                                .WithScreenLayout(screenLayout)
                                                .WithOwnerOnScreen(ownerScreen)
                                                .WithDialogMode(mode);
                                            data = sizeCase(data);
                                            data = locationCase(data, startupLocation, windowScreen);

                                            yield return data;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }  
            */
        }

        public IEnumerator<object[]> GetEnumerator()
        {
            foreach (var parameter in Parameters())
                yield return [parameter];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

// Screen scheme 1 (windows-like):     ┌─────────────────────────────────────┐                                     
//                                     │ screen01 x2.5                       │                                      
//                                     │ pos: 0:-12000                       │ 
//                                     │ phys: 8000x6000                     │ 
//                                     │ log: 3200x2400                      │ 
//                   ┌─────────────────┤                                     ├─────────────────┐ 
//                   │ screen00 x1.25  │                                     │ screen02 x1.25  │
//                   │ pos:-4000:-9000 │                                     │ pos: 8000:-9000 │                                    
//                   │ phys: 4000x3000 │                                     │ phys: 4000x3000 │                                     
//                   │ log: 3200x2400  │                                     │ log: 3200x2400  │                                     
// ┌─────────────────┴─────────────────┼─────────────────────────────────────┼─────────────────┴─────────────────┐
// │ screen10 x2.5                     │ screen11 x2.5                       │ screen12 x2.5                     │
// │ pos: -8000:-6000                  │ pos: 0:-6000                        │ pos: 8000:-6000                   │
// │ phys: 8000x6000                   │ phys: 8000x6000                     │ phys: 8000x6000                   │
// │ log: 3200x2400                    │ log: 3200x2400                      │ log: 3200x2400                    │
// │                                   │                                     │                                   │
// │                                   │                                     │                                   │
// │                                   │                                     │                                   │
// │                                   │                                     │                                   │
// │                                   │                                     │                                   │
// └─────────────────┬─────────────────┼─────────────────────────────────────┼─────────────────┬─────────────────┘
//                   │ screen20 x1.25  │ screen21 x2.5                       │ screen22 x1.25  │
//                   │ pos: -4000:0    │ pos: 0:0                            │ pos: 8000:0     │
//                   │ phys: 4000x3000 │ phys: 8000x6000                     │ phys: 4000x3000 │
//                   │ log: 3200x2400  │ log: 3200x2400                      │ log: 3200x2400  │ 
//                   └─────────────────┤                                     ├─────────────────┘                
//                                     │ wrk: 8000x5900                      │    
//                                     │ main                                │    
//                                     │                                     │    
//                                     ├─────────────────────────────────────┤    
//                                     └─────────────────────────────────────┘
//
// Screen scheme 2 (unix-like):        ┌─────────────────────────────────────┐                                     
//                                     │ screen01 x2                         │                                      
//                                     │ pos: 4000:0                         │ 
//                                     │ phys: 8000x6000                     │ 
//                                     │ log: 4000x3000                      │ 
//                   ┌─────────────────┤                                     ├─────────────────┐ 
//                   │ screen00 x2     │                                     │ screen02 x2     │
//                   │ pos: 4000:3000  │                                     │ pos: 16000:3000 │                                    
//                   │ phys: 4000x3000 │                                     │ phys: 4000x3000 │                                     
//                   │ log: 2000x1500  │                                     │ log: 2000x1500  │                                     
// ┌─────────────────┴─────────────────┼─────────────────────────────────────┼─────────────────┴─────────────────┐
// │ screen10 x2                       │ screen11 x2                         │ screen12 x2                       │
// │ pos: 0:6000                       │ pos: 8000:6000                      │ pos: 16000:8000                   │
// │ phys: 8000x6000                   │ phys: 8000x6000                     │ phys: 8000x6000                   │
// │ log: 4000x3000                    │ log: 4000x3000                      │ log: 4000x3000                    │
// │                                   │                                     │                                   │
// │                                   │                                     │                                   │
// │                                   │                                     │                                   │
// │                                   │                                     │                                   │
// │                                   │                                     │                                   │
// └─────────────────┬─────────────────┼─────────────────────────────────────┼─────────────────┬─────────────────┘
//                   │ screen20 x2     │ screen21 x2                         │ screen22 x2     │
//                   │ pos: 4000:12000 │ pos: 8000:12000                     │ pos:16000:12000 │
//                   │ phys: 4000x3000 │ phys: 8000x6000                     │ phys: 4000x3000 │
//                   │ log: 2000x1500  │ log: 4000x3000                      │ log: 2000x1500  │ 
//                   └─────────────────┤                                     ├─────────────────┘                
//                                     │ wrk: 8000x5900                      │    
//                                     │ main                                │    
//                                     │                                     │    
//                                     ├─────────────────────────────────────┤    
//                                     └─────────────────────────────────────┘


    [Theory]
    [ClassData(typeof(SizeAndPositionTestDataProvider))]
    public void Window_InitialSizeAndPositionMultiTest(SizeAndPositionTestData parameters)
    {
        var screenSetup = new SizeAndPositionTestDataProvider().ScreenLayouts[parameters.ScreenLayout];
        var screenMap = screenSetup.Select((x, i) => new { x, i }).ToDictionary(x => (ScreenId)x.i, x => x.x);

        parameters.Calculate(screenMap);

        var screens = new Mock<IScreenImpl>();
        screens.Setup(x => x.AllScreens).Returns(screenSetup);
        screens.Setup(x => x.ScreenCount).Returns(screenSetup.Length);
        screens.Setup(x => x.ScreenFromPoint(It.IsAny<PixelPoint>())).Returns<PixelPoint>(p =>
            ScreenHelper.ScreenFromPoint(p, screenSetup));
        screens.Setup(x => x.ScreenFromRect(It.IsAny<PixelRect>())).Returns<PixelRect>(p =>
            ScreenHelper.ScreenFromRect(p, screenSetup));
        screens.Setup(x => x.ScreenFromWindow(It.IsAny<IWindowBaseImpl>())).Returns<IWindowBaseImpl>(p =>
            ScreenHelper.ScreenFromWindow(p, screenSetup));

        var parentWindowImpl = new Mock<IWindowImpl>();
        parentWindowImpl.Setup(r => r.Compositor).Returns(RendererMocks.CreateDummyCompositor());
        parentWindowImpl.SetupGet(x => x.Screen).Returns(screens.Object);
        parentWindowImpl.SetupGet(x => x.RenderScaling).Returns(ScreenHelper.ScreenFromPoint(parameters.OwnerPosition, screenSetup)!.Scaling);
        parentWindowImpl.SetupGet(x => x.DesktopScaling).Returns(ScreenHelper.ScreenFromPoint(parameters.OwnerPosition, screenSetup)!.Scaling);
        PixelPoint parentPosition = default;
        parentWindowImpl.SetupGet(x => x.Position).Returns(() => parentPosition);
        parentWindowImpl.Setup(x => x.Move(It.IsAny<PixelPoint>())).Callback<PixelPoint>(p => parentPosition = p);
        parentWindowImpl.SetupGet(x => x.FrameSize).Returns(() =>
            parameters.PlatformHasFrameSize ? parameters.OwnerSize + SizeAndPositionTestDataProvider.FrameTotalSize : null);

        var impl = new Mock<IWindowImpl>();

        impl.Setup(r => r.Compositor).Returns(RendererMocks.CreateDummyCompositor());
        impl.SetupGet(x => x.Screen).Returns(screens.Object);

        impl.SetupProperty(x => x.Resized);
        impl.SetupProperty(x => x.ScalingChanged);
        impl.SetupProperty(x => x.PositionChanged);

        bool returnCorrectScaling = false;
        PixelPoint position = default;
        impl.SetupGet(x => x.Position).Returns(() => position);
        impl.Setup(x => x.Move(It.IsAny<PixelPoint>())).Callback<PixelPoint>(p =>
        {
            returnCorrectScaling = true;
            position = p;
            if (parameters.PlatformImplBehavior.HasFlag(PlatformImplBehavior.MoveTriggersPositionChanged))
                impl.Object.PositionChanged?.Invoke(p);
        });
 
        PixelSize size = default;
        PixelSize minSize = new PixelSize(0, 0);
        PixelSize maxSize = new PixelSize(int.MaxValue, int.MaxValue);
        impl.Setup(x => x.Resize(It.IsAny<Size>(), It.IsAny<WindowResizeReason>())).Callback<Size, WindowResizeReason>((s, r) =>
        {
            size = new PixelSize((int)(MathUtilities.Clamp(s.Width, minSize.Width, maxSize.Width) * impl.Object.DesktopScaling),
                (int)(MathUtilities.Clamp(s.Height, minSize.Height, maxSize.Height) * impl.Object.DesktopScaling));
            if (parameters.PlatformImplBehavior.HasFlag(PlatformImplBehavior.ResizeTriggersResized))
                impl.Object.Resized?.Invoke(s, r);
        });
        impl.SetupGet(x => x.ClientSize).Returns(() => size.ToSize(impl.Object.DesktopScaling));
        impl.SetupGet(x => x.FrameSize).Returns(() =>
        {
            if (!parameters.PlatformHasFrameSize)
                return default;
            return new PixelSize(size.Width, size.Height)
                .ToSize(impl.Object.DesktopScaling) + SizeAndPositionTestDataProvider.FrameTotalSize;
        });
        impl.Setup(x => x.SetMinMaxSize(It.IsAny<Size>(), It.IsAny<Size>())).Callback<Size, Size>((min, max) =>
        {
            minSize = PixelSize.FromSize(min, impl.Object.DesktopScaling);
            maxSize = PixelSize.FromSize(max, impl.Object.DesktopScaling);
            if (max.Width == Double.PositiveInfinity)
                maxSize = maxSize.WithWidth(int.MaxValue);
            if (max.Height == Double.PositiveInfinity)
                maxSize = maxSize.WithHeight(int.MaxValue);
            size = new PixelSize(MathUtilities.Clamp(size.Width, minSize.Width, maxSize.Width),
                MathUtilities.Clamp(size.Height, minSize.Height, maxSize.Height));
            if (parameters.PlatformImplBehavior.HasFlag(PlatformImplBehavior.ResizeTriggersResized))
                impl.Object.Resized?.Invoke(size.ToSize(impl.Object.DesktopScaling), WindowResizeReason.Unspecified);
        });

        impl.SetupGet(x => x.DesktopScaling).Returns(() =>
        {
            if (parameters.PlatformImplBehavior.HasFlag(PlatformImplBehavior.WrongScalingBeforeSettingPosition) && !returnCorrectScaling)
                return 1.0;
            return screens.Object.ScreenFromPoint(position)?.Scaling ?? 1.0;
        });
        impl.SetupGet(x => x.RenderScaling).Returns(() => impl.Object.DesktopScaling);

        // add some cases
        impl.SetupGet(x => x.MaxAutoSizeHint).Returns(new Size(double.MaxValue, double.MaxValue));


        var parentWindowServices = TestServices.StyledWindow.With(
            windowingPlatform: new MockWindowingPlatform(() => parentWindowImpl.Object));

        var windowServices = TestServices.StyledWindow.With(
            windowingPlatform: new MockWindowingPlatform(() => impl.Object));

        using (UnitTestApplication.Start(parentWindowServices))
        {
            var parentWindow = new Window { Position = parameters.OwnerPosition, Width = parameters.OwnerSize.Width, Height = parameters.OwnerSize.Height };
            parentWindow.Show();

            using (UnitTestApplication.Start(windowServices))
            {
                var target = new Window { WindowStartupLocation = parameters.StartupLocation, SizeToContent = parameters.SizeToContent };
                if (parameters.ContentSize != null)
                    target.Content = new Rectangle { Width = parameters.ContentSize.Value.Width, Height = parameters.ContentSize.Value.Height };
                if (parameters.Position != null)
                    target.Position = parameters.Position.Value;
                if (parameters.Width != null)
                    target.Width = parameters.Width.Value;
                if (parameters.Height != null)
                    target.Height = parameters.Height.Value;
                if (parameters.MaxWidth != null)
                    target.MaxWidth = parameters.MaxWidth.Value;
                if (parameters.MaxHeight != null)
                    target.MaxHeight = parameters.MaxHeight.Value;
                if (parameters.MaxWidth != null)
                    target.MaxWidth = parameters.MaxWidth.Value;
                if (parameters.MaxHeight != null)
                    target.MaxHeight = parameters.MaxHeight.Value;

                // Check first opening
                ShowAndCheck(true);

                if (parameters.Mode != ShowMode.Modal)
                {
                    // Check second opening
                    ShowAndCheck(false);
                }

                void ShowAndCheck(bool isFirst)
                {
                    if (parameters.Mode == ShowMode.Modal)
                        target.ShowDialog(parentWindow);
                    else if (parameters.Mode == ShowMode.WithOwner)
                        target.Show(parentWindow);
                    else
                        target.Show();

                    var expectedScaling = ScreenHelper.ScreenFromPoint(target.Position, screenSetup)?.Scaling ?? 1.0;

                    Assert.Equal(parameters.ExpectedPosition.X, target.Position.X);
                    Assert.Equal(parameters.ExpectedPosition.Y, target.Position.Y);
                    Assert.Equal(parameters.ExpectedClientSize.Width, target.ClientSize.Width);
                    Assert.Equal(parameters.ExpectedClientSize.Height, target.ClientSize.Height);
                    if (isFirst)
                    {
                        Assert.Equal(parameters.ExpectedWidthAndHeight.Width, target.Width);
                        Assert.Equal(parameters.ExpectedWidthAndHeight.Height, target.Height);
                    }
                    else
                    {
                        Assert.Equal(parameters.ExpectedClientSize.Width, target.Width);
                        Assert.Equal(parameters.ExpectedClientSize.Height, target.Height);
                    }

                    Assert.Equal(expectedScaling, target.DesktopScaling);
                    Assert.Equal(expectedScaling, target.RenderScaling);

                    target.PlatformImpl!.Resized!(target.PlatformImpl.ClientSize, WindowResizeReason.Unspecified);

                    Assert.Equal(parameters.ExpectedPosition.X, target.Position.X);
                    Assert.Equal(parameters.ExpectedPosition.Y, target.Position.Y);
                    Assert.Equal(parameters.ExpectedClientSize.Width, target.ClientSize.Width);
                    Assert.Equal(parameters.ExpectedClientSize.Height, target.ClientSize.Height);
                    Assert.Equal(parameters.ExpectedClientSize.Width, target.Width);
                    Assert.Equal(parameters.ExpectedClientSize.Height, target.Height);
                    Assert.Equal(expectedScaling, target.DesktopScaling);
                    Assert.Equal(expectedScaling, target.RenderScaling);

                    target.Close();
                }
            }
        }
    }
}

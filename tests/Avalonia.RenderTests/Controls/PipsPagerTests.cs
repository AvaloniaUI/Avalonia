using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Themes.Simple;
using Xunit;

namespace Avalonia.Skia.RenderTests
{
    public class PipsPagerTests : TestBase
    {
        public PipsPagerTests()
            : base(@"Controls/PipsPager")
        {
        }
        
        private static Border CreateTarget(int selectedPageIndex)
        {
            var pipsPager = new PipsPager
            {
                NumberOfPages = 5,
                SelectedPageIndex = selectedPageIndex,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var target = new Border
            {
                Padding = new Thickness(20),
                Background = Brushes.White,
                Child = pipsPager,
                Width = 400,
                Height = 150
            };

            AvaloniaLocator.CurrentMutable.Bind<ICursorFactory>().ToConstant(new CursorFactoryStub());
            target.Styles.Add(new SimpleTheme());

            target.Resources["ThemeForegroundBrush"] = Brushes.Black;
            target.Resources["ThemeControlLowBrush"] = Brushes.Gray;
            target.Resources["ThemeControlHighBrush"] = Brushes.Gray;
            target.Resources["ThemeControlMidBrush"] = Brushes.LightGray;
            target.Resources["ThemeBorderLowBrush"] = Brushes.Gray;
            target.Resources["ThemeBorderMidBrush"] = Brushes.Gray;
            target.Resources["ThemeAccentBrush"] = Brushes.Red;
            target.Resources["ThemeAccentBrush2"] = Brushes.Red;
            target.Resources["ThemeAccentBrush3"] = Brushes.Red;

            return target;
        }

        [Fact]
        public async Task PipsPager_Default()
        {
            var target = CreateTarget(1);

            target.Measure(new Size(400, 150));
            target.Arrange(new Rect(0, 0, 400, 150));

            await RenderToFile(target);
            CompareImages(skipImmediate: true);
        }

        [Fact]
        public async Task PipsPager_Preselected_Index()
        {
            var target = CreateTarget(3);

            target.Measure(new Size(400, 150));
            target.Arrange(new Rect(0, 0, 400, 150));

            await RenderToFile(target);
            CompareImages(skipImmediate: true);
        }

        private sealed class CursorFactoryStub : ICursorFactory
        {
            public ICursorImpl GetCursor(StandardCursorType cursorType) => new CursorStub();

            public ICursorImpl CreateCursor(Bitmap cursor, PixelPoint hotSpot) => new CursorStub();

            private sealed class CursorStub : ICursorImpl
            {
                public void Dispose()
                {
                }
            }
        }
    }
}

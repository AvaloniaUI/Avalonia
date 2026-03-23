using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Themes.Simple;
using Xunit;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else
namespace Avalonia.Direct2D1.RenderTests.Controls
#endif
{
    public class PipsPagerTests : TestBase
    {
        public PipsPagerTests()
            : base(@"Controls\PipsPager")
        {
        }

        private Decorator CreateTarget(int selectedPageIndex)
        {
            var pipsPager = new PipsPager
            {
                NumberOfPages = 5,
                SelectedPageIndex = selectedPageIndex,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var target = new Decorator
            {
                Width = 400,
                Height = 150,
                Child = new Border
                {
                    Background = Brushes.White,
                    Child = pipsPager
                }
            };

            AvaloniaLocator.CurrentMutable.Bind<ICursorFactory>().ToConstant(new CursorFactoryStub());
            target.Styles.Add(new SimpleTheme());

            return target;
        }

        [Fact]
        public async Task PipsPager_Default()
        {
            var target = CreateTarget(1);
            await RenderToFile(target);
            CompareImages(skipImmediate: true);
        }

        [Fact]
        public async Task PipsPager_Preselected_Index()
        {
            var target = CreateTarget(3);
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

using Perspex.Controls.Platform;
using Perspex.MobilePlatform.Fakes;
using Perspex.Platform;

namespace Perspex.MobilePlatform
{
    public static class Platform
    {
        internal static IWindowImpl NativeWindowImpl;
        private static FakeWindow FakeWindow;
        internal static IPlatformRenderInterface NativeRenderInterface;
        internal static SceneComposer Scene;
        public static void InitAndReplace()
        {
            NativeWindowImpl = PerspexLocator.Current.GetService<IWindowImpl>();
            NativeWindowImpl.Show();
            NativeWindowImpl.ClientSize = new Size(640, 480);
            FakeWindow = new FakeWindow(NativeWindowImpl);
            NativeRenderInterface =  PerspexLocator.Current.GetService<IPlatformRenderInterface>();
            Scene = new SceneComposer(NativeWindowImpl);
            PerspexLocator.CurrentMutable.Bind<ITopLevelRenderer>().ToConstant(new TopLevelRenderManager());
            PerspexLocator.CurrentMutable.Bind<IWindowImpl>().ToTransient<MobileWindow>();
            PerspexLocator.CurrentMutable.Bind<IPopupImpl>().ToTransient<MobilePopup>();
        }
    }
}

using Perspex.Controls.Platform;
using Perspex.TinyWM.Fakes;
using Perspex.Platform;

namespace Perspex.TinyWM
{
    public static class WindowManager
    {
        internal static IWindowImpl NativeWindowImpl;
        internal static IPlatformRenderInterface NativeRenderInterface;
        internal static SceneComposer Scene;
        public static void InitAndReplace()
        {
            NativeWindowImpl = PerspexLocator.Current.GetService<IWindowImpl>();
            NativeWindowImpl.Show();
            NativeWindowImpl.ClientSize = new Size(640, 480);
            NativeRenderInterface =  PerspexLocator.Current.GetService<IPlatformRenderInterface>();
            Scene = new SceneComposer(NativeWindowImpl);
            PerspexLocator.CurrentMutable.Bind<ITopLevelRenderer>().ToConstant(new TopLevelRenderManager());
            PerspexLocator.CurrentMutable.Bind<IWindowImpl>().ToTransient<WindowImpl>();
            PerspexLocator.CurrentMutable.Bind<IPopupImpl>().ToTransient<PopupImpl>();
        }
    }
}

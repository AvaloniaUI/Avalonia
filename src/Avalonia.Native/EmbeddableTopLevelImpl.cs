using Avalonia.Native.Interop;

namespace Avalonia.Native
{
    internal class EmbeddableTopLevelImpl : TopLevelImpl
    {
        public EmbeddableTopLevelImpl(IAvaloniaNativeFactory factory) : base(factory)
        {
            using (var e = new TopLevelEvents(this))
            {
                Init(new MacOSTopLevelHandle(factory.CreateTopLevel(e)), factory.CreateScreens());
            }
        }
    }
}

// -----------------------------------------------------------------------
// <copyright file="Direct2D1Initialize.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Direct2D1
{
    using Perspex.Direct2D1.Media;
    using Perspex.Media;
    using Perspex.Platform;
    using Splat;

    public static class Direct2D1Platform
    {
        public static void Initialize()
        {
            SharpDX.Direct2D1.Factory d2d1Factory = new SharpDX.Direct2D1.Factory();
            SharpDX.DirectWrite.Factory dwFactory = new SharpDX.DirectWrite.Factory();
            TextService textService = new TextService(dwFactory);

            var locator = Locator.CurrentMutable;
            locator.Register(() => d2d1Factory, typeof(SharpDX.Direct2D1.Factory));
            locator.Register(() => dwFactory, typeof(SharpDX.DirectWrite.Factory));
            locator.Register(() => textService, typeof(ITextService));

            locator.Register(() => new Renderer(), typeof(IRenderer));
            locator.Register(() => new StreamGeometryImpl(), typeof(IStreamGeometryImpl));
        }
    }
}

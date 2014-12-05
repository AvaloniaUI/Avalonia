// -----------------------------------------------------------------------
// <copyright file="LayoutableTests.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Layout.UnitTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Moq.AutoMock;
    using Perspex.Controls;
    using Perspex.Input;
    using Perspex.Platform;
    using Perspex.Rendering;
    using Perspex.Styling;
    using Splat;

    [TestClass]
    public class LayoutableTests
    {
        private Mock<ILayoutManager> layoutManager;

        [TestMethod]
        public void Calling_InvalidateMeasure_On_Window_Should_Call_LayoutManager_InvalidateMeasure()
        {
            using (var d = Locator.Current.WithResolver())
            {
                this.RegisterServices();

                Window target = new Window();
            }
        }

        private void RegisterServices()
        {
            var l = Locator.CurrentMutable;
            var m = new AutoMocker();

            var lm = m.CreateInstance<ILayoutManager>();
            //this.layoutManager = 

            l.RegisterConstant(new Mock<IInputManager>().Object, typeof(IInputManager));
            l.RegisterConstant(this.layoutManager.Object, typeof(ILayoutManager));
            l.RegisterConstant(new Mock<IPlatformRenderInterface>().Object, typeof(IPlatformRenderInterface));
            l.RegisterConstant(new Mock<IRenderManager>().Object, typeof(IRenderManager));
            l.RegisterConstant(new Mock<IStyler>().Object, typeof(IStyler));
            l.RegisterConstant(new Mock<IWindowImpl>().Object, typeof(IWindowImpl));
        }
    }
}

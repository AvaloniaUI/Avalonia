using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class NativeMenuTests : ScopedTestBase
    {
        [Fact]
        public void DockMenuProperty_Set_And_Get_On_Application()
        {
            using (UnitTestApplication.Start())
            {
                var app = Application.Current!;
                var menu = new NativeMenu();
                menu.Add(new NativeMenuItem("Test Item"));

                NativeMenu.SetDockMenu(app, menu);

                var result = NativeMenu.GetDockMenu(app);
                Assert.Same(menu, result);
            }
        }

        [Fact]
        public void DockMenuProperty_Returns_Null_When_Not_Set()
        {
            using (UnitTestApplication.Start())
            {
                var app = Application.Current!;

                var result = NativeMenu.GetDockMenu(app);
                Assert.Null(result);
            }
        }

        [Fact]
        public void DockMenuProperty_Can_Be_Set_On_Non_Application_Object()
        {
            var obj = new Border();
            var menu = new NativeMenu();

            NativeMenu.SetDockMenu(obj, menu);

            var result = NativeMenu.GetDockMenu(obj);
            Assert.Same(menu, result);
        }

        [Fact]
        public void MenuProperty_Still_Works_After_DockMenuProperty_Added()
        {
            using (UnitTestApplication.Start())
            {
                var app = Application.Current!;
                var appMenu = new NativeMenu();
                appMenu.Add(new NativeMenuItem("App Menu Item"));

                NativeMenu.SetMenu(app, appMenu);

                var result = NativeMenu.GetMenu(app);
                Assert.Same(appMenu, result);
            }
        }

        [Fact]
        public void DockMenuProperty_And_MenuProperty_Are_Independent()
        {
            using (UnitTestApplication.Start())
            {
                var app = Application.Current!;
                var appMenu = new NativeMenu();
                var dockMenu = new NativeMenu();

                NativeMenu.SetMenu(app, appMenu);
                NativeMenu.SetDockMenu(app, dockMenu);

                Assert.Same(appMenu, NativeMenu.GetMenu(app));
                Assert.Same(dockMenu, NativeMenu.GetDockMenu(app));
                Assert.NotSame(NativeMenu.GetMenu(app), NativeMenu.GetDockMenu(app));
            }
        }

        [Fact]
        public void DockMenu_Items_Can_Be_Added_And_Removed()
        {
            var menu = new NativeMenu();
            var item1 = new NativeMenuItem("Item 1");
            var item2 = new NativeMenuItem("Item 2");
            var separator = new NativeMenuItemSeparator();

            menu.Add(item1);
            menu.Add(separator);
            menu.Add(item2);

            Assert.Equal(3, menu.Items.Count);

            menu.Items.Remove(separator);

            Assert.Equal(2, menu.Items.Count);
            Assert.Same(item1, menu.Items[0]);
            Assert.Same(item2, menu.Items[1]);
        }
    }
}

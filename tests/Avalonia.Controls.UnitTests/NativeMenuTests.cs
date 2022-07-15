using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class NativeMenuTests
    {
        [Fact]
        public void NativeMenuItems_Should_Get_Collected_By_GC_When_Not_In_Menu()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = new Window();
                NativeMenu.SetMenu(window,new NativeMenu());
                var nativeMenu = NativeMenu.GetMenu(window);
                var topMenus = new string[] { "TopMenu1", "TopMenu2"};
                var weakReferences = new List<WeakReference<NativeMenuItem>>();
                foreach (var topMenu in topMenus)
                {
                    var menuItem= new NativeMenuItem(topMenu);
                    weakReferences.Add(new WeakReference<NativeMenuItem>(menuItem));
                    var menu = new NativeMenu();
                    menuItem.Menu = menu;
                    nativeMenu.Add(menuItem);
                }
                nativeMenu.Items.Clear();
                GC.Collect();
            }
        }
    }
}

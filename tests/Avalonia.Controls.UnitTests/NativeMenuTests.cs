using System;
using System.Collections.Generic;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class NativeMenuTests
    {
        // Code below is in the separate method because .NET will hold last enumerated NativeMenuItem in a local variable which will prevent it from being GC-ed.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static List<WeakReference<NativeMenuItem>> GetNativeMenuItemWeakReferences()
        {
            var weakReferences = new List<WeakReference<NativeMenuItem>>();
            var windowImpl = new Mock<IWindowImpl>();
            var windowingPlatform = new MockWindowingPlatform(() => windowImpl.Object);

            using (UnitTestApplication.Start(new TestServices(windowingPlatform: windowingPlatform)))
            {
                var window = new Window();
                NativeMenu.SetMenu(window, new NativeMenu());
                var nativeMenu = NativeMenu.GetMenu(window);
                var topMenus = new string[] { "TopMenu1", "TopMenu2" };

                foreach (var topMenu in topMenus)
                {
                    var menuItem = new NativeMenuItem(topMenu) { Menu = new NativeMenu() };
                    weakReferences.Add(new WeakReference<NativeMenuItem>(menuItem));
                    nativeMenu.Add(menuItem);
                }
                nativeMenu.Items.Clear();

            }
            return weakReferences;
        }

        [Fact]
        public void NativeMenuItems_Should_Get_Collected_By_GC_When_Not_In_Menu()
        {
            var weakReferences = GetNativeMenuItemWeakReferences();
            GC.Collect();

            Assert.False(weakReferences[0].TryGetTarget(out var _));
            Assert.False(weakReferences[1].TryGetTarget(out var _));

        }
    }
}

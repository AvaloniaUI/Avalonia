using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Xunit;

namespace Avalonia.Styling.UnitTests
{
    public class SelectorTests_Platform
    {
        [Fact]
        public void Platform_Selector_Should_Have_Correct_String_Representation()
        {
            var target = default(Selector).Platform(x => x.Class("foo"), "windows");

            Assert.Equal(":windows(.foo)", target.ToString());
        }

        [Fact]
        public void Platform_Match_Windows()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var control = new Control1();
                var target = default(Selector).Platform(x => x.OfType<Control1>(), "windows");

                Assert.Equal(SelectorMatchResult.AlwaysThisType, target.Match(control).Result);
            }
        }
        
        public class Control1 : Control
        {
        }
    }
}

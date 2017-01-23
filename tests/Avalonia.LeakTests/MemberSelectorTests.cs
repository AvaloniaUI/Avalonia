using Avalonia.Markup.Xaml.Templates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.dotMemoryUnit;
using Xunit;
using Xunit.Abstractions;

namespace Avalonia.LeakTests
{
    [DotMemoryUnit(FailIfRunWithoutSupport = false)]
    public class MemberSelectorTests
    {
        public MemberSelectorTests(ITestOutputHelper atr)
        {
            DotMemoryUnitTestOutput.SetOutputMethod(atr.WriteLine);
        }

        [Fact]
        public void Should_Not_Hold_Reference_To_Object()
        {
            WeakReference dataRef = null;

            var selector = new MemberSelector() { MemberName = "Child.StringValue" };

            Action run = () =>
            {
                var data = new Item()
                {
                    Child = new Item() { StringValue = "Value1" }
                };

                Assert.Same("Value1", selector.Select(data));

                dataRef = new WeakReference(data);
            };

            run();

            GC.Collect();

            Assert.False(dataRef.IsAlive);
        }

        private class Item
        {
            public Item Child { get; set; }
            public int IntValue { get; set; }

            public string StringValue { get; set; }
        }
    }
}

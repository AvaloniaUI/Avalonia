using Avalonia.Rendering.Composition.Drawing;
using Xunit;

namespace Avalonia.Base.UnitTests.Rendering.SceneGraph
{
    public class RenderDataResourcesTests
    {
        [Fact]
        public void Intern_Null_Returns_Null_Handle()
        {
            var resources = new RenderDataResources();
            Assert.Equal(RenderDataResources.NullHandle, resources.Intern(null));
            Assert.Equal(0, resources.Count);
        }

        [Fact]
        public void Intern_Same_Reference_Returns_Same_Handle()
        {
            var resources = new RenderDataResources();
            var obj = new object();

            var first = resources.Intern(obj);
            var second = resources.Intern(obj);

            Assert.Equal(first, second);
            Assert.Equal(1, resources.Count);
        }

        [Fact]
        public void Intern_Distinct_References_Return_Distinct_Handles()
        {
            var resources = new RenderDataResources();
            var a = resources.Intern(new object());
            var b = resources.Intern(new object());

            Assert.NotEqual(a, b);
            Assert.Equal(2, resources.Count);
        }

        [Fact]
        public void Intern_Equal_But_Distinct_References_Return_Distinct_Handles()
        {
            var resources = new RenderDataResources();
            var a = resources.Intern(new EqualByValue());
            var b = resources.Intern(new EqualByValue());

            Assert.NotEqual(a, b);
            Assert.Equal(2, resources.Count);
        }

        [Fact]
        public void Indexer_Returns_Interned_Resource()
        {
            var resources = new RenderDataResources();
            var obj = new object();

            var handle = resources.Intern(obj);

            Assert.Same(obj, resources[handle]);
        }

        [Fact]
        public void Indexer_Null_Handle_Returns_Null()
        {
            var resources = new RenderDataResources();
            resources.Intern(new object());

            Assert.Null(resources[RenderDataResources.NullHandle]);
        }

        [Fact]
        public void Add_Appends_Without_Deduplication()
        {
            var resources = new RenderDataResources();
            var obj = new object();

            var first = resources.Add(obj);
            var second = resources.Add(obj);

            Assert.NotEqual(first, second);
            Assert.Equal(2, resources.Count);
            Assert.Same(obj, resources[first]);
            Assert.Same(obj, resources[second]);
        }

        [Fact]
        public void Dispose_Resets_The_Table()
        {
            var resources = new RenderDataResources();
            resources.Intern(new object());

            resources.Dispose();

            Assert.Equal(0, resources.Count);
        }

        private sealed class EqualByValue
        {
            public override bool Equals(object? obj) => obj is EqualByValue;
            public override int GetHashCode() => 1;
        }
    }
}

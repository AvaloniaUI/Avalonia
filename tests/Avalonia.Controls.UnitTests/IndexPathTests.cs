using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class IndexPathTests
    {
        [Fact]
        public void Simple_Index()
        {
            var a = new IndexPath(1);

            Assert.Equal(1, a.GetSize());
            Assert.Equal(1, a.GetAt(0));
        }

        [Fact]
        public void Equal_Paths()
        {
            var a = new IndexPath(1);
            var b = new IndexPath(1);

            Assert.True(a == b);
            Assert.False(a != b);
            Assert.True(a.Equals(b));
            Assert.Equal(0, a.CompareTo(b));
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void Unequal_Paths()
        {
            var a = new IndexPath(1);
            var b = new IndexPath(2);

            Assert.False(a == b);
            Assert.True(a != b);
            Assert.False(a.Equals(b));
            Assert.Equal(-1, a.CompareTo(b));
            Assert.NotEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void Equal_Null_Path()
        {
            var a = new IndexPath(null);
            var b = new IndexPath(null);

            Assert.True(a == b);
            Assert.False(a != b);
            Assert.True(a.Equals(b));
            Assert.Equal(0, a.CompareTo(b));
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void Unequal_Null_Path()
        {
            var a = new IndexPath(null);
            var b = new IndexPath(2);

            Assert.False(a == b);
            Assert.True(a != b);
            Assert.False(a.Equals(b));
            Assert.Equal(-1, a.CompareTo(b));
            Assert.NotEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void Default_Is_Null_Path()
        {
            var a = new IndexPath(null);
            var b = default(IndexPath);

            Assert.True(a == b);
            Assert.False(a != b);
            Assert.True(a.Equals(b));
            Assert.Equal(0, a.CompareTo(b));
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void Null_Equality()
        {
            var a = new IndexPath(null);
            var b = new IndexPath(1);

            // Implementing operator == on a struct automatically implements an operator which
            // accepts null, so make sure this does something useful.
            Assert.True(a == null);
            Assert.False(a != null);
            Assert.False(b == null);
            Assert.True(b != null);
        }
    }
}

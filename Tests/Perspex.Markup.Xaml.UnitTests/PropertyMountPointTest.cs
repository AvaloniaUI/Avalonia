namespace Perspex.Xaml.Base.UnitTest
{
    using Markup.Xaml.DataBinding.ChangeTracking;
    using System;
    using Xunit;

    public class PropertyMountPointTest
    {
        [Fact]
        public void SourceAndPathAreNull()
        {
            Assert.Throws<ArgumentNullException>(() => new PropertyMountPoint(null, null));
        }
    }
}

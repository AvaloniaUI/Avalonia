// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Markup.Xaml.DataBinding.ChangeTracking;
using System;
using Xunit;

namespace Perspex.Xaml.Base.UnitTest
{
    public class PropertyMountPointTest
    {
        [Fact]
        public void SourceAndPathAreNull()
        {
            Assert.Throws<ArgumentNullException>(() => new PropertyMountPoint(null, null));
        }
    }
}

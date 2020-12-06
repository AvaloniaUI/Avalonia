using System;
using System.Linq;
using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Base.UnitTests.Utilities
{
    public class UriExtensionsTests
    {
        [Fact]
        public void Values_For_Query_Uri_Parsed()
        {
            const string key = "assembly";
            const string value = "Avalonia.Themes.Default";

            var uri = new Uri("resm:Avalonia.Themes.Default.Accents.BaseLight.xaml?"+ $"{key}={value}");
            var parsed = uri.ParseQueryString();
            
            Assert.True(parsed.Count == 1);
            Assert.Equal(key, parsed.Keys.First());
            Assert.Equal(value, parsed.Values.First());
        }
    }
}

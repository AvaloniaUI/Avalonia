using System;
using System.Reflection;
using Avalonia.Shared.PlatformSupport;
using Avalonia.Shared.PlatformSupport.Internal;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Platform
{
    public class AssetLoaderTests
    {
        public class MockAssembly : Assembly {}

        private const string TestAssemblyName = "Awesome Library";

        static AssetLoaderTests()
        {
            var assembly = Mock.Of<MockAssembly>();
            Mock.Get(assembly).Setup(x => x.GetName()).Returns(new AssemblyName(TestAssemblyName));
            Mock.Get(assembly).Setup(x => x.FullName).Returns(TestAssemblyName);

            var descriptor = Mock.Of<IAssemblyDescriptor>();
            Mock.Get(descriptor).Setup(x => x.Assembly).Returns(assembly);

            var resolver = Mock.Of<IAssemblyDescriptorResolver>();
            Mock.Get(resolver).Setup(x => x.Get(TestAssemblyName)).Returns(descriptor);

            AssetLoader.SetAssemblyDescriptorResolver(resolver);
        }

        [Fact]
        public void AssemblyName_With_Whitespace_Should_Load_Resm()
        {
            var uri = new Uri($"resm:Avalonia.Base.UnitTests.Assets.something?assembly={TestAssemblyName}");
            var loader = new AssetLoader();

            var assemblyActual = loader.GetAssembly(uri, null);

            Assert.Equal(TestAssemblyName, assemblyActual.FullName);
        }
    }
}

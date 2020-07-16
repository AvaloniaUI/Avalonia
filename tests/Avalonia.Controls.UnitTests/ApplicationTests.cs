using System;
using Avalonia.Data;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class ApplicationTests
    {
        [Fact]
        public void Throws_ArgumentNullException_On_Run_If_MainWindow_Is_Null()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                Assert.Throws<ArgumentNullException>(() => { Application.Current.Run(null); });
            }
        }

        [Fact]
        public void Raises_ResourcesChanged_When_Event_Handler_Added_After_Resources_Has_Been_Accessed()
        {
            // Test for #1765.
            using (UnitTestApplication.Start())
            {
                var resources = Application.Current.Resources;
                var raised = false;

                Application.Current.ResourcesChanged += (s, e) => raised = true;
                resources["foo"] = "bar";

                Assert.True(raised);
            }
        }

        [Fact]
        public void Can_Bind_To_DataContext()
        {
            using (UnitTestApplication.Start())
            {
                var application = Application.Current;

                application.DataContext = "Test";

                application.Bind(Application.NameProperty, new Binding("."));

                Assert.Equal("Test", Application.Current.Name);
            }
        }
    }
}

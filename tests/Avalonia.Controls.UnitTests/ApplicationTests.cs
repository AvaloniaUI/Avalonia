// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia.Threading;
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
    }
}

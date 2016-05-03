// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Controls.Templates;
using Perspex.Styling;
using Perspex.Utilities;
using Xunit;

namespace Perspex.Controls.UnitTests
{
    public class ProgressBarTests
    {
        [Fact]
        public void ProgressBar_Should_Fill_Indicator_If_Mininum_Equal_To_Maximum()
        {
            var progressBar = new ProgressBar
            {
                Template = new FuncControlTemplate(CreateProgressBarTemplate),
                Minimum = 0.0,
                Maximum = 0.4,
                Width = 100
            };

            progressBar.Maximum -= 0.3;
            progressBar.Maximum -= 0.1; // It is close to zero now, but not equal due to floating point error

            progressBar.ApplyTemplate();
            progressBar.Arrange(new Rect(new Size(100, 100)));

            var bounds = progressBar.Bounds;
            var indicator = progressBar.Indicator;

            Assert.True(MathUtilities.Equal(bounds.Size.Width, indicator.Width));
        }

        private Border CreateProgressBarTemplate(ITemplatedControl arg)
        {
            return new Border
            {
                Name = "PART_Indicator"
            };
        }
    }
}

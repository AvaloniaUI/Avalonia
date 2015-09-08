// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Controls.Primitives;
using Xunit;

namespace Perspex.Controls.UnitTests.Primitives
{
    public class RangeBaseTests
    {
        [Fact]
        public void Maximum_Should_Be_Coerced_To_Minimum()
        {
            var target = new TestRange
            {
                Minimum = 100,
                Maximum = 50,
            };

            Assert.Equal(100, target.Minimum);
            Assert.Equal(100, target.Maximum);
        }

        [Fact]
        public void Value_Should_Be_Coerced_To_Range()
        {
            var target = new TestRange
            {
                Minimum = 0,
                Maximum = 50,
                Value = 100,
            };

            Assert.Equal(0, target.Minimum);
            Assert.Equal(50, target.Maximum);
            Assert.Equal(50, target.Value);
        }

        [Fact]
        public void Changing_Minimum_Should_Coerce_Value_And_Maximum()
        {
            var target = new TestRange
            {
                Minimum = 0,
                Maximum = 100,
                Value = 50,
            };

            target.Minimum = 200;

            Assert.Equal(200, target.Minimum);
            Assert.Equal(200, target.Maximum);
            Assert.Equal(200, target.Value);
        }

        [Fact]
        public void Changing_Maximum_Should_Coerce_Value()
        {
            var target = new TestRange
            {
                Minimum = 0,
                Maximum = 100,
                Value = 100,
            };

            target.Maximum = 50;

            Assert.Equal(0, target.Minimum);
            Assert.Equal(50, target.Maximum);
            Assert.Equal(50, target.Value);
        }

        [Fact]
        public void Properties_Should_Not_Accept_Nan_And_Inifinity()
        {
            var target = new TestRange();

            Assert.Throws<ArgumentException>(() => target.Minimum = double.NaN);
            Assert.Throws<ArgumentException>(() => target.Minimum = double.PositiveInfinity);
            Assert.Throws<ArgumentException>(() => target.Minimum = double.NegativeInfinity);
            Assert.Throws<ArgumentException>(() => target.Maximum = double.NaN);
            Assert.Throws<ArgumentException>(() => target.Maximum = double.PositiveInfinity);
            Assert.Throws<ArgumentException>(() => target.Maximum = double.NegativeInfinity);
            Assert.Throws<ArgumentException>(() => target.Value = double.NaN);
            Assert.Throws<ArgumentException>(() => target.Value = double.PositiveInfinity);
            Assert.Throws<ArgumentException>(() => target.Value = double.NegativeInfinity);
        }

        private class TestRange : RangeBase
        {
        }
    }
}
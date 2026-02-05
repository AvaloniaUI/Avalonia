using System;
using System.ComponentModel.DataAnnotations;
using Avalonia.Data;
using Avalonia.Data.Core.Plugins;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Data.Core.Plugins
{
    [InvariantCulture]
    public class DataAnnotationsValidationPluginTests
    {
        [Fact]
        public void Should_Match_Property_With_ValidatorAttribute()
        {
            var target = new DataAnnotationsValidationPlugin();
            var data = new Data();

            Assert.True(target.Match(data, nameof(Data.Between5And10)));
        }

        [Fact]
        public void Should_Match_Property_With_Multiple_ValidatorAttributes()
        {
            var target = new DataAnnotationsValidationPlugin();
            var data = new Data();

            Assert.True(target.Match(data, nameof(Data.PhoneNumber)));
        }

        [Fact]
        public void Should_Not_Match_Property_Without_ValidatorAttribute()
        {
            var target = new DataAnnotationsValidationPlugin();
            var data = new Data();

            Assert.False(target.Match(data, nameof(Data.Unvalidated)));
        }

        [Fact]
        public void Produces_Range_BindingNotificationsx()
        {
            var data = new Data();
            var validatorPlugin = new DataAnnotationsValidationPlugin();
            var validator = validatorPlugin.Start(data, nameof(data.Between5And10));
            var errorMessage = new RangeAttribute(5, 10).FormatErrorMessage(nameof(Data.Between5And10));

            Assert.False(validator.RaisesEvents);

            data.Between5And10 = 3;
            var error = Assert.IsType<DataValidationException>(validator.GetDataValidationError());
            Assert.Equal(errorMessage, error.Message);

            data.Between5And10 = 7;
            Assert.Null(validator.GetDataValidationError());

            data.Between5And10 = 11;
            error = Assert.IsType<DataValidationException>(validator.GetDataValidationError());
            Assert.Equal(errorMessage, error.Message);
        }

        private class Data
        {
            [Range(5, 10)]
            public int Between5And10 { get; set; } = 5;

            public int Unvalidated { get; set; }

            [Phone]
            [MaxLength(10)]
            public string? PhoneNumber { get; set; }
        }
    }
}

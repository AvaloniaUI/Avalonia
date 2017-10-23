// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Avalonia.Data;
using Avalonia.Markup.Data.Plugins;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.UnitTests.Data.Plugins
{
    public class DataAnnotationsValidationPluginTests : IClassFixture<InvariantCultureFixture>
    {
        [Fact]
        public void Should_Match_Property_With_ValidatorAttribute()
        {
            var target = new DataAnnotationsValidationPlugin();
            var data = new Data();

            Assert.True(target.Match(new WeakReference(data), nameof(Data.Between5And10)));
        }

        [Fact]
        public void Should_Match_Property_With_Multiple_ValidatorAttributes()
        {
            var target = new DataAnnotationsValidationPlugin();
            var data = new Data();

            Assert.True(target.Match(new WeakReference(data), nameof(Data.PhoneNumber)));
        }

        [Fact]
        public void Should_Not_Match_Property_Without_ValidatorAttribute()
        {
            var target = new DataAnnotationsValidationPlugin();
            var data = new Data();

            Assert.False(target.Match(new WeakReference(data), nameof(Data.Unvalidated)));
        }

        [Fact]
        public void Produces_Range_BindingNotificationsx()
        {
            var inpcAccessorPlugin = new InpcPropertyAccessorPlugin();
            var validatorPlugin = new DataAnnotationsValidationPlugin();
            var data = new Data();
            var accessor = inpcAccessorPlugin.Start(new WeakReference(data), nameof(data.Between5And10));
            var validator = validatorPlugin.Start(new WeakReference(data), nameof(data.Between5And10), accessor);
            var result = new List<object>();
            
            var errmsg = new RangeAttribute(5, 10).FormatErrorMessage(nameof(Data.Between5And10));

            validator.Subscribe(x => result.Add(x));
            validator.SetValue(3, BindingPriority.LocalValue);
            validator.SetValue(7, BindingPriority.LocalValue);
            validator.SetValue(11, BindingPriority.LocalValue);

            Assert.Equal(new[]
            {
                new BindingNotification(5),
                new BindingNotification(
                    new ValidationException(errmsg),
                    BindingErrorType.DataValidationError,
                    3),
                new BindingNotification(7),
                new BindingNotification(
                    new ValidationException(errmsg),
                    BindingErrorType.DataValidationError,
                    11),
            }, result);
        }

        [Fact]
        public void Produces_Aggregate_BindingNotificationsx()
        {
            var inpcAccessorPlugin = new InpcPropertyAccessorPlugin();
            var validatorPlugin = new DataAnnotationsValidationPlugin();
            var data = new Data();
            var accessor = inpcAccessorPlugin.Start(new WeakReference(data), nameof(data.PhoneNumber));
            var validator = validatorPlugin.Start(new WeakReference(data), nameof(data.PhoneNumber), accessor);
            var result = new List<object>();

            validator.Subscribe(x => result.Add(x));
            validator.SetValue("123456", BindingPriority.LocalValue);
            validator.SetValue("abcdefghijklm", BindingPriority.LocalValue);

            Assert.Equal(new[]
            {
                new BindingNotification(null),
                new BindingNotification("123456"),
                new BindingNotification(
                    new AggregateException(
                        new ValidationException("The PhoneNumber field is not a valid phone number."),
                        new ValidationException("The field PhoneNumber must be a string or array type with a maximum length of '10'.")),
                    BindingErrorType.DataValidationError,
                    "abcdefghijklm"),
            }, result);
        }

        private class Data
        {
            [Range(5, 10)]
            public int Between5And10 { get; set; } = 5;

            public int Unvalidated { get; set; }

            [Phone]
            [MaxLength(10)]
            public string PhoneNumber { get; set; }
        }
    }
}

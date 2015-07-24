// -----------------------------------------------------------------------
// <copyright file="PerspexObjectTests_Validation.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Base.UnitTests
{
    using System;
    using Xunit;

    public class PerspexObjectTests_Validation
    {
        [Fact]
        public void SetValue_Causes_Validation()
        {
            Class1 target = new Class1();

            target.SetValue(Class1.QuxProperty, 5);
            Assert.Throws<ArgumentOutOfRangeException>(() => target.SetValue(Class1.QuxProperty, 25));
            Assert.Equal(5, target.GetValue(Class1.QuxProperty));
        }

        [Fact]
        public void SetValue_Causes_Coercion()
        {
            Class1 target = new Class1();

            target.SetValue(Class1.QuxProperty, 5);
            Assert.Equal(5, target.GetValue(Class1.QuxProperty));
            target.SetValue(Class1.QuxProperty, -5);
            Assert.Equal(0, target.GetValue(Class1.QuxProperty));
            target.SetValue(Class1.QuxProperty, 15);
            Assert.Equal(10, target.GetValue(Class1.QuxProperty));
        }

        [Fact]
        public void Revalidate_Causes_Recoercion()
        {
            Class1 target = new Class1();

            target.SetValue(Class1.QuxProperty, 7);
            Assert.Equal(7, target.GetValue(Class1.QuxProperty));
            target.MaxQux = 5;
            target.Revalidate(Class1.QuxProperty);
        }

        private class Class1 : PerspexObject
        {
            public static readonly PerspexProperty<int> QuxProperty =
                PerspexProperty.Register<Class1, int>("Qux", validate: Coerce);

            public Class1()
            {
                this.MaxQux = 10;
                this.ErrorQux = 20;
            }

            public int MaxQux { get; set; }

            public int ErrorQux { get; set; }

            private static int Coerce(PerspexObject instance, int value)
            {
                if (value > ((Class1)instance).ErrorQux)
                {
                    throw new ArgumentOutOfRangeException();
                }

                return Math.Min(Math.Max(value, 0), ((Class1)instance).MaxQux);
            }
        }
    }
}

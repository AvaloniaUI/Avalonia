// -----------------------------------------------------------------------
// <copyright file="PerspexObjectTests_Coercion.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Base.UnitTests
{
    using System;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using Xunit;

    public class PerspexObjectTests_Coercion
    {
        [Fact]
        public void CoerceValue_Causes_Recoercion()
        {
            Class1 target = new Class1();

            target.SetValue(Class1.QuxProperty, 7);
            Assert.Equal(7, target.GetValue(Class1.QuxProperty));
            target.MaxQux = 5;
            target.CoerceValue(Class1.QuxProperty);
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

        private class Class1 : PerspexObject
        {
            public static readonly PerspexProperty<int> QuxProperty =
                PerspexProperty.Register<Class1, int>("Qux", coerce: Coerce);

            public Class1()
            {
                this.MaxQux = 10;
            }

            public int MaxQux { get; set; }

            private static int Coerce(PerspexObject instance, int value)
            {
                return Math.Min(Math.Max(value, 0), ((Class1)instance).MaxQux);
            }
        }
    }
}

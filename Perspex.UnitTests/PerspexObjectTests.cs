// -----------------------------------------------------------------------
// <copyright file="PerspexObjectTests.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.UnitTests
{
    using System;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class PerspexObjectTests
    {
        [TestInitialize]
        public void Initialize()
        {
            // Ensure properties are registered.
            PerspexProperty p;
            p = Class1.FooProperty;
            p = Class2.BarProperty;
        }

        [TestMethod]
        public void GetProperties_Returns_Registered_Properties()
        {
            string[] names = PerspexObject.GetProperties(typeof(Class1)).Select(x => x.Name).ToArray();

            CollectionAssert.AreEqual(new[] { "Foo", "Baz" }, names);
        }

        [TestMethod]
        public void GetProperties_Returns_Registered_Properties_For_Base_Types()
        {
            string[] names = PerspexObject.GetProperties(typeof(Class2)).Select(x => x.Name).ToArray();

            CollectionAssert.AreEqual(new[] { "Bar", "Foo", "Baz" }, names);
        }

        [TestMethod]
        public void GetValue_Returns_Default_Value()
        {
            Class1 target = new Class1();

            Assert.AreEqual("foodefault", target.GetValue(Class1.FooProperty));
        }

        [TestMethod]
        public void GetValue_Returns_Overridden_Default_Value()
        {
            Class2 target = new Class2();

            Assert.AreEqual("foooverride", target.GetValue(Class1.FooProperty));
        }

        [TestMethod]
        public void GetValue_Returns_Set_Value()
        {
            Class1 target = new Class1();

            target.SetValue(Class1.FooProperty, "newvalue");

            Assert.AreEqual("newvalue", target.GetValue(Class1.FooProperty));
        }

        [TestMethod]
        public void GetValue_Returns_Inherited_Value()
        {
            Class1 parent = new Class1();
            Class2 child = new Class2 { Parent = parent };

            parent.SetValue(Class1.BazProperty, "changed");

            Assert.AreEqual("changed", child.GetValue(Class1.BazProperty));
        }

        [TestMethod]
        public void ClearValue_Clears_Value()
        {
            Class1 target = new Class1();

            target.SetValue(Class1.FooProperty, "newvalue");
            target.ClearValue(Class1.FooProperty);

            Assert.AreEqual("foodefault", target.GetValue(Class1.FooProperty));
        }

        [TestMethod]
        public void SetValue_Sets_Value()
        {
            Class1 target = new Class1();

            target.SetValue(Class1.FooProperty, "newvalue");

            Assert.AreEqual("newvalue", target.GetValue(Class1.FooProperty));
        }

        [TestMethod]
        public void SetValue_Raises_PropertyChanged()
        {
            Class1 target = new Class1();
            bool raised = false;

            target.PropertyChanged += (s, e) =>
            {
                raised = s == target &&
                         e.Property == Class1.FooProperty &&
                         (string)e.OldValue == "foodefault" &&
                         (string)e.NewValue == "newvalue";
            };

            target.SetValue(Class1.FooProperty, "newvalue");

            Assert.IsTrue(raised);
        }

        [TestMethod]
        public void SetValue_Doesnt_Raise_PropertyChanged_If_Value_Not_Changed()
        {
            Class1 target = new Class1();
            bool raised = false;

            target.PropertyChanged += (s, e) =>
            {
                raised = true;
            };

            target.SetValue(Class1.FooProperty, "foodefault");

            Assert.IsFalse(raised);
        }

        [TestMethod]
        public void GetObservable_Returns_Initial_Value()
        {
            Class1 target = new Class1();
            bool raised = false;

            target.GetObservable(Class1.FooProperty).Subscribe(x => raised = x == "foodefault");

            Assert.IsTrue(raised);
        }

        [TestMethod]
        public void GetObservable_Returns_Property_Change()
        {
            Class1 target = new Class1();
            bool raised = false;

            target.GetObservable(Class1.FooProperty).Subscribe(x => raised = x == "newvalue");
            raised = false;
            target.SetValue(Class1.FooProperty, "newvalue");

            Assert.IsTrue(raised);
        }

        [TestMethod]
        public void GetObservable_Returns_Property_Change_Only_For_Correct_Property()
        {
            Class2 target = new Class2();
            bool raised = false;

            target.GetObservable(Class1.FooProperty).Subscribe(x => raised = true);
            raised = false;
            target.SetValue(Class2.BarProperty, "newvalue");

            Assert.IsFalse(raised);
        }

        [TestMethod]
        public void GetObservable_Dispose_Stops_Property_Changes()
        {
            Class1 target = new Class1();
            bool raised = false;

            target.GetObservable(Class1.FooProperty)
                  .Subscribe(x => raised = true)
                  .Dispose();
            raised = false;
            target.SetValue(Class1.FooProperty, "newvalue");

            Assert.IsFalse(raised);
        }

        [TestMethod]
        public void Setting_InheritanceParent_Raises_PropertyChanged_When_Value_Changed_In_Parent()
        {
            bool raised = false;

            Class1 parent = new Class1();
            parent.SetValue(Class1.BazProperty, "changed");

            Class2 child = new Class2();
            child.PropertyChanged += (s, e) =>
                raised = s == child &&
                         e.Property == Class1.BazProperty &&
                         (string)e.OldValue == "bazdefault" &&
                         (string)e.NewValue == "changed";

            child.Parent = parent;

            Assert.IsTrue(raised);
        }

        [TestMethod]
        public void Setting_InheritanceParent_Doesnt_Raise_PropertyChanged_When_Local_Value_Set()
        {
            bool raised = false;

            Class1 parent = new Class1();
            parent.SetValue(Class1.BazProperty, "changed");

            Class2 child = new Class2();
            child.SetValue(Class1.BazProperty, "localvalue");
            child.PropertyChanged += (s, e) => raised = true;

            child.Parent = parent;

            Assert.IsFalse(raised);
        }

        [TestMethod]
        public void Setting_Value_In_InheritanceParent_Raises_PropertyChanged()
        {
            bool raised = false;

            Class1 parent = new Class1();

            Class2 child = new Class2();
            child.PropertyChanged += (s, e) =>
                raised = s == child &&
                         e.Property == Class1.BazProperty &&
                         (string)e.OldValue == "bazdefault" &&
                         (string)e.NewValue == "changed";
            child.Parent = parent;

            parent.SetValue(Class1.BazProperty, "changed");

            Assert.IsTrue(raised);
        }

        [TestMethod]
        public void Binding_Sets_Current_Value()
        {
            Class1 target = new Class1();
            Class1 source = new Class1();

            source.SetValue(Class1.FooProperty, "initial");
            target.SetValue(Class1.FooProperty, source.GetObservable(Class1.FooProperty));

            Assert.AreEqual("initial", target.GetValue(Class1.FooProperty));
        }

        [TestMethod]
        public void Binding_NonGeneric_Sets_Current_Value()
        {
            Class1 target = new Class1();
            Class1 source = new Class1();

            source.SetValue(Class1.FooProperty, "initial");
            target.SetValue((PerspexProperty)Class1.FooProperty, source.GetObservable(Class1.FooProperty));

            Assert.AreEqual("initial", target.GetValue(Class1.FooProperty));
        }

        [TestMethod]
        public void Binding_Sets_Subsequent_Value()
        {
            Class1 target = new Class1();
            Class1 source = new Class1();

            source.SetValue(Class1.FooProperty, "initial");
            target.SetValue(Class1.FooProperty, source.GetObservable(Class1.FooProperty));
            source.SetValue(Class1.FooProperty, "subsequent");

            Assert.AreEqual("subsequent", target.GetValue(Class1.FooProperty));
        }

        [TestMethod]
        public void Binding_Doesnt_Set_Value_After_Clear()
        {
            Class1 target = new Class1();
            Class1 source = new Class1();

            source.SetValue(Class1.FooProperty, "initial");
            target.SetValue(Class1.FooProperty, source.GetObservable(Class1.FooProperty));
            target.ClearValue(Class1.FooProperty);
            source.SetValue(Class1.FooProperty, "newvalue");

            Assert.AreEqual("foodefault", target.GetValue(Class1.FooProperty));
        }

        [TestMethod]
        public void Binding_Doesnt_Set_Value_After_Reset()
        {
            Class1 target = new Class1();
            Class1 source = new Class1();

            source.SetValue(Class1.FooProperty, "initial");
            target.SetValue(Class1.FooProperty, source.GetObservable(Class1.FooProperty));
            target.SetValue(Class1.FooProperty, "reset");
            source.SetValue(Class1.FooProperty, "newvalue");

            Assert.AreEqual("reset", target.GetValue(Class1.FooProperty));
        }

        [TestMethod]
        public void Setting_UnsetValue_Reverts_To_Default_Value()
        {
            Class1 target = new Class1();

            target.SetValue(Class1.FooProperty, "newvalue");
            target.SetValue(Class1.FooProperty, PerspexProperty.UnsetValue);

            Assert.AreEqual("foodefault", target.GetValue(Class1.FooProperty));
        }

        [TestMethod]
        public void StyleBinding_Overrides_Default_Value()
        {
            Class1 target = new Class1();

            target.SetValue(Class1.FooProperty, "stylevalue", Observable.Return(true));

            Assert.AreEqual("stylevalue", target.GetValue(Class1.FooProperty));
        }

        [TestMethod]
        public void StyleBinding_Doesnt_Override_Local_Value()
        {
            Class1 target = new Class1();

            target.SetValue(Class1.FooProperty, "newvalue");
            target.SetValue(Class1.FooProperty, "stylevalue", Observable.Return(true));

            Assert.AreEqual("newvalue", target.GetValue(Class1.FooProperty));
        }

        [TestMethod]
        public void StyleBinding_Deactivated_Doesnt_Override_Default_Value()
        {
            Class1 target = new Class1();

            target.SetValue(Class1.FooProperty, "stylevalue", Observable.Return(false));

            Assert.AreEqual("foodefault", target.GetValue(Class1.FooProperty));
        }

        [TestMethod]
        public void StyleBinding_Toggles_On_Activation()
        {
            Class1 target = new Class1();

            Subject<bool> source = new Subject<bool>();
            target.SetValue(Class1.FooProperty, "stylevalue", source);

            Assert.AreEqual("foodefault", target.GetValue(Class1.FooProperty));
            source.OnNext(true);
            Assert.AreEqual("stylevalue", target.GetValue(Class1.FooProperty));
            source.OnNext(false);
            Assert.AreEqual("foodefault", target.GetValue(Class1.FooProperty));
        }

        [TestMethod]
        public void StyleBinding_Detaches_OnCompleted()
        {
            Class1 target = new Class1();

            Subject<bool> source = new Subject<bool>();
            target.SetValue(Class1.FooProperty, "stylevalue", source);

            Assert.AreEqual("foodefault", target.GetValue(Class1.FooProperty));
            source.OnNext(true);
            Assert.AreEqual("stylevalue", target.GetValue(Class1.FooProperty));
            source.OnCompleted();
            Assert.AreEqual("foodefault", target.GetValue(Class1.FooProperty));
        }

        [TestMethod]
        public void Later_StyleBindings_Have_Precedence()
        {
            Class1 target = new Class1();

            Subject<bool> source1 = new Subject<bool>();
            Subject<bool> source2 = new Subject<bool>();
            target.SetValue(Class1.FooProperty, "style1", source1);
            target.SetValue(Class1.FooProperty, "style2", source2);

            Assert.AreEqual("foodefault", target.GetValue(Class1.FooProperty));
            source1.OnNext(true);
            Assert.AreEqual("style1", target.GetValue(Class1.FooProperty));
            source2.OnNext(true);
            Assert.AreEqual("style2", target.GetValue(Class1.FooProperty));
            source1.OnNext(false);
            Assert.AreEqual("style2", target.GetValue(Class1.FooProperty));
            source2.OnNext(false);
            Assert.AreEqual("foodefault", target.GetValue(Class1.FooProperty));
            source2.OnNext(true);
            Assert.AreEqual("style2", target.GetValue(Class1.FooProperty));
            source1.OnNext(true);
            Assert.AreEqual("style2", target.GetValue(Class1.FooProperty));
        }

        private class Class1 : PerspexObject
        {
            public static readonly PerspexProperty<string> FooProperty =
                PerspexProperty.Register<Class1, string>("Foo", "foodefault");

            public static readonly PerspexProperty<string> BazProperty =
                PerspexProperty.Register<Class1, string>("Baz", "bazdefault", true);
        }

        private class Class2 : Class1
        {
            public static readonly PerspexProperty<string> BarProperty =
                PerspexProperty.Register<Class2, string>("Bar", "bardefault");

            static Class2()
            {
                FooProperty.OverrideDefaultValue(typeof(Class2), "foooverride");
            }

            public Class1 Parent
            {
                get { return (Class1)this.InheritanceParent; }
                set { this.InheritanceParent = value; }
            }
        }
    }
}

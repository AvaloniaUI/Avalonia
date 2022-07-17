using System;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class ClassesTests
    {
        [Fact]
        public void Duplicates_Should_Not_Be_Added()
        {
            var target = new Classes();

            target.Add("foo");
            target.Add("foo");

            Assert.Equal(new[] { "foo" }, target);
        }

        [Fact]
        public void Duplicates_Should_Not_Be_Added_Via_AddRange()
        {
            var target = new Classes();

            target.Add("foo");
            target.AddRange(new[] { "foo", "bar" });

            Assert.Equal(new[] { "foo", "bar" }, target);
        }

        [Fact]
        public void Duplicates_Should_Not_Be_Added_Via_Pseudoclasses()
        {
            var target = new Classes();
            var ps = (IPseudoClasses)target;

            ps.Add(":foo");
            ps.Add(":foo");

            Assert.Equal(new[] { ":foo" }, target);
        }

        [Fact]
        public void Duplicates_Should_Not_Be_Inserted()
        {
            var target = new Classes();

            target.Add("foo");
            target.Insert(0, "foo");

            Assert.Equal(new[] { "foo" }, target);
        }

        [Fact]
        public void Duplicates_Should_Not_Be_Inserted_Via_InsertRange()
        {
            var target = new Classes();

            target.Add("foo");
            target.InsertRange(1, new[] { "foo", "bar" });

            Assert.Equal(new[] { "foo", "bar" }, target);
        }

        [Fact]
        public void Should_Not_Be_Able_To_Add_Pseudoclass()
        {
            var target = new Classes();

            Assert.Throws<ArgumentException>(() => target.Add(":foo"));
        }

        [Fact]
        public void Should_Not_Be_Able_To_Add_Pseudoclasses_Via_AddRange()
        {
            var target = new Classes();

            Assert.Throws<ArgumentException>(() => target.AddRange(new[] { "foo", ":bar" }));
        }

        [Fact]
        public void Should_Not_Be_Able_To_Insert_Pseudoclass()
        {
            var target = new Classes();

            Assert.Throws<ArgumentException>(() => target.Insert(0, ":foo"));
        }

        [Fact]
        public void Should_Not_Be_Able_To_Insert_Pseudoclasses_Via_InsertRange()
        {
            var target = new Classes();

            Assert.Throws<ArgumentException>(() => target.InsertRange(0, new[] { "foo", ":bar" }));
        }

        [Fact]
        public void Should_Not_Be_Able_To_Remove_Pseudoclass()
        {
            var target = new Classes();

            Assert.Throws<ArgumentException>(() => target.Remove(":foo"));
        }

        [Fact]
        public void Should_Not_Be_Able_To_Remove_Pseudoclasses_Via_RemoveAll()
        {
            var target = new Classes();

            Assert.Throws<ArgumentException>(() => target.RemoveAll(new[] { "foo", ":bar" }));
        }

        [Fact]
        public void Should_Not_Be_Able_To_Remove_Pseudoclasses_Via_RemoveRange()
        {
            var target = new Classes();

            Assert.Throws<ArgumentException>(() => target.RemoveRange(0, 1));
        }

        [Fact]
        public void Should_Not_Be_Able_To_Remove_Pseudoclass_Via_RemoveAt()
        {
            var target = new Classes();

            ((IPseudoClasses)target).Add(":foo");

            Assert.Throws<ArgumentException>(() => target.RemoveAt(0));
        }

        [Fact]
        public void Replace_Should_Not_Replace_Pseudoclasses()
        {
            var target = new Classes("foo", "bar");

            ((IPseudoClasses)target).Add(":baz");

            target.Replace(new[] { "qux" });

            Assert.Equal(new[] { ":baz", "qux" }, target);
        }

        [Fact]
        public void Replace_Should_Not_Accept_Pseudoclasses()
        {
            var target = new Classes();

            Assert.Throws<ArgumentException>(() => target.Replace(new[] { ":qux" }));
        }

        [Fact]
        public void Clear_Should_Not_Remove_Pseudoclasses()
        {
            var target = new Classes("foo", "bar");

            ((IPseudoClasses)target).Add(":baz");

            target.Clear();

            Assert.Equal(new[] { ":baz" }, target);
        }

        [Fact]
        public void RemoveAll_Should_Remove_Classes()
        {
            var target = new Classes("foo", "bar", "baz");

            target.RemoveAll(new[] { "bar", "baz" });

            Assert.Equal(new[] { "foo" }, target);
        }

        [Fact]
        public void Listeners_Can_Be_Added_By_Listener()
        {
            var classes = new Classes();
            var listener1 = new ClassesChangedListener(() => { });
            var listener2 = new ClassesChangedListener(() => classes.AddListener(listener1));

            classes.AddListener(listener2);
            classes.Add("bar");
        }

        [Fact]
        public void Listeners_Can_Be_Removed_By_Listener()
        {
            var classes = new Classes();
            var listener1 = new ClassesChangedListener(() => { });
            var listener2 = new ClassesChangedListener(() => classes.RemoveListener(listener1));

            classes.AddListener(listener1);
            classes.AddListener(listener2);
            classes.Add("bar");
        }

        private class ClassesChangedListener : IClassesChangedListener
        {
            private Action _action;

            public ClassesChangedListener(Action action) => _action = action;
            public void Changed() => _action();
        }
    }
}

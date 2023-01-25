using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia.Collections;
using Xunit;

namespace Avalonia.Base.UnitTests.Collections
{
    public class AvaloniaListTests
    {
        [Fact]
        public void Items_Passed_To_Constructor_Should_Appear_In_List()
        {
            var items = new[] { 1, 2, 3 };
            var target = new AvaloniaList<int>(items);

            Assert.Equal(items, target);
        }

        [Fact]
        public void AddRange_With_Null_Should_Throw_Exception()
        {
            var target = new AvaloniaList<int>();

            Assert.Throws<ArgumentNullException>(() => target.AddRange(null));
        }

        [Fact]
        public void RemoveAll_With_Null_Should_Throw_Exception()
        {
            var target = new AvaloniaList<int>();

            Assert.Throws<ArgumentNullException>(() => target.RemoveAll(null));
        }

        [Fact]
        public void InsertRange_With_Null_Should_Throw_Exception()
        {
            var target = new AvaloniaList<int>();

            Assert.Throws<ArgumentNullException>(() => target.InsertRange(1, null));
        }

        [Fact]
        public void InsertRange_Past_End_Should_Throw_Exception()
        {
            var target = new AvaloniaList<int>();

            Assert.Throws<ArgumentOutOfRangeException>(() => target.InsertRange(1, new List<int>() { 1 }));
        }

        [Fact]
        public void Move_Should_Update_Collection()
        {
            var target = new AvaloniaList<int>(new[] { 1, 2, 3 });

            target.Move(2, 0);

            Assert.Equal(new[] { 3, 1, 2 }, target);
        }

        [Fact]
        public void MoveRange_Should_Update_Collection()
        {
            var target = new AvaloniaList<int>(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });

            target.MoveRange(4, 3, 0);

            Assert.Equal(new[] { 5, 6, 7, 1, 2, 3, 4, 8, 9, 10 }, target);
        }

        [Fact]
        public void MoveRange_Can_Move_To_End()
        {
            var target = new AvaloniaList<int>(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });

            target.MoveRange(0, 5, 10);

            Assert.Equal(new[] { 6, 7, 8, 9, 10, 1, 2, 3, 4, 5 }, target);
        }

        [Fact]
        public void MoveRange_Raises_Correct_CollectionChanged_Event()
        {
            var target = new AvaloniaList<int>(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
            var raised = false;

            target.CollectionChanged += (s, e) =>
            {
                Assert.Equal(NotifyCollectionChangedAction.Move, e.Action);
                Assert.Equal(0, e.OldStartingIndex);
                Assert.Equal(10, e.NewStartingIndex);
                Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, e.OldItems);
                Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, e.NewItems);
                raised = true;
            };

            target.MoveRange(0, 9, 10);

            Assert.True(raised);
            Assert.Equal(new[] { 10, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, target);
        }

        [Fact]
        public void Adding_Item_Should_Raise_CollectionChanged()
        {
            var target = new AvaloniaList<int>(new[] { 1, 2 });
            var raised = false;

            target.CollectionChanged += (s, e) =>
            {
                Assert.Equal(target, s);
                Assert.Equal(NotifyCollectionChangedAction.Add, e.Action);
                Assert.Equal(new[] { 3 }, e.NewItems.Cast<int>());
                Assert.Equal(2, e.NewStartingIndex);

                raised = true;
            };

            target.Add(3);

            Assert.True(raised);
        }

        [Fact]
        public void Adding_Items_Should_Raise_CollectionChanged()
        {
            var target = new AvaloniaList<int>(new[] { 1, 2 });
            var raised = false;

            target.CollectionChanged += (s, e) =>
            {
                Assert.Equal(target, s);
                Assert.Equal(NotifyCollectionChangedAction.Add, e.Action);
                Assert.Equal(new[] { 3, 4 }, e.NewItems.Cast<int>());
                Assert.Equal(2, e.NewStartingIndex);

                raised = true;
            };

            target.AddRange(new[] { 3, 4 });

            Assert.True(raised);
        }

        [Fact]
        public void AddRange_IEnumerable_Should_Raise_Count_PropertyChanged()
        {
            var target = new AvaloniaList<int>(new[] { 1, 2, 3, 4, 5 });
            var raised = false;

            target.PropertyChanged += (s, e) => {
                Assert.Equal(e.PropertyName, nameof(target.Count));
                Assert.Equal(target.Count, 7);
                raised = true;
            };

            target.AddRange(Enumerable.Range(6, 2));

            Assert.True(raised);
        }

        [Fact]
        public void AddRange_Items_Should_Raise_Correct_CollectionChanged()
        {
            var target = new AvaloniaList<object>();

            var eventItems = new List<object>();

            target.CollectionChanged += (sender, args) =>
            {
                eventItems.AddRange(args.NewItems.Cast<object>());
            };
            
            target.AddRange(Enumerable.Range(0,10).Select(i => new object()));

            Assert.Equal(eventItems, target);
        }

        [Fact]
        public void Replacing_Item_Should_Raise_CollectionChanged()
        {
            var target = new AvaloniaList<int>(new[] { 1, 2 });
            var raised = false;

            target.CollectionChanged += (s, e) =>
            {
                Assert.Equal(target, s);
                Assert.Equal(NotifyCollectionChangedAction.Replace, e.Action);
                Assert.Equal(new[] { 2 }, e.OldItems.Cast<int>());
                Assert.Equal(new[] { 3 }, e.NewItems.Cast<int>());
                Assert.Equal(1, e.OldStartingIndex);
                Assert.Equal(1, e.NewStartingIndex);

                raised = true;
            };

            target[1] = 3;

            Assert.True(raised);
        }

        [Fact]
        public void Inserting_Item_Should_Raise_CollectionChanged()
        {
            var target = new AvaloniaList<int>(new[] { 1, 2 });
            var raised = false;

            target.CollectionChanged += (s, e) =>
            {
                Assert.Equal(target, s);
                Assert.Equal(NotifyCollectionChangedAction.Add, e.Action);
                Assert.Equal(new[] { 3 }, e.NewItems.Cast<int>());
                Assert.Equal(1, e.NewStartingIndex);

                raised = true;
            };

            target.Insert(1, 3);

            Assert.True(raised);
        }

        [Fact]
        public void Inserting_Items_Should_Raise_CollectionChanged()
        {
            var target = new AvaloniaList<int>(new[] { 1, 2 });
            var raised = false;

            target.CollectionChanged += (s, e) =>
            {
                Assert.Equal(target, s);
                Assert.Equal(NotifyCollectionChangedAction.Add, e.Action);
                Assert.Equal(new[] { 3, 4 }, e.NewItems.Cast<int>());
                Assert.Equal(1, e.NewStartingIndex);

                raised = true;
            };

            target.InsertRange(1, new[] { 3, 4 });

            Assert.True(raised);
        }

        [Fact]
        public void Removing_Item_Should_Raise_CollectionChanged()
        {
            var target = new AvaloniaList<int>(new[] { 1, 2, 3 });
            var raised = false;

            target.CollectionChanged += (s, e) =>
            {
                Assert.Equal(target, s);
                Assert.Equal(NotifyCollectionChangedAction.Remove, e.Action);
                Assert.Equal(new[] { 3 }, e.OldItems.Cast<int>());
                Assert.Equal(2, e.OldStartingIndex);

                raised = true;
            };

            target.Remove(3);

            Assert.True(raised);
        }

        [Fact]
        public void Moving_Item_Should_Raise_CollectionChanged()
        {
            var target = new AvaloniaList<int>(new[] { 1, 2, 3 });
            var raised = false;

            target.CollectionChanged += (s, e) =>
            {
                Assert.Equal(target, s);
                Assert.Equal(NotifyCollectionChangedAction.Move, e.Action);
                Assert.Equal(new[] { 3 }, e.OldItems.Cast<int>());
                Assert.Equal(new[] { 3 }, e.NewItems.Cast<int>());
                Assert.Equal(2, e.OldStartingIndex);
                Assert.Equal(0, e.NewStartingIndex);

                raised = true;
            };

            target.Move(2, 0);

            Assert.True(raised);
        }

        [Fact]
        public void Moving_Items_Should_Raise_CollectionChanged()
        {
            var target = new AvaloniaList<int>(new[] { 1, 2, 3 });
            var raised = false;

            target.CollectionChanged += (s, e) =>
            {
                Assert.Equal(target, s);
                Assert.Equal(NotifyCollectionChangedAction.Move, e.Action);
                Assert.Equal(new[] { 2, 3 }, e.OldItems.Cast<int>());
                Assert.Equal(new[] { 2, 3 }, e.NewItems.Cast<int>());
                Assert.Equal(1, e.OldStartingIndex);
                Assert.Equal(0, e.NewStartingIndex);

                raised = true;
            };

            target.MoveRange(1, 2, 0);

            Assert.True(raised);
        }

        [Fact]
        public void Clearing_Items_Should_Raise_CollectionChanged_Reset()
        {
            var target = new AvaloniaList<int>(new[] { 1, 2, 3 });
            var raised = false;

            target.CollectionChanged += (s, e) =>
            {
                Assert.Equal(target, s);
                Assert.Equal(NotifyCollectionChangedAction.Reset, e.Action);

                raised = true;
            };

            target.Clear();

            Assert.True(raised);
        }

        [Fact]
        public void Clearing_Items_Should_Raise_CollectionChanged_Remove()
        {
            var target = new AvaloniaList<int>(new[] { 1, 2, 3 });
            var raised = false;

            target.ResetBehavior = ResetBehavior.Remove;
            target.CollectionChanged += (s, e) =>
            {
                Assert.Equal(target, s);
                Assert.Equal(NotifyCollectionChangedAction.Remove, e.Action);
                Assert.Equal(new[] { 1, 2, 3 }, e.OldItems.Cast<int>());
                Assert.Equal(0, e.OldStartingIndex);

                raised = true;
            };

            target.Clear();

            Assert.True(raised);
        }

        [Fact]
        public void Can_CopyTo_Array_Of_Same_Type()
        {
            var target = new AvaloniaList<string> { "foo", "bar", "baz" };
            var result = new string[3];

            target.CopyTo(result, 0);

            Assert.Equal(target, result);
        }

        [Fact]
        public void Can_CopyTo_Array_Of_Base_Type()
        {
            var target = new AvaloniaList<string> { "foo", "bar", "baz" };
            var result = new object[3];

            ((IList)target).CopyTo(result, 0);

            Assert.Equal(target, result);
        }

        [Fact]
        public void RemoveAll_Should_Send_Single_Notification_For_Sequential_Range()
        {
            var target = new AvaloniaList<string>(Enumerable.Range(0, 10).Select(x => $"Item {x}"));
            var toRemove = new[] { "Item 5", "Item 6", "Item 7" };
            var raised = 0;

            target.CollectionChanged += (s, e) =>
            {
                Assert.Equal(NotifyCollectionChangedAction.Remove, e.Action);
                Assert.Equal(5, e.OldStartingIndex);
                Assert.Equal(toRemove, e.OldItems);
                ++raised;
            };

            target.RemoveAll(toRemove);

            Assert.Equal(1, raised);
        }

        [Fact]
        public void RemoveAll_Should_Send_Single_Notification_For_Sequential_Range_With_Duplicate_Source_Items()
        {
            var items = Enumerable.Range(0, 20).Select(x => $"Item {x / 2}");
            var target = new AvaloniaList<string>(items);
            var toRemove = new[] { "Item 5", "Item 6", "Item 7" };
            var raised = 0;

            target.CollectionChanged += (s, e) =>
            {
                Assert.Equal(NotifyCollectionChangedAction.Remove, e.Action);
                Assert.Equal(10, e.OldStartingIndex);

                Assert.Equal(new[] 
                { 
                    "Item 5",
                    "Item 5",
                    "Item 6",
                    "Item 6",
                    "Item 7",
                    "Item 7",
                }, e.OldItems);
                ++raised;
            };

            target.RemoveAll(toRemove);

            Assert.Equal(1, raised);
        }

        [Fact]
        public void RemoveAll_Should_Send_Multiple_Notifications_For_Non_Sequential_Range()
        {
            var target = new AvaloniaList<string>(Enumerable.Range(0, 10).Select(x => $"Item {x}"));
            var raised = 0;
            var toRemove = new[] 
            {
                new[] { "Item 2", "Item 3" },
                new[] { "Item 5", "Item 6" }
            };

            target.CollectionChanged += (s, e) =>
            {
                Assert.Equal(NotifyCollectionChangedAction.Remove, e.Action);
                
                if (raised == 0)
                {
                    Assert.Equal(5, e.OldStartingIndex);
                    Assert.Equal(toRemove[1], e.OldItems);
                }
                else
                {
                    Assert.Equal(2, e.OldStartingIndex);
                    Assert.Equal(toRemove[0], e.OldItems);
                }

                ++raised;
            };

            target.RemoveAll(toRemove[0].Concat(toRemove[1]));

            Assert.Equal(2, raised);
        }

        [Fact]
        public void RemoveAll_Should_Send_Multiple_Notifications_For_Sequential_Range_With_Nonsequential_Duplicate_Source_Items()
        {
            var items = Enumerable.Range(0, 10).Select(x => $"Item {x}");
            var target = new AvaloniaList<string>(items.Concat(items));
            var raised = 0;
            var toRemove = new[] { "Item 5", "Item 6", "Item 7" };

            target.CollectionChanged += (s, e) =>
            {
                Assert.Equal(NotifyCollectionChangedAction.Remove, e.Action);

                if (raised == 0)
                {
                    Assert.Equal(15, e.OldStartingIndex);
                    Assert.Equal(toRemove, e.OldItems);
                }
                else
                {
                    Assert.Equal(5, e.OldStartingIndex);
                    Assert.Equal(toRemove, e.OldItems);
                }

                ++raised;
            };

            target.RemoveAll(toRemove);

            Assert.Equal(2, raised);
        }

        [Fact]
        public void RemoveAll_Should_Not_Send_Notification_For_Items_Not_Present()
        {
            var target = new AvaloniaList<string>(Enumerable.Range(0, 10).Select(x => $"Item {x}"));
            var toRemove = new[] { "Item 5", "Item 6", "Item 7", "Not present" };
            var raised = 0;

            target.CollectionChanged += (s, e) =>
            {
                Assert.Equal(NotifyCollectionChangedAction.Remove, e.Action);
                Assert.Equal(5, e.OldStartingIndex);
                Assert.Equal(toRemove.Take(3).ToArray(), e.OldItems);
                ++raised;
            };

            target.RemoveAll(toRemove);

            Assert.Equal(1, raised);
        }

        [Fact]
        public void RemoveAll_Should_Handle_Empty_List()
        {
            var target = new AvaloniaList<string>();
            var toRemove = new[] { "Item 5", "Item 6", "Item 7" };
            var raised = 0;

            target.CollectionChanged += (s, e) =>
            {
                ++raised;
            };

            target.RemoveAll(toRemove);

            Assert.Equal(0, raised);
        }
    }
}

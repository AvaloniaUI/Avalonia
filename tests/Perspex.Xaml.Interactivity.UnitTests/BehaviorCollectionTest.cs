using Perspex.Controls;
using Perspex.Controls.Primitives;
using Perspex.Xaml.Interactivity;
using AppContainerUITestMethod = Xunit.FactAttribute;
using Xunit;

namespace Perspex.Markup.Xaml.UnitTests.Interactivity
{
    public class BehaviorCollectionTest
    {
        [AppContainerUITestMethod]
        public void VectorChanged_NonBehaviorAdded_ExceptionThrown()
        {
            BehaviorCollection behaviorCollection = new BehaviorCollection();
            behaviorCollection.Add(new StubBehavior());

            TestUtilities.AssertThrowsInvalidOperationException(() => behaviorCollection.Add(new TextBlock()));
        }

        [AppContainerUITestMethod]
        public void VectorChanged_BehaviorChangedToNonBehavior_ExceptionThrown()
        {
            BehaviorCollection behaviorCollection = new BehaviorCollection();
            behaviorCollection.Add(new StubBehavior());
            
            TestUtilities.AssertThrowsInvalidOperationException(() => behaviorCollection[0] = new ToggleButton()); // TODO: ToggleSwitch
        }

        [AppContainerUITestMethod]
        public void VectorChanged_DuplicateAdd_ExceptionThrown()
        {
            BehaviorCollection behaviorCollection = new BehaviorCollection();
            StubBehavior stub = new StubBehavior();
            behaviorCollection.Add(stub);

            TestUtilities.AssertThrowsInvalidOperationException(() => behaviorCollection.Add(stub));

        }

        [AppContainerUITestMethod]
        public void VectorChanged_AddWhileNotAttached_AttachNotCalled()
        {
            BehaviorCollection behaviorCollection = new BehaviorCollection();
            StubBehavior stub = new StubBehavior();
            behaviorCollection.Add(stub);

            TestUtilities.AssertNotAttached(stub);
        }

        [AppContainerUITestMethod]
        public void VectorChanged_AddWhileAttached_AllAttached()
        {
            BehaviorCollection behaviorCollection = new BehaviorCollection();
            behaviorCollection.Attach(new Button());

            behaviorCollection.Add(new StubBehavior());
            behaviorCollection.Add(new StubBehavior());
            behaviorCollection.Add(new StubBehavior());

            foreach (StubBehavior stub in behaviorCollection)
            {
                TestUtilities.AssertAttached(stub, behaviorCollection.AssociatedObject);
            }
        }

        [AppContainerUITestMethod]
        public void VectorChanged_ReplaceWhileAttached_OldDetachedNewAttached()
        {
            BehaviorCollection behaviorCollection = new BehaviorCollection();
            behaviorCollection.Attach(new Button());

            StubBehavior first = new StubBehavior();
            behaviorCollection.Add(first);

            StubBehavior second = new StubBehavior();

            behaviorCollection[0] = second;

            TestUtilities.AssertDetached(first);

            TestUtilities.AssertAttached(second, behaviorCollection.AssociatedObject);
        }

        [AppContainerUITestMethod]
        public void VectorChanged_RemoveWhileNotAttached_DetachNotCalled()
        {
            BehaviorCollection behaviorCollection = new BehaviorCollection();

            StubBehavior behavior = new StubBehavior();
            behaviorCollection.Add(behavior);
            behaviorCollection.Remove(behavior);

            TestUtilities.AssertNotDetached(behavior);
        }

        [AppContainerUITestMethod]
        public void VectorChanged_RemoveWhileAttached_Detached()
        {
            BehaviorCollection behaviorCollection = new BehaviorCollection();
            behaviorCollection.Attach(new ToggleButton()); // TODO: ToggleSwitch

            StubBehavior behavior = new StubBehavior();
            behaviorCollection.Add(behavior);
            behaviorCollection.Remove(behavior);

            TestUtilities.AssertDetached(behavior);
        }

        [AppContainerUITestMethod]
        public void VectorChanged_ResetWhileNotAttached_DetachNotCalled()
        {
            StubBehavior[] behaviorArray = { new StubBehavior(), new StubBehavior(), new StubBehavior() };

            BehaviorCollection behaviorCollection = new BehaviorCollection();
            foreach (StubBehavior behavior in behaviorArray)
            {
                behaviorCollection.Add(behavior);
            }

            behaviorCollection.Clear();

            foreach (StubBehavior behavior in behaviorArray)
            {
                TestUtilities.AssertNotDetached(behavior);
            }
        }

        [AppContainerUITestMethod]
        public void VectorChanged_ResetWhileAttached_AllDetached()
        {
            StubBehavior[] behaviorArray = { new StubBehavior(), new StubBehavior(), new StubBehavior() };

            BehaviorCollection behaviorCollection = new BehaviorCollection();
            behaviorCollection.Attach(new Button());

            foreach (StubBehavior behavior in behaviorArray)
            {
                behaviorCollection.Add(behavior);
            }

            behaviorCollection.Clear();

            foreach (StubBehavior behavior in behaviorArray)
            {
                TestUtilities.AssertDetached(behavior);
            }
        }

        [AppContainerUITestMethod]
        public void Attach_MultipleBehaviors_AllAttached()
        {
            BehaviorCollection behaviorCollection = new BehaviorCollection();
            behaviorCollection.Add(new StubBehavior());
            behaviorCollection.Add(new StubBehavior());
            behaviorCollection.Add(new StubBehavior());

            Button button = new Button();
            behaviorCollection.Attach(button);
             
            Assert.Equal(button, behaviorCollection.AssociatedObject); // "Attach should set the AssociatedObject to the given parameter."

            foreach (StubBehavior stub in behaviorCollection)
            {
                TestUtilities.AssertAttached(stub, button);
            }
        }

        [AppContainerUITestMethod]
        public void Attach_Null_AttachNotCalledOnItems()
        {
            BehaviorCollection behaviorCollection = new BehaviorCollection();
            behaviorCollection.Add(new StubBehavior());
            behaviorCollection.Add(new StubBehavior());
            behaviorCollection.Add(new StubBehavior());

            behaviorCollection.Attach(null);

            foreach (StubBehavior stub in behaviorCollection)
            {
                TestUtilities.AssertNotAttached(stub);
            }
        }

        [AppContainerUITestMethod]
        public void Attach_MultipleObjects_ExceptionThrown()
        {
            BehaviorCollection behaviorCollection = new BehaviorCollection();
            StubBehavior stub = new StubBehavior();
            behaviorCollection.Attach(new Button());

            TestUtilities.AssertThrowsInvalidOperationException(() => behaviorCollection.Attach(new StackPanel()));
        }

        [AppContainerUITestMethod]
        public void Attach_NonNullThenNull_ExceptionThrown()
        {
            BehaviorCollection behaviorCollection = new BehaviorCollection();
            behaviorCollection.Add(new StubBehavior());

            behaviorCollection.Attach(new Button());

            TestUtilities.AssertThrowsInvalidOperationException(() => behaviorCollection.Attach(null));
        }

        [AppContainerUITestMethod]
        public void Attach_MultipleTimeSameObject_AttachCalledOnce()
        {
            BehaviorCollection behaviorCollection = new BehaviorCollection() { new StubBehavior() };

            Button button = new Button();
            behaviorCollection.Attach(button);
            behaviorCollection.Attach(button);

            // This method hard codes AttachCount == 1.
            TestUtilities.AssertAttached((StubBehavior)behaviorCollection[0], button);
        }

        [AppContainerUITestMethod]
        public void Detach_NotAttached_DetachNotCalledOnItems()
        {
            BehaviorCollection behaviorCollection = new BehaviorCollection() { new StubBehavior() };

            behaviorCollection.Detach();

            TestUtilities.AssertNotDetached((StubBehavior)behaviorCollection[0]);
        }

        [AppContainerUITestMethod]
        public void Detach_Attached_AllItemsDetached()
        {
            BehaviorCollection behaviorCollection = new BehaviorCollection();
            behaviorCollection.Add(new StubBehavior());
            behaviorCollection.Add(new StubBehavior());
            behaviorCollection.Add(new StubBehavior());

            behaviorCollection.Attach(new Button());
            behaviorCollection.Detach();

            Assert.Null(behaviorCollection.AssociatedObject); // "The AssociatedObject should be null after Detach."

            foreach (StubBehavior behavior in behaviorCollection)
            {
                TestUtilities.AssertDetached(behavior);
            }
        }
    }
}

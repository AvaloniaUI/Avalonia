using System.Collections.Generic;
using System.Linq;
using Perspex.Controls;
using Perspex.Controls.Primitives;
using Perspex.Xaml.Interactivity;
using AppContainerUITestMethod = Xunit.FactAttribute;
using Xunit;

namespace Perspex.Markup.Xaml.UnitTests.Interactivity
{
    public class InteractionTest
    {
        // TODO:

        /*
        [AppContainerUITestMethod]
        public void SetBehaviors_MultipleBehaviors_AllAttached()
        {
            BehaviorCollection behaviorCollection = new BehaviorCollection();
            behaviorCollection.Add(new StubBehavior());
            behaviorCollection.Add(new StubBehavior());
            behaviorCollection.Add(new StubBehavior());

            Button button = new Button();
            Interaction.SetBehaviors(button, behaviorCollection);

            foreach (StubBehavior behavior in behaviorCollection)
            {
                Assert.Equal(1, behavior.AttachCount); // "Should only have called Attach once."
                Assert.Equal(0, behavior.DetachCount); // "Should not have called Detach."
                Assert.Equal(button, behavior.AssociatedObject); // "Should be attached to the host of the BehaviorCollection."
            }
        }

        [AppContainerUITestMethod]
        public void SetBehaviors_MultipleSets_DoesNotReattach()
        {
            BehaviorCollection behaviorCollection = new BehaviorCollection() { new StubBehavior() };

            Button button = new Button();
            Interaction.SetBehaviors(button, behaviorCollection);
            Interaction.SetBehaviors(button, behaviorCollection);

            foreach (StubBehavior behavior in behaviorCollection)
            {
                Assert.Equal(1, behavior.AttachCount); // "Should only have called Attach once."
            }
        }

        [AppContainerUITestMethod]
        public void SetBehaviors_CollectionThenNull_DeatchCollection()
        {
            BehaviorCollection behaviorCollection = new BehaviorCollection() { new StubBehavior() };

            Button button = new Button();
            Interaction.SetBehaviors(button, behaviorCollection);
            Interaction.SetBehaviors(button, null);

            foreach (StubBehavior behavior in behaviorCollection)
            {
                Assert.Equal(1, behavior.DetachCount); // "Should only have called Detach once."
                Assert.Null(behavior.AssociatedObject); // "AssociatedObject should be null after Detach."
            }
        }
        */

        [AppContainerUITestMethod]
        public void SetBehaviors_NullThenNull_NoOp()
        {
            // As long as this doesn't crash/assert, we're good.

            Button button = new Button();
            Interaction.SetBehaviors(button, null);
            Interaction.SetBehaviors(button, null);
            Interaction.SetBehaviors(button, null);
        }

        [AppContainerUITestMethod]
        public void SetBehaviors_ManualDetachThenNull_DoesNotDoubleDetach()
        {
            BehaviorCollection behaviorCollection = new BehaviorCollection();
            behaviorCollection.Add(new StubBehavior());
            behaviorCollection.Add(new StubBehavior());
            behaviorCollection.Add(new StubBehavior());

            Button button = new Button();
            Interaction.SetBehaviors(button, behaviorCollection);

            foreach (StubBehavior behavior in behaviorCollection)
            {
                behavior.Detach();
            }

            Interaction.SetBehaviors(button, null);

            foreach (StubBehavior behavior in behaviorCollection)
            {
                Assert.Equal(1, behavior.DetachCount); // "Setting BehaviorCollection to null should not call Detach on already Detached Behaviors."
                Assert.Null(behavior.AssociatedObject); // "AssociatedObject should be null after Detach."
            }
        }

        [AppContainerUITestMethod]
        public void ExecuteActions_NullParameters_ReturnsEmptyEnumerable()
        {
            // Mostly just want to test that this doesn't throw any exceptions.
            IEnumerable<object> result = Interaction.ExecuteActions(null, null, null);

            Assert.NotNull(result);
            Assert.Equal(0, result.Count()); // "Calling ExecuteActions with a null ActionCollection should return an empty enumerable."
        }

        [AppContainerUITestMethod]
        public void ExecuteActions_MultipleActions_AllActionsExecuted()
        {
            ActionCollection actions = new ActionCollection();
            actions.Add(new StubAction());
            actions.Add(new StubAction());
            actions.Add(new StubAction());

            Button sender = new Button();
            string parameterString = "TestString";

            Interaction.ExecuteActions(sender, actions, parameterString);

            foreach (StubAction action in actions)
            {
                Assert.Equal(1, action.ExecuteCount); // "Each IAction should be executed once."
                Assert.Equal(sender, action.Sender); // "Sender is passed to the actions."
                Assert.Equal(parameterString, action.Parameter); // "Parameter is passed to the actions."
            }
        }

        [AppContainerUITestMethod]
        public void ExecuteActions_ActionsWithResults_ResultsInActionOrder()
        {
            string[] expectedReturnValues = { "A", "B", "C" };

            ActionCollection actions = new ActionCollection();

            foreach (string returnValue in expectedReturnValues)
            {
                actions.Add(new StubAction(returnValue));
            }

            List<object> results = Interaction.ExecuteActions(null, actions, null).ToList();

            Assert.Equal(expectedReturnValues.Length, results.Count); // "Should have the same number of results as IActions."

            for (int resultIndex = 0; resultIndex < results.Count; resultIndex++)
            {
                Assert.Equal(expectedReturnValues[resultIndex], results[resultIndex]); // "Results should be returned in the order of the actions in the ActionCollection."
            }
        }
    }
}

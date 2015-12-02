using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Input;
using Perspex;
using Perspex.Controls;
using Perspex.Markup;
using Perspex.Xaml.Interactivity;

namespace Perspex.Xaml.Interactivity.UnitTests
{
    public class StubBehavior : PerspexObject, IBehavior
    {
        public int AttachCount
        {
            get;
            private set;
        }

        public int DetachCount
        {
            get;
            private set;
        }

        public ActionCollection Actions
        {
            get;
            private set;
        }

        public StubBehavior()
        {
            this.Actions = new ActionCollection();
        }

        public PerspexObject AssociatedObject
        {
            get;
            private set;
        }

        public void Attach(PerspexObject PerspexObject)
        {
            this.AssociatedObject = PerspexObject;
            this.AttachCount++;
        }

        public void Detach()
        {
            this.AssociatedObject = null;
            this.DetachCount++;
        }

        public IEnumerable<object> Execute(object sender, object parameter)
        {
            return Interaction.ExecuteActions(sender, this.Actions, parameter);
        }
    }

    public class StubAction : PerspexObject, IAction
    {
        private readonly object returnValue;

        public StubAction()
        {
            this.returnValue = null;
        }

        public StubAction(object returnValue)
        {
            this.returnValue = returnValue;
        }

        public object Sender
        {
            get;
            private set;
        }

        public object Parameter
        {
            get;
            private set;
        }

        public int ExecuteCount
        {
            get;
            private set;
        }

        public object Execute(object sender, object parameter)
        {
            this.ExecuteCount++;
            this.Sender = sender;
            this.Parameter = parameter;
            return this.returnValue;
        }
    }

    public class EventObjectStub : PerspexObject
    {
        public string Name;

        public delegate void IntEventHandler(int i);

        public event EventHandler StubEvent;
        public event EventHandler StubEvent2;
        public event IntEventHandler IntEvent;
        public event EventHandler Click;

        public EventObjectStub(string name = null)
        {
            this.Name = name;
        }

        public void FireStubEvent()
        {
            if (this.StubEvent != null)
            {
                this.StubEvent.Invoke(this, new EventArgs());
            }
        }

        public void FireClickEvent()
        {
            if (this.Click != null)
            {
                this.Click.Invoke(this, new EventArgs());
            }
        }

        public void FireStubEvent2()
        {
            if (this.StubEvent2 != null)
            {
                this.StubEvent2(this, new EventArgs());
            }
        }

        public void FireIntEvent()
        {
            if (this.IntEvent != null)
            {
                this.IntEvent(0);
            }
        }
    }

    public class ClrEventClassStub
    {
        public event EventHandler Event;
        public static readonly string EventName = "Event";

        public void Fire()
        {
            if (this.Event != null)
            {
                this.Event(this, EventArgs.Empty);
            }
        }
    }

    public class ChangePropertyActionTargetStub
    {
        public const string DoublePropertyName = "DoubleProperty";
        public const string StringPropertyName = "StringProperty";
        public const string ObjectPropertyName = "ObjectProperty";
        public const string AdditivePropertyName = "AdditiveProperty";
        public const string WriteOnlyPropertyName = "WriteOnlyProperty";

        public double DoubleProperty
        {
            get;
            set;
        }

        public string StringProperty
        {
            get;
            set;
        }

        public object ObjectProperty
        {
            get;
            set;
        }

        public object WriteOnlyProperty
        {
            set
            {
            }
        }
    }

    // TODO:
    /*
    public class NavigateToPageActionTargetStub : Frame
    {
        public bool HasNavigated
        {
            get;
            private set;
        }

        public object LastParameter
        {
            get;
            private set;
        }

        public new bool Navigate(Type sourcePageType)
        {
            this.HasNavigated = true;
            return true;
        }

        public new bool Navigate(Type sourcePageType, object parameter)
        {
            this.HasNavigated = true;
            this.LastParameter = parameter;

            return true;
        }
    }
    */

    public class MethodObjectStub : PerspexObject
    {
        public string LastMethodCalled
        {
            get;
            private set;
        }

        public MethodObjectStub()
        {
            this.LastMethodCalled = "None";
        }

        public void UniqueMethodWithNoParameters()
        {
            this.LastMethodCalled = "UniqueMethodWithNoParameters";
        }

        public void DuplicatedMethod()
        {
            this.LastMethodCalled = "DuplicatedMethodWithNoParameters";
        }

        public void DuplicatedMethod(object sender, EventArgs args)
        {
            this.LastMethodCalled = "DuplicatedMethodWithEventHandlerSignature";
        }

        public void DuplicatedMethod(object sender, StubEventArgs args)
        {
            this.LastMethodCalled = "DuplicatedMethodWithStubEventArgsSignature";
        }

        private void AnotherDuplicateMethod()
        {
            this.LastMethodCalled = "HiddenAnotherDuplicateMethod";
        }

        public void AnotherDuplicateMethod(object sender, object args)
        {
            this.LastMethodCalled = "AnotherDuplicateMethod";
        }

        public void AnotherDuplicateMethod(object sender, int args)
        {
            this.LastMethodCalled = "AnotherDuplicateMethodWithValueType";
        }

        public void IndistinguishableWithNullMethod(object sender, Nullable<bool> args)
        {
            this.LastMethodCalled = "NullableIndistinguishableWithNullMethod";
        }

        public void IndistinguishableWithNullMethod(object sender, Button args)
        {
            this.LastMethodCalled = "ButtonIndistinguishableWithNullMethod";
        }

        public void IndistinguishableWithNullMethod(object sender, bool args)
        {
            this.LastMethodCalled = "BoolIndistinguishableWithNullMethod";
        }

        public int IncompatibleReturnType()
        {
            this.LastMethodCalled = "IncompatibleReturnType";
            return 0;
        }

        public void IncompatibleParameters(double d)
        {
            this.LastMethodCalled = "IncompatibleParameters";
        }
    }

    public class StubEventArgs : EventArgs
    {
    }

    public class StubCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public int ExecutionCount
        {
            get;
            private set;
        }

        public object LastParameter
        {
            get;
            private set;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            this.ExecutionCount++;
            string stringParam = parameter as string;
            this.LastParameter = parameter;
        }
    }

    public class BoolToTestParameterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo language)
        {
            bool boolValue;
            bool boolParameter;
            if (value == null ||
                !bool.TryParse(value.ToString(), out boolValue) ||
                !bool.TryParse(parameter.ToString(), out boolParameter))
            {
                return null;
            }

            string convertedValue = boolValue && boolParameter ? "TrueParameter" : "FalseParameter";
            return convertedValue + language;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo language)
        {
            throw new NotImplementedException();
        }
    }

    // TODO:
    /*
    public class StubFrame : Frame
    {
        public bool NavigatedTo
        {
            get;
            private set;
        }

        public object Parameter
        {
            get;
            private set;
        }

        public StubFrame()
        {
            this.Navigated += this.OnNavigated;
        }

        private void OnNavigated(object sender, NavigationEventArgs e)
        {
            this.NavigatedTo = true;
            this.Parameter = e.Parameter;
        }
    }

    public class StubPage : Page
    {
    }
    
    public class NavigableStub : PerspexObject, INavigate
    {
        public string NavigatedTypeFullName
        {
            get;
            private set;
        }

        public bool Navigate(Type sourcePageType)
        {
            this.NavigatedTypeFullName = sourcePageType.FullName;
            return true;
        }
    }
    */

    public static class BehaviorTestHelper
    {
        public static T CreateNamedElement<T>(string name) where T : Control, new()
        {
            T frameworkElement = new T()
            {
                Name = name
            };
            return frameworkElement;
        }
    }
}

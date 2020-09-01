using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Utilities;
using ExecuteFactory = System.Func<object, System.Reflection.MethodInfo, System.Type, System.Action<object>>;

namespace Avalonia.Data.Converters
{
    class MethodToCommandConverter : ICommand
    {
        readonly static Func<object, bool> AlwaysEnabled = (_) => true;
        readonly static MethodInfo tryConvert = typeof(TypeUtilities)
            .GetMethod(nameof(TypeUtilities.TryConvert), BindingFlags.Public | BindingFlags.Static);
        readonly static PropertyInfo currentCulture = typeof(CultureInfo)
            .GetProperty(nameof(CultureInfo.CurrentCulture), BindingFlags.Public | BindingFlags.Static);
        readonly Func<object, bool> canExecute;
        readonly Action<object> execute;
        readonly WeakPropertyChangedProxy weakPropertyChanged;
        readonly PropertyChangedEventHandler propertyChangedEventHandler;
        readonly string[] dependencyProperties;

        readonly static IDictionary<(bool IsTask, bool IsValueTask, bool HasParametrInfo), ExecuteFactory> _executeFactories =
            new Dictionary<(bool IsTask, bool IsValueTask, bool HasParametrInfo), ExecuteFactory>()
            {
                [(false, false, false)] = (target, methodInfo, parameterInfo) => CreateExecute(target, methodInfo),
                [(false, false, true)] = (target, methodInfo, parameterInfo) => CreateExecute(target, methodInfo, parameterInfo),
                [(true, false, false)] = (target, methodInfo, parameterInfo) => CreateExecuteAsyncTask(target, methodInfo),
                [(true, false, true)] = (target, methodInfo, parameterInfo) => CreateExecuteAsyncTask(target, methodInfo, parameterInfo),
                [(false, true, false)] = (target, methodInfo, parameterInfo) => CreateExecuteAsyncValueTask(target, methodInfo),
                [(false, true, true)] = (target, methodInfo, parameterInfo) => CreateExecuteAsyncValueTask(target, methodInfo, parameterInfo),
            };

        public MethodToCommandConverter(Delegate action)
        {
            var target = action.Target;
            var canExecuteMethodName = "Can" + action.Method.Name;
            var parameters = action.Method.GetParameters();
            var parameterInfo = parameters.Length == 0 ? null : parameters[0].ParameterType;
            var isAsyncTask = action.Method.ReturnType == typeof(Task);
            var isAsyncValue = action.Method.ReturnType == typeof(ValueTask);

            if (_executeFactories.TryGetValue((isAsyncTask, isAsyncValue, parameterInfo != null), out var factory) == false)
            {
                throw new ArgumentException();
            }

            execute = factory(target, action.Method, parameterInfo);

            var canExecuteMethod = action.Method.DeclaringType.GetRuntimeMethods()
                .FirstOrDefault(m => m.Name == canExecuteMethodName
                    && m.GetParameters().Length == 1
                    && m.GetParameters()[0].ParameterType == typeof(object));
            if (canExecuteMethod == null)
            {
                canExecute = AlwaysEnabled;
            }
            else
            {
                canExecute = CreateCanExecute(target, canExecuteMethod);
                dependencyProperties = canExecuteMethod
                    .GetCustomAttributes(typeof(Metadata.DependsOnAttribute), true)
                    .OfType<Metadata.DependsOnAttribute>()
                    .Select(x => x.Name)
                    .ToArray();
                if (dependencyProperties.Any()
                    && target is INotifyPropertyChanged inpc)
                {
                    propertyChangedEventHandler = OnPropertyChanged;
                    weakPropertyChanged = new WeakPropertyChangedProxy(inpc, propertyChangedEventHandler);
                }
            }
        }

        void OnPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(args.PropertyName)
                               || dependencyProperties?.Contains(args.PropertyName) == true)
            {
                RaiseCanExecuteChanged();
            }
        }

#pragma warning disable 0067
        public event EventHandler CanExecuteChanged;
#pragma warning restore 0067

        public bool CanExecute(object parameter) => canExecute(parameter);

        public void Execute(object parameter) => execute?.Invoke(parameter);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RaiseCanExecuteChanged()
        {
            Threading.Dispatcher.UIThread.Post(() => CanExecuteChanged?.Invoke(this, EventArgs.Empty)
            , Threading.DispatcherPriority.Input);
        }

        static Action<object> CreateExecute(object target
            , System.Reflection.MethodInfo method)
        {


            var parameter = Expression.Parameter(typeof(object), "parameter");

            var instance = Expression.Convert
            (
                Expression.Constant(target),
                method.DeclaringType
            );


            var call = Expression.Call
            (
                instance,
                method
            );


            return Expression
                .Lambda<Action<object>>(call, parameter)
                .Compile();
        }

        static Action<object> CreateExecute(object target
            , System.Reflection.MethodInfo method
            , Type parameterType)
        {

            var parameter = Expression.Parameter(typeof(object), "parameter");

            var instance = Expression.Convert
            (
                Expression.Constant(target),
                method.DeclaringType
            );

            Expression body;

            if (parameterType == typeof(object))
            {
                body = Expression.Call(instance,
                    method,
                    parameter
                    );
            }
            else
            {
                var arg0 = Expression.Variable(typeof(object), "argX");
                var convertCall = Expression.Call(tryConvert,
                     Expression.Constant(parameterType),
                     parameter,
                      Expression.Property(null, currentCulture),
                     arg0
                    );

                var call = Expression.Call(instance,
                    method,
                    Expression.Convert(arg0, parameterType)
                    );
                body = Expression.Block(new[] { arg0 },
                    convertCall,
                    call
                    );

            }
            Action<object> action;
            try
            {
                action = Expression
                   .Lambda<Action<object>>(body, parameter)
                   .Compile();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return action;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Action<object> CreateExecuteAsync<T>(object target
            , System.Reflection.MethodInfo method
            , Action<T> executor)
        {
            var parameter = Expression.Parameter(typeof(object), "parameter");

            var instance = Expression.Convert
            (
                Expression.Constant(target),
                method.DeclaringType
            );

            var task = Expression.Call(
                instance,
                method);

            return MakeExecuteAsync<T>(parameter, task, executor);
        }

        static Action<object> CreateExecuteAsync<T>(object target
            , System.Reflection.MethodInfo method
            , Type parameterType
            , Action<T> executor)
        {
            var parameter = Expression.Parameter(typeof(object), "parameter");

            var instance = Expression.Convert
            (
                Expression.Constant(target),
                method.DeclaringType
            );

            Expression task;

            if (parameterType == typeof(object))
            {
                task = Expression.Call(instance,
                    method,
                    parameter
                    );
            }
            else
            {
                var arg0 = Expression.Variable(typeof(object), "argX");
                var convertCall = Expression.Call(tryConvert,
                     Expression.Constant(parameterType),
                     parameter,
                      Expression.Property(null, currentCulture),
                     arg0
                    );

                var call = Expression.Call(instance,
                    method,
                    Expression.Convert(arg0, parameterType)
                    );
                task = Expression.Block(new[] { arg0 },
                    convertCall,
                    call
                    );

            }

            return MakeExecuteAsync(parameter, task, executor);

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Action<object> MakeExecuteAsync<T>(ParameterExpression parameter, Expression task, Action<T> executor)
        {
            Func<object, T> executeAction;
            try
            {
                executeAction = Expression
                   .Lambda<Func<object, T>>(task, parameter)
                   .Compile();
            }
            catch (Exception ex)
            {

                throw ex;
            }

            return p =>
            {
                executor(executeAction(p));
            };
        }

        static Action<object> CreateExecuteAsyncTask(object target
            , System.Reflection.MethodInfo method)
        {
            return CreateExecuteAsync<Task>(target, method, ExecuteAsyncTask);
        }
        static Action<object> CreateExecuteAsyncTask(object target
            , System.Reflection.MethodInfo method
            , Type parameterInfo)
        {
            return CreateExecuteAsync<Task>(target, method, parameterInfo, ExecuteAsyncTask);
        }
        static Action<object> CreateExecuteAsyncValueTask(object target
            , System.Reflection.MethodInfo method)
        {
            return CreateExecuteAsync<ValueTask>(target, method, ExecuteAsyncValueTask);
        }

        static Action<object> CreateExecuteAsyncValueTask(object target
            , System.Reflection.MethodInfo method
             , Type parameterInfo)
        {
            return CreateExecuteAsync<ValueTask>(target, method, parameterInfo, ExecuteAsyncValueTask);
        }



        static Func<object, bool> CreateCanExecute(object target
            , System.Reflection.MethodInfo method)
        {
            var parameter = Expression.Parameter(typeof(object), "parameter");
            var instance = Expression.Convert
            (
                Expression.Constant(target),
                method.DeclaringType
            );
            var call = Expression.Call
            (
                instance,
                method,
                parameter
            );
            return Expression
                .Lambda<Func<object, bool>>(call, parameter)
                .Compile();
        }

        internal class WeakPropertyChangedProxy
        {
            readonly WeakReference<PropertyChangedEventHandler> _listener = new WeakReference<PropertyChangedEventHandler>(null);
            readonly PropertyChangedEventHandler _handler;
            internal WeakReference<INotifyPropertyChanged> Source { get; } = new WeakReference<INotifyPropertyChanged>(null);

            public WeakPropertyChangedProxy()
            {
                _handler = new PropertyChangedEventHandler(OnPropertyChanged);
            }

            public WeakPropertyChangedProxy(INotifyPropertyChanged source, PropertyChangedEventHandler listener) : this()
            {
                SubscribeTo(source, listener);
            }

            public void SubscribeTo(INotifyPropertyChanged source, PropertyChangedEventHandler listener)
            {
                source.PropertyChanged += _handler;

                Source.SetTarget(source);
                _listener.SetTarget(listener);
            }

            public void Unsubscribe()
            {
                if (Source.TryGetTarget(out INotifyPropertyChanged source) && source != null)
                    source.PropertyChanged -= _handler;

                Source.SetTarget(null);
                _listener.SetTarget(null);
            }

            void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                if (_listener.TryGetTarget(out var handler) && handler != null)
                    handler(sender, e);
                else
                    Unsubscribe();
            }

        }

        static void ExecuteAsyncTask(Task task)
        {
            var stateMachine = new AsyncTaskCommandStateMachine();
            stateMachine.task = task;
            stateMachine.builder = AsyncVoidMethodBuilder.Create();
            stateMachine.state = -1;
            stateMachine.builder.Start(ref stateMachine);
        }

        static void ExecuteAsyncValueTask(ValueTask task)
        {
            var stateMachine = new AsyncValueTaskCommandStateMachine();
            stateMachine.task = task;
            stateMachine.builder = AsyncValueTaskMethodBuilder.Create();
            stateMachine.state = -1;
            stateMachine.builder.Start(ref stateMachine);
        }

        private sealed class AsyncTaskCommandStateMachine : IAsyncStateMachine
        {
            public int state;

            public AsyncVoidMethodBuilder builder;

            public Task task;

            private TaskAwaiter _currentAwaiter;

            public void MoveNext()
            {
                int num = state;
                try
                {
                    TaskAwaiter awaiter;
                    if (num != 0)
                    {
                        awaiter = task.GetAwaiter();
                        if (!awaiter.IsCompleted)
                        {
                            num = (state = 0);
                            _currentAwaiter = awaiter;
                            AsyncTaskCommandStateMachine stateMachine = this;
                            builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
                            return;
                        }
                    }
                    else
                    {
                        awaiter = _currentAwaiter;
                        _currentAwaiter = default;
                        num = (state = -1);
                    }
                    awaiter.GetResult();
                }
                catch (Exception exception)
                {
                    state = -2;
                    builder.SetException(exception);
                    return;
                }
                state = -2;
                builder.SetResult();
            }

            public void SetStateMachine(IAsyncStateMachine stateMachine)
            {

            }
        }

        private sealed class AsyncValueTaskCommandStateMachine : IAsyncStateMachine
        {
            public int state;

            public AsyncValueTaskMethodBuilder builder;

            public ValueTask task;

            private ValueTaskAwaiter _currentAwaiter;

            public void MoveNext()
            {
                int num = state;
                try
                {
                    ValueTaskAwaiter awaiter;
                    if (num != 0)
                    {
                        awaiter = task.GetAwaiter();
                        if (!awaiter.IsCompleted)
                        {
                            num = (state = 0);
                            _currentAwaiter = awaiter;
                            AsyncValueTaskCommandStateMachine stateMachine = this;
                            builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
                            return;
                        }
                    }
                    else
                    {
                        awaiter = _currentAwaiter;
                        _currentAwaiter = default;
                        num = (state = -1);
                    }
                    awaiter.GetResult();
                }
                catch (Exception exception)
                {
                    state = -2;
                    builder.SetException(exception);
                    return;
                }
                state = -2;
                builder.SetResult();
            }

            public void SetStateMachine(IAsyncStateMachine stateMachine)
            {

            }
        }

    }
}

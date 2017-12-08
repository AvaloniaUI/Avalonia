using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.LogicalTree;
using ReactiveUI;
using Splat;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace ControlCatalog
{
    public class ViewLocator
    {
        public static IControl Build(object data)
        {
            var name = data.GetType().FullName.Replace("ViewModel", "View");

            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => name.Contains(a.GetName().Name));

            Type type = null;

            foreach (var assembly in assemblies)
            {
                type = assembly.GetType(name);

                if (type != null)
                {
                    break;
                }
            }

            if (type != null)
            {
                if (typeof(Control).IsAssignableFrom(type))
                {
                    var constructor = type.GetConstructor(Type.EmptyTypes);

                    if (constructor != null)
                    {
                        return (Control)Activator.CreateInstance(type);
                    }
                }
            }

            return new TextBlock { Text = $"View Locator Error: Unable to find type {name}" };
        }
    }

    /// <summary>
    ///     This content control will automatically load the View associated with
    ///     the ViewModel property and display it. This control is very useful
    ///     inside a DataTemplate to display the View associated with a ViewModel.
    /// </summary>
    public class ViewModelViewHost : TemplatedControl, IViewFor, IEnableLogger, IActivationForViewFetcher
    {
        public static readonly AvaloniaProperty ViewModelProperty =
            AvaloniaProperty.Register<ViewModelViewHost, object>(nameof(ViewModel), null, false, BindingMode.OneWay, null,
                somethingChanged);

        public static readonly AvaloniaProperty DefaultContentProperty =
            AvaloniaProperty.Register<ViewModelViewHost, object>(nameof(DefaultContent), null, false, BindingMode.OneWay, null,
                somethingChanged);

        public static readonly AvaloniaProperty ViewContractObservableProperty =
            AvaloniaProperty.Register<ViewModelViewHost, IObservable<string>>(nameof(ViewContractObservable),
                Observable.Return(default(string)));

        private readonly Subject<Unit> updateViewModel = new Subject<Unit>();

        private string viewContract = string.Empty;

        /// <summary>
        ///     If no ViewModel is displayed, this content (i.e. a control) will be displayed.
        /// </summary>
        public object DefaultContent
        {
            get { return GetValue(DefaultContentProperty); }
            set { SetValue(DefaultContentProperty, value); }
        }

        public IObservable<string> ViewContractObservable
        {
            get { return (IObservable<string>)GetValue(ViewContractObservableProperty); }
            set { SetValue(ViewContractObservableProperty, value); }
        }

        public string ViewContract
        {
            get { return viewContract; }
            set { ViewContractObservable = Observable.Return(value); }
        }

        public int GetAffinityForView(Type view)
        {
            throw new NotImplementedException();
        }

        public IObservable<bool> GetActivationForView(IActivatable view)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     The ViewModel to display
        /// </summary>
        public object ViewModel
        {
            get { return GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        protected override void OnDataContextEndUpdate()
        {
            base.OnDataContextEndUpdate();

            if (Content as ILogical != null)
            {
                LogicalChildren.Remove(Content as ILogical);
            }

            if (DataContext != null)
            {
                Content = ViewLocator.Build(DataContext);
            }

            if (Content as ILogical != null)
            {
                LogicalChildren.Add(Content as ILogical);
            }
        }

        private static void somethingChanged(IAvaloniaObject dependencyObject, bool changed)
        {
            if (changed)
            {
                ((ViewModelViewHost)dependencyObject).updateViewModel.OnNext(Unit.Default);
            }
        }

        public static readonly StyledProperty<object> ContentProperty = ContentControl.ContentProperty.AddOwner<ViewModelViewHost>();

        public object Content
        {
            get { return GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }
    }
}
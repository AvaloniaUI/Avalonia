using System;
using System.Collections.Generic;
using System.Reactive.Disposables;

#pragma warning disable CS1591 // Enable me later

namespace Perspex
{
    public class PerspexLocator : IPerspexDependencyResolver
    {
        private readonly IPerspexDependencyResolver _parentScope;
        public static IPerspexDependencyResolver Current { get; set; }
        public static PerspexLocator CurrentMutable { get; set; }
        private readonly Dictionary<Type, Func<object>> _registry = new Dictionary<Type, Func<object>>();

        static PerspexLocator()
        {
            Current = CurrentMutable = new PerspexLocator();
        }

        public PerspexLocator()
        {
            
        }

        public PerspexLocator(IPerspexDependencyResolver parentScope)
        {
            _parentScope = parentScope;
        }

        public object GetService(Type t)
        {
            Func<object> rv;
            return _registry.TryGetValue(t, out rv) ? rv() : _parentScope?.GetService(t);
        }

        public class RegistrationHelper<TService>
        {
            private readonly PerspexLocator _locator;

            public RegistrationHelper(PerspexLocator locator)
            {
                _locator = locator;
            }

            public PerspexLocator ToConstant<TImpl>(TImpl constant) where TImpl : TService
            {
                _locator._registry[typeof (TService)] = () => constant;
                return _locator;
            }

            public PerspexLocator ToFunc<TImlp>(Func<TImlp> func) where TImlp : TService
            {
                _locator._registry[typeof (TService)] = () => func();
                return _locator;
            }

            public PerspexLocator ToSingleton<TImpl>() where TImpl : class, TService, new()
            {
                TImpl instance = null;
                return ToFunc(() => instance ?? (instance = new TImpl()));
            }

            public PerspexLocator ToTransient<TImpl>() where TImpl : class, TService, new() => ToFunc(() => new TImpl());
        }

        public RegistrationHelper<T> Bind<T>() => new RegistrationHelper<T>(this);


        public PerspexLocator BindToSelf<T>(T constant)
            => Bind<T>().ToConstant(constant);

        public PerspexLocator BindToSelfSingleton<T>() where T : class, new() => Bind<T>().ToSingleton<T>();

        class ResolverDisposable : IDisposable
        {
            private readonly IPerspexDependencyResolver _resolver;
            private readonly PerspexLocator _mutable;

            public ResolverDisposable(IPerspexDependencyResolver resolver, PerspexLocator mutable)
            {
                _resolver = resolver;
                _mutable = mutable;
            }

            public void Dispose()
            {
                Current = _resolver;
                CurrentMutable = _mutable;
            }
        }


        public static IDisposable EnterScope()
        {
            var d = new ResolverDisposable(Current, CurrentMutable);
            Current = CurrentMutable =  new PerspexLocator(Current);
            return d;
        }
    }

    public interface IPerspexDependencyResolver
    {
        object GetService(Type t);
    }

    public static class LocatorExtensions
    {
        public static T GetService<T>(this IPerspexDependencyResolver resolver)
        {
            return (T) resolver.GetService(typeof (T));
        }
    }
}


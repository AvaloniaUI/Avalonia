using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Platform;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedParameter.Global

namespace Avalonia.Markup.Xaml.XamlIl.Runtime
{
    public static class XamlIlRuntimeHelpers
    {
        [ThreadStatic] private static List<IResourceNode>? s_resourceNodeBuffer;

        public static Func<IServiceProvider, object> DeferredTransformationFactoryV1(Func<IServiceProvider, object> builder,
            IServiceProvider provider)
        {
            return DeferredTransformationFactoryV2<Control>(builder, provider);
        }

        public static Func<IServiceProvider, object> DeferredTransformationFactoryV2<T>(Func<IServiceProvider, object> builder,
            IServiceProvider provider)
        {
            var resourceNodes = AsResourceNodes(provider.GetRequiredService<IAvaloniaXamlIlParentStackProvider>());
            var rootObject = provider.GetRequiredService<IRootObjectProvider>().RootObject;
            var parentScope = provider.GetService<INameScope>();
            return sp =>
            {
                var scope = parentScope != null ? new ChildNameScope(parentScope) : (INameScope)new NameScope();
                var obj = builder(new DeferredParentServiceProvider(sp, resourceNodes, rootObject, scope));
                scope.Complete();

                return new TemplateResult<T>((T)obj, scope);
            };
        }

        private static IResourceNode[] AsResourceNodes(IAvaloniaXamlIlParentStackProvider provider)
        {
            var buffer = s_resourceNodeBuffer ??= new List<IResourceNode>(8);

            if (provider is IAvaloniaXamlIlEagerParentStackProvider eagerProvider)
            {
                var enumerator = new EagerParentStackEnumerator(eagerProvider);

                while (enumerator.TryGetNextOfType<IResourceNode>() is { } node)
                    buffer.Add(node);
            }
            else
            {
                foreach (var item in provider.Parents)
                {
                    if (item is IResourceNode node)
                        buffer.Add(node);
                }
            }

            var result = buffer.ToArray();
            buffer.Clear();
            return result;
        }

        private sealed class DeferredParentServiceProvider :
            IAvaloniaXamlIlEagerParentStackProvider,
            IServiceProvider,
            IRootObjectProvider,
            IAvaloniaXamlIlControlTemplateProvider
        {
            private readonly IServiceProvider? _parentProvider;
            private readonly IResourceNode[] _parentResourceNodes;
            private readonly INameScope _nameScope;
            private IRuntimePlatform? _runtimePlatform;
            private Optional<IAvaloniaXamlIlEagerParentStackProvider?> _parentStackProvider;

            public DeferredParentServiceProvider(
                IServiceProvider? parentProvider,
                IResourceNode[] parentResourceNodes,
                object rootObject,
                INameScope nameScope)
            {
                _parentProvider = parentProvider;
                _parentResourceNodes = parentResourceNodes;
                _nameScope = nameScope;
                RootObject = rootObject;
            }

            public object RootObject { get; }

            public object IntermediateRootObject
                => RootObject;

            public IEnumerable<object> Parents
                => _parentResourceNodes;

            public IReadOnlyList<object> DirectParents
                => _parentResourceNodes;

            public IAvaloniaXamlIlEagerParentStackProvider? ParentProvider
            {
                get
                {
                    if (!_parentStackProvider.HasValue)
                    {
                        _parentStackProvider =
                            new Optional<IAvaloniaXamlIlEagerParentStackProvider?>(GetParentStackProviderFromServices());
                    }

                    return _parentStackProvider.GetValueOrDefault();
                }
            }

            private IAvaloniaXamlIlEagerParentStackProvider? GetParentStackProviderFromServices()
                => _parentProvider?.GetService<IAvaloniaXamlIlParentStackProvider>()
                    as IAvaloniaXamlIlEagerParentStackProvider;

            public object? GetService(Type serviceType)
            {
                if (serviceType == typeof(INameScope))
                    return _nameScope;
                if (serviceType == typeof(IAvaloniaXamlIlParentStackProvider))
                    return this;
                if (serviceType == typeof(IRootObjectProvider))
                    return this;
                if (serviceType == typeof(IAvaloniaXamlIlControlTemplateProvider))
                    return this;
                if (serviceType == typeof(IRuntimePlatform))
                    return _runtimePlatform ??= AvaloniaLocator.Current.GetService<IRuntimePlatform>();
                return _parentProvider?.GetService(serviceType);
            }
        }

        public static void ApplyNonMatchingMarkupExtensionV1(object target, object property, IServiceProvider prov,
            object value)
        {
            if (value is IBinding b)
            {
                if (property is AvaloniaProperty p)
                    ((AvaloniaObject)target).Bind(p, b);
                else
                    throw new ArgumentException("Attempt to apply binding to non-avalonia property " + property);
            }
            else if (value is UnsetValueType unset)
            {
                if (property is AvaloniaProperty p)
                    ((AvaloniaObject)target).SetValue(p, unset);
                //TODO: Investigate
                //throw new ArgumentException("Attempt to apply UnsetValue to non-avalonia property " + property);
            }
            else
                throw new ArgumentException("Don't know what to do with " + value.GetType());
        }

        public static IServiceProvider CreateInnerServiceProviderV1(IServiceProvider compiled)
            => new InnerServiceProvider(compiled);

        private sealed class InnerServiceProvider : IServiceProvider
        {
            private readonly IServiceProvider _compiledProvider;
            private XamlTypeResolver? _resolver;

            public InnerServiceProvider(IServiceProvider compiledProvider)
            {
                _compiledProvider = compiledProvider;
            }
            public object? GetService(Type serviceType)
            {
                if (serviceType == typeof(IXamlTypeResolver))
                    return _resolver ??= new XamlTypeResolver(
                        _compiledProvider.GetRequiredService<IAvaloniaXamlIlXmlNamespaceInfoProvider>());
                return null;
            }
        }

        private sealed class XamlTypeResolver : IXamlTypeResolver
        {
            private readonly IAvaloniaXamlIlXmlNamespaceInfoProvider _nsInfo;

            public XamlTypeResolver(IAvaloniaXamlIlXmlNamespaceInfoProvider nsInfo)
            {
                _nsInfo = nsInfo;
            }

            [RequiresUnreferencedCode(TrimmingMessages.XamlTypeResolvedRequiresUnreferenceCodeMessage)]
            public Type Resolve(string qualifiedTypeName)
            {
                var sp = qualifiedTypeName.Split(new[] {':'}, 2);
                var (ns, name) = sp.Length == 1 ? ("", qualifiedTypeName) : (sp[0], sp[1]);
                var namespaces = _nsInfo.XmlNamespaces;
                if (!namespaces.TryGetValue(ns, out var lst))
                    throw new ArgumentException("Unable to resolve namespace for type " + qualifiedTypeName);
                var resolvable = lst.Where(static e => e.ClrAssemblyName is { Length: > 0 });
                foreach (var entry in resolvable)
                {
                    var asm = Assembly.Load(new AssemblyName(entry.ClrAssemblyName));
                    var resolved = asm.GetType(entry.ClrNamespace + "." + name);
                    if (resolved != null)
                        return resolved;
                }

                throw new ArgumentException(
                    $"Unable to resolve type {qualifiedTypeName} from any of the following locations: " +
                    string.Join(",", resolvable.Select(e => $"`clr-namespace:{e.ClrNamespace};assembly={e.ClrAssemblyName}`")))
                    { HelpLink = "https://docs.avaloniaui.net/guides/basics/introduction-to-xaml#valid-xaml-namespaces" };
            }
        }

        // Don't emit debug symbols for this code so debugger will be forced to step into XAML instead
        #line hidden
        public static IServiceProvider CreateRootServiceProviderV2()
        {
            return new RootServiceProvider(new NameScope(), null);
        }
        public static IServiceProvider CreateRootServiceProviderV3(IServiceProvider parentServiceProvider)
        {
            return new RootServiceProvider(new NameScope(), parentServiceProvider);
        }
        #line default

        private sealed class RootServiceProvider : IServiceProvider
        {
            private readonly INameScope _nameScope;
            private readonly IServiceProvider? _parentServiceProvider;
            private readonly IRuntimePlatform? _runtimePlatform;
            private IAvaloniaXamlIlParentStackProvider? _parentStackProvider;

            public RootServiceProvider(INameScope nameScope, IServiceProvider? parentServiceProvider)
            {
                _nameScope = nameScope;
                _parentServiceProvider = parentServiceProvider;
                _runtimePlatform = AvaloniaLocator.Current.GetService<IRuntimePlatform>();
            }

            public object? GetService(Type serviceType)
            {
                if (serviceType == typeof(INameScope))
                    return _nameScope;
                if (serviceType == typeof(IAvaloniaXamlIlParentStackProvider))
                    return _parentStackProvider ??= CreateParentStackProvider();
                if (serviceType == typeof(IRuntimePlatform))
                    return _runtimePlatform ?? throw new KeyNotFoundException($"{nameof(IRuntimePlatform)} was not registered");

                return null;
            }

            private IAvaloniaXamlIlParentStackProvider CreateParentStackProvider()
                => _parentServiceProvider?.GetService<IAvaloniaXamlIlParentStackProvider>()
                   ?? GetParentStackProviderForApplication(Application.Current);

            private static IAvaloniaXamlIlEagerParentStackProvider GetParentStackProviderForApplication(Application? application)
                => application is null ?
                    EmptyAvaloniaXamlIlParentStackProvider.Instance :
                    ApplicationAvaloniaXamlIlParentStackProvider.GetForApplication(application);

            private sealed class ApplicationAvaloniaXamlIlParentStackProvider :
                IAvaloniaXamlIlEagerParentStackProvider,
                IReadOnlyList<object>
            {
                private static ApplicationAvaloniaXamlIlParentStackProvider? s_lastProvider;
                private static Application? s_lastApplication;

                public static ApplicationAvaloniaXamlIlParentStackProvider GetForApplication(Application application)
                {
                    if (application != s_lastApplication)
                    {
                        s_lastProvider = new ApplicationAvaloniaXamlIlParentStackProvider(application);
                        s_lastApplication = application;
                    }

                    return s_lastProvider!;
                }

                private readonly Application _application;
                private IEnumerable<object>? _parents;

                public ApplicationAvaloniaXamlIlParentStackProvider(Application application)
                    => _application = application;

                public IEnumerable<object> Parents
                    => _parents ??= new object[] { _application };

                public IReadOnlyList<object> DirectParents
                    => this;

                public int Count
                    => 1;

                public IEnumerator<object> GetEnumerator()
                    => Parents.GetEnumerator();

                IEnumerator IEnumerable.GetEnumerator()
                    => GetEnumerator();

                public object this[int index]
                {
                    get
                    {
                        if (index != 0)
                            ThrowArgumentOutOfRangeException();

                        return _application;
                    }
                }

                [DoesNotReturn]
                [MethodImpl(MethodImplOptions.NoInlining)]
                [SuppressMessage("ReSharper", "NotResolvedInText")]
                private static void ThrowArgumentOutOfRangeException()
                    => throw new ArgumentOutOfRangeException("index");

                public IAvaloniaXamlIlEagerParentStackProvider? ParentProvider
                    => null;
            }

            private sealed class EmptyAvaloniaXamlIlParentStackProvider : IAvaloniaXamlIlEagerParentStackProvider
            {
                public static EmptyAvaloniaXamlIlParentStackProvider Instance { get; } = new();

                private EmptyAvaloniaXamlIlParentStackProvider()
                {
                }

                public IEnumerable<object> Parents
                    => Array.Empty<object>();

                public IReadOnlyList<object> DirectParents
                    => Array.Empty<object>();

                public IAvaloniaXamlIlEagerParentStackProvider? ParentProvider
                    => null;
            }
        }
    }
}

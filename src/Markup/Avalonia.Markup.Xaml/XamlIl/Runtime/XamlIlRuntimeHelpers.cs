using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
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
            var resourceNodes = AsResourceNodes(provider.GetRequiredService<IAvaloniaXamlIlParentStackProvider>().Parents);
            var rootObject = provider.GetRequiredService<IRootObjectProvider>().RootObject;
            var parentScope = provider.GetService<INameScope>();
            return sp =>
            {
                var scope = parentScope != null ? new ChildNameScope(parentScope) : (INameScope)new NameScope();
                var obj = builder(new DeferredParentServiceProvider(sp, resourceNodes, rootObject, scope));
                scope.Complete();

                if(typeof(T) == typeof(Control))
                    return new TemplateResult<Control>((Control)obj, scope);

                return new TemplateResult<T>((T)obj, scope);
            };
        }

        private static IResourceNode[] AsResourceNodes(IEnumerable<object> items)
        {
            var buffer = s_resourceNodeBuffer ??= new List<IResourceNode>(8);

            foreach (var item in items)
            {
                if (item is IResourceNode node)
                    buffer.Add(node);
            }

            var result = buffer.ToArray();
            buffer.Clear();
            return result;
        }

        private sealed class DeferredParentServiceProvider :
            IAvaloniaXamlIlParentStackProvider,
            IServiceProvider,
            IRootObjectProvider,
            IAvaloniaXamlIlControlTemplateProvider
        {
            private readonly IServiceProvider? _parentProvider;
            private readonly IResourceNode[] _parentResourceNodes;
            private readonly INameScope _nameScope;
            private IRuntimePlatform? _runtimePlatform;

            public DeferredParentServiceProvider(IServiceProvider? parentProvider, IResourceNode[] parentResourceNodes,
                object rootObject, INameScope nameScope)
            {
                _parentProvider = parentProvider;
                _parentResourceNodes = parentResourceNodes;
                _nameScope = nameScope;
                RootObject = rootObject;
            }

            public IEnumerable<object> Parents => _parentResourceNodes;

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

            public object RootObject { get; }
            public object IntermediateRootObject => RootObject;
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
                   ?? DefaultAvaloniaXamlIlParentStackProvider.GetForApplication(Application.Current);

            private sealed class DefaultAvaloniaXamlIlParentStackProvider : IAvaloniaXamlIlParentStackProvider
            {
                private static readonly DefaultAvaloniaXamlIlParentStackProvider s_empty = new(Enumerable.Empty<object>());
                private static DefaultAvaloniaXamlIlParentStackProvider s_lastProvider = s_empty;
                private static Application? s_lastApplication;

                public static DefaultAvaloniaXamlIlParentStackProvider GetForApplication(Application? application)
                {
                    if (application != s_lastApplication)
                        SetLastProvider(application);

                    return s_lastProvider;

                    static void SetLastProvider(Application? application)
                    {
                        if (application is null)
                        {
                            s_lastProvider = s_empty;
                        }
                        else
                        {
                            s_lastProvider = new DefaultAvaloniaXamlIlParentStackProvider(new object[] { application });
                            s_lastApplication = application;
                        }
                    }
                }

                public DefaultAvaloniaXamlIlParentStackProvider(IEnumerable<object> parents)
                    => Parents = parents;

                public IEnumerable<object> Parents { get; }
            }
        }
    }
}

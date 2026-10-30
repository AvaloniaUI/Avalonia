using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Styling;
using Moq;
using Xunit;
using Avalonia.UnitTests;

namespace Avalonia.Base.UnitTests.Styling
{
    public class ResourceDictionaryTests : ScopedTestBase
    {
        [Fact]
        public void Cannot_Add_Null_Key()
        {
            var target = new ResourceDictionary();
            Assert.Throws<ArgumentNullException>(() => target.Add(null!, "null"));
        }

        [Fact]
        public void Can_Add_Null_Value()
        {
            var target = new ResourceDictionary();
            target.Add("null", null);
        }

        [Fact]
        public void TryGetResource_Should_Find_Resource()
        {
            var target = new ResourceDictionary
            {
                { "foo", "bar" },
            };

            Assert.True(target.TryGetResource("foo", null, out var result));
            Assert.Equal("bar", result);
        }

        [Fact]
        public void TryGetResource_Should_Find_Resource_From_Merged_Dictionary()
        {
            var target = new ResourceDictionary
            {
                MergedDictionaries =
                {
                    new ResourceDictionary
                    {
                        { "foo", "bar" },
                    }
                }
            };

            Assert.True(target.TryGetResource("foo", null, out var result));
            Assert.Equal("bar", result);
        }

        [Fact]
        public void TryGetResource_Should_Find_Resource_From_Itself_Before_Merged_Dictionary()
        {
            var target = new ResourceDictionary
            {
                { "foo", "bar" },
            };

            target.MergedDictionaries.Add(new ResourceDictionary
            {
                { "foo", "baz" },
            });

            Assert.True(target.TryGetResource("foo", null, out var result));
            Assert.Equal("bar", result);
        }

        [Fact]
        public void TryGetResource_Should_Find_Resource_From_Later_Merged_Dictionary()
        {
            var target = new ResourceDictionary
            {
                MergedDictionaries =
                {
                    new ResourceDictionary
                    {
                        { "foo", "bar" },
                    },
                    new ResourceDictionary
                    {
                        { "foo", "baz" },
                    }
                }
            };

            Assert.True(target.TryGetResource("foo", null, out var result));
            Assert.Equal("baz", result);
        }

        [Fact]
        public void NotifyHostedResourcesChanged_Should_Be_Called_On_AddOwner()
        {
            var host = new Mock<IResourceHost>();
            var target = new ResourceDictionary { { "foo", "bar" } };

            ((IResourceProvider)target).AddOwner(host.Object);

            host.Verify(x => x.NotifyHostedResourcesChanged(It.IsAny<ResourcesChangedEventArgs>()));
        }

        [Fact]
        public void NotifyHostedResourcesChanged_Should_Be_Called_On_RemoveOwner()
        {
            var host = new Mock<IResourceHost>();
            var target = new ResourceDictionary { { "foo", "bar" } };

            ((IResourceProvider)target).AddOwner(host.Object);
            host.Invocations.Clear();
            ((IResourceProvider)target).RemoveOwner(host.Object);

            host.Verify(x => x.NotifyHostedResourcesChanged(It.IsAny<ResourcesChangedEventArgs>()));
        }

        [Fact]
        public void NotifyHostedResourcesChanged_Should_Be_Called_On_Resource_Add()
        {
            var host = new Mock<IResourceHost>();
            var target = new ResourceDictionary(host.Object);

            host.Invocations.Clear();
            target.Add("foo", "bar");

            host.Verify(x => x.NotifyHostedResourcesChanged(It.IsAny<ResourcesChangedEventArgs>()));
        }

        [Fact]
        public void NotifyHostedResourcesChanged_Should_Be_Called_On_MergedDictionary_Add()
        {
            var host = new Mock<IResourceHost>();
            var target = new ResourceDictionary(host.Object);

            host.Invocations.Clear();
            target.MergedDictionaries.Add(new ResourceDictionary
            {
                { "foo", "bar" },
            });

            host.Verify(
                x => x.NotifyHostedResourcesChanged(It.IsAny<ResourcesChangedEventArgs>()),
                Times.Once);
        }

        [Fact]
        public void NotifyHostedResourcesChanged_Should_Not_Be_Called_On_Empty_MergedDictionary_Add()
        {
            var host = new Mock<IResourceHost>();
            var target = new ResourceDictionary(host.Object);

            host.Invocations.Clear();
            target.MergedDictionaries.Add(new ResourceDictionary());

            host.Verify(
                x => x.NotifyHostedResourcesChanged(It.IsAny<ResourcesChangedEventArgs>()),
                Times.Never);
        }

        [Fact]
        public void NotifyHostedResourcesChanged_Should_Be_Called_On_MergedDictionary_Remove()
        {
            var host = new Mock<IResourceHost>();
            var target = new ResourceDictionary(host.Object)
            {
                MergedDictionaries =
                {
                    new ResourceDictionary { { "foo", "bar" } },
                }
            };

            host.Invocations.Clear();
            target.MergedDictionaries.RemoveAt(0);

            host.Verify(
                x => x.NotifyHostedResourcesChanged(It.IsAny<ResourcesChangedEventArgs>()),
                Times.Once);
        }

        [Fact]
        public void NotifyHostedResourcesChanged_Should_Be_Called_On_MergedDictionary_Resource_Add()
        {
            var host = new Mock<IResourceHost>();
            var target = new ResourceDictionary(host.Object)
            {
                MergedDictionaries =
                {
                    new ResourceDictionary(),
                }
            };

            host.Invocations.Clear();
            ((IResourceDictionary)target.MergedDictionaries[0]).Add("foo", "bar");

            host.Verify(
                x => x.NotifyHostedResourcesChanged(It.IsAny<ResourcesChangedEventArgs>()),
                Times.Once);
        }

        [Fact]
        public void Sets_Added_MergedDictionary_Owner()
        {
            var host = new Mock<IResourceHost>();

            var target = new ResourceDictionary(host.Object);
            target.MergedDictionaries.Add(new ResourceDictionary());

            Assert.Same(host.Object, target.Owner);
            Assert.Same(host.Object, ((ResourceDictionary)target.MergedDictionaries[0]).Owner);
        }

        [Fact]
        public void AddOwner_Sets_MergedDictionary_Owner()
        {
            var host = new Mock<IResourceHost>();

            var target = new ResourceDictionary
            {
                MergedDictionaries =
                {
                    new ResourceDictionary(),
                }
            };

            ((IResourceProvider)target).AddOwner(host.Object);

            Assert.Same(host.Object, target.Owner);
            Assert.Same(host.Object, ((ResourceDictionary)target.MergedDictionaries[0]).Owner);
        }

        [Fact]
        public void RemoveOwner_Clears_MergedDictionary_Owner()
        {
            var host = new Mock<IResourceHost>();

            var target = new ResourceDictionary(host.Object)
            {
                MergedDictionaries =
                {
                    new ResourceDictionary(),
                }
            };

            ((IResourceProvider)target).RemoveOwner(host.Object);

            Assert.Null(target.Owner);
            Assert.Null(((ResourceDictionary)target.MergedDictionaries[0]).Owner);
        }

        [Fact]
        public void MakeReadOnly_Prevents_Changes()
        {
            var target = new ResourceDictionary { { "foo", "bar" } };
            target.MergedDictionaries.Add(new ResourceDictionary());
            target.ThemeDictionaries.Add(ThemeVariant.Dark, new ResourceDictionary());

            Assert.False(target.IsReadOnly);
            target.MakeReadOnly();
            Assert.True(target.IsReadOnly);
            Assert.True(((ICollection<KeyValuePair<object, object?>>)target).IsReadOnly);

            Assert.Throws<NotSupportedException>(() => target["foo"] = "baz");
            Assert.Throws<NotSupportedException>(() => target.Add("baz", "qux"));
            Assert.Throws<NotSupportedException>(() => target.AddDeferred("baz", _ => "qux"));
            Assert.Throws<NotSupportedException>(() => target.SetItems([new("baz", "qux")]));
            Assert.Throws<NotSupportedException>(() => target.Remove("foo"));
            Assert.Throws<NotSupportedException>(() => ((ICollection<KeyValuePair<object, object?>>)target).Remove(new("foo", "bar")));
            Assert.Throws<NotSupportedException>(() => target.Clear());
            Assert.Throws<NotSupportedException>(() => target.MergedDictionaries.Add(new ResourceDictionary()));
            Assert.Throws<NotSupportedException>(() => target.MergedDictionaries.RemoveAt(0));
            Assert.Throws<NotSupportedException>(() => target.ThemeDictionaries.Add(ThemeVariant.Light, new ResourceDictionary()));
            Assert.Throws<NotSupportedException>(() => target.ThemeDictionaries.Remove(ThemeVariant.Dark));

            Assert.Equal("bar", target["foo"]);
            Assert.Single(target.MergedDictionaries);
            Assert.Single(target.ThemeDictionaries);
        }

        [Fact]
        public void MakeReadOnly_Without_Merged_And_Theme_Dictionaries_Returns_Empty_Collections()
        {
            var target = new ResourceDictionary();
            target.MakeReadOnly();

            Assert.Empty(target.MergedDictionaries);
            Assert.Empty(target.ThemeDictionaries);
            Assert.Throws<NotSupportedException>(() => target.MergedDictionaries.Add(new ResourceDictionary()));
            Assert.Throws<NotSupportedException>(() => target.ThemeDictionaries.Add(ThemeVariant.Light, new ResourceDictionary()));
        }

        [Fact]
        public void ReadOnly_Dictionary_Resolves_Resources()
        {
            var calls = 0;
            var target = new ResourceDictionary
            {
                MergedDictionaries = { new ResourceDictionary { { "merged", "merged" } } },
                ThemeDictionaries = { [ThemeVariant.Dark] = new ResourceDictionary { { "themed", "dark" } } }
            };
            target.AddDeferred("deferred", _ => ++calls);
            target.MakeReadOnly();

            Assert.Equal(1, target["deferred"]);
            Assert.Equal(1, target["deferred"]);
            Assert.Equal(1, calls);
            Assert.True(target.TryGetResource("merged", null, out var merged));
            Assert.Equal("merged", merged);
            Assert.True(target.TryGetResource("themed", ThemeVariant.Dark, out var themed));
            Assert.Equal("dark", themed);
        }

        [Fact]
        public void MakeReadOnly_Affects_Theme_Dictionaries()
        {
            var dark = new ResourceDictionary();
            var target = new ResourceDictionary { ThemeDictionaries = { [ThemeVariant.Dark] = dark } };
            target.MakeReadOnly();

            Assert.True(dark.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => dark.Add("foo", "bar"));
        }

        [Fact]
        public void MakeReadOnly_Affects_Merged_Dictionaries()
        {
            var nested = new ResourceDictionary();
            var merged = new ResourceDictionary { MergedDictionaries = { nested } };
            var included = new ResourceDictionary();
            var target = new ResourceDictionary
            {
                MergedDictionaries = { merged, new TestResourceInclude(included) }
            };
            target.MakeReadOnly();

            Assert.True(merged.IsReadOnly);
            Assert.True(nested.IsReadOnly);
            Assert.True(included.IsReadOnly);
        }

        [Fact]
        public void MakeReadOnly_Skips_Other_Resource_Providers()
        {
            var provider = new Mock<IResourceProvider>();
            var target = new ResourceDictionary { MergedDictionaries = { provider.Object } };

            target.MakeReadOnly();

            Assert.True(target.IsReadOnly);
        }

        private class TestResourceInclude(IResourceDictionary loaded) : IResourceInclude
        {
            public IResourceDictionary Loaded { get; } = loaded;
            public IResourceHost? Owner => Loaded.Owner;
            public bool HasResources => Loaded.HasResources;
            public event EventHandler? OwnerChanged { add { } remove { } }
            public void AddOwner(IResourceHost owner) => Loaded.AddOwner(owner);
            public void RemoveOwner(IResourceHost owner) => Loaded.RemoveOwner(owner);
            public bool TryGetResource(object key, ThemeVariant? theme, out object? value) =>
                Loaded.TryGetResource(key, theme, out value);
        }
    }
}

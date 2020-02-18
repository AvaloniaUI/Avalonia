// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Controls;
using Moq;
using Xunit;

namespace Avalonia.Styling.UnitTests
{
    public class ResourceDictionaryTests
    {
        [Fact]
        public void TryGetResource_Should_Find_Resource()
        {
            var target = new ResourceDictionary
            {
                { "foo", "bar" },
            };

            Assert.True(target.TryGetResource("foo", out var result));
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

            Assert.True(target.TryGetResource("foo", out var result));
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

            Assert.True(target.TryGetResource("foo", out var result));
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

            Assert.True(target.TryGetResource("foo", out var result));
            Assert.Equal("baz", result);
        }

        [Fact]
        public void ResourcesChanged_Should_Be_Raised_On_Resource_Add()
        {
            var target = new ResourceDictionary();
            var raised = false;

            target.ResourcesChanged += (_, __) => raised = true;
            target.Add("foo", "bar");

            Assert.True(raised);
        }

        [Fact]
        public void ResourcesChanged_Should_Be_Raised_On_MergedDictionary_Add()
        {
            var target = new ResourceDictionary();
            var raised = false;

            target.ResourcesChanged += (_, __) => raised = true;
            target.MergedDictionaries.Add(new ResourceDictionary
            {
                { "foo", "bar" },
            });

            Assert.True(raised);
        }

        [Fact]
        public void ResourcesChanged_Should_Not_Be_Raised_On_Empty_MergedDictionary_Add()
        {
            var target = new ResourceDictionary();
            var raised = false;

            target.ResourcesChanged += (_, __) => raised = true;
            target.MergedDictionaries.Add(new ResourceDictionary());

            Assert.False(raised);
        }

        [Fact]
        public void ResourcesChanged_Should_Be_Raised_On_MergedDictionary_Remove()
        {
            var target = new ResourceDictionary
            {
                MergedDictionaries =
                {
                    new ResourceDictionary { { "foo", "bar" } },
                }
            };
            var raised = false;

            target.ResourcesChanged += (_, __) => raised = true;
            target.MergedDictionaries.RemoveAt(0);

            Assert.True(raised);
        }

        [Fact]
        public void ResourcesChanged_Should_Be_Raised_On_MergedDictionary_Resource_Add()
        {
            var target = new ResourceDictionary
            {
                MergedDictionaries =
                {
                    new ResourceDictionary(),
                }
            };

            var raised = false;

            target.ResourcesChanged += (_, __) => raised = true;
            ((IResourceDictionary)target.MergedDictionaries[0]).Add("foo", "bar");

            Assert.True(raised);
        }

        [Fact]
        public void MergedDictionary_ParentResourcesChanged_Should_Be_Called_On_Resource_Add()
        {
            var target = new ResourceDictionary();
            var merged = new Mock<ISetResourceParent>();

            target.MergedDictionaries.Add(merged.Object);
            merged.ResetCalls();

            target.Add("foo", "bar");

            merged.Verify(
                x => x.ParentResourcesChanged(It.IsAny<ResourcesChangedEventArgs>()),
                Times.Once);
        }

        [Fact]
        public void MergedDictionary_ParentResourcesChanged_Should_Be_Called_On_NotifyResourceChanged()
        {
            var target = new ResourceDictionary();
            var merged = new Mock<ISetResourceParent>();

            target.MergedDictionaries.Add(merged.Object);
            merged.ResetCalls();

            ((ISetResourceParent)target).ParentResourcesChanged(new ResourcesChangedEventArgs());

            merged.Verify(
                x => x.ParentResourcesChanged(It.IsAny<ResourcesChangedEventArgs>()),
                Times.Once);
        }
    }
}

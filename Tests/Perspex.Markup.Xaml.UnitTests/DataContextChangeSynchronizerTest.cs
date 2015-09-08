// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Controls;
using GitHubClient.ViewModels;
using Perspex.Markup.Xaml.DataBinding;
using Perspex.Markup.Xaml.DataBinding.ChangeTracking;
using OmniXaml.Builder;
using OmniXaml.TypeConversion;
using OmniXaml.TypeConversion.BuiltInConverters;
using Perspex.Xaml.Base.UnitTest.SampleModel;
using Xunit;

namespace Perspex.Xaml.Base.UnitTest
{
    public class DataContextChangeSynchronizerTest
    {
        private TypeConverterProvider _repo;
        private SamplePerspexObject _guiObject;
        private ViewModelMock _viewModel;

        public DataContextChangeSynchronizerTest()
        {
            _repo = new TypeConverterProvider();
            _guiObject = new SamplePerspexObject();
            _viewModel = new ViewModelMock();
        }

        [Fact]
        public void SameTypesFromUIToModel()
        {
            var synchronizer = new DataContextChangeSynchronizer(new DataContextChangeSynchronizer.BindingSource(new PropertyPath("IntProp"), _viewModel), new DataContextChangeSynchronizer.BindingTarget(_guiObject, SamplePerspexObject.IntProperty), _repo);
            synchronizer.StartUpdatingSourceWhenTargetChanges();

            const int someValue = 4;
            _guiObject.Int = someValue;

            Assert.Equal(someValue, _viewModel.IntProp);
        }

        [Fact]
        public void DifferentTypesFromUIToModel()
        {
            var synchronizer = new DataContextChangeSynchronizer(new DataContextChangeSynchronizer.BindingSource(new PropertyPath("IntProp"), _viewModel), new DataContextChangeSynchronizer.BindingTarget(_guiObject, SamplePerspexObject.StringProperty), _repo);
            synchronizer.StartUpdatingSourceWhenTargetChanges();

            _guiObject.String = "2";

            Assert.Equal(2, _viewModel.IntProp);
        }

        [Fact]
        public void DifferentTypesAndNonConvertibleValueFromUIToModel()
        {
            var synchronizer = new DataContextChangeSynchronizer(new DataContextChangeSynchronizer.BindingSource(new PropertyPath("IntProp"), _viewModel), new DataContextChangeSynchronizer.BindingTarget(_guiObject, SamplePerspexObject.StringProperty), _repo);
            synchronizer.StartUpdatingSourceWhenTargetChanges();

            _guiObject.String = "";

            Assert.Equal(default(int), _viewModel.IntProp);
        }


        [Fact]
        public void DifferentTypesFromModelToUI()
        {
            var synchronizer = new DataContextChangeSynchronizer(new DataContextChangeSynchronizer.BindingSource(new PropertyPath("IntProp"), _viewModel), new DataContextChangeSynchronizer.BindingTarget(_guiObject, SamplePerspexObject.StringProperty), _repo);
            synchronizer.StartUpdatingTargetWhenSourceChanges();

            _viewModel.IntProp = 2;

            Assert.Equal("2", _guiObject.String);
        }

        [Fact]
        public void SameTypesFromModelToUI()
        {
            var synchronizer = new DataContextChangeSynchronizer(new DataContextChangeSynchronizer.BindingSource(new PropertyPath("IntProp"), _viewModel), new DataContextChangeSynchronizer.BindingTarget(_guiObject, SamplePerspexObject.IntProperty), _repo);
            synchronizer.StartUpdatingTargetWhenSourceChanges();

            _viewModel.IntProp = 2;

            Assert.Equal(2, _guiObject.Int);
        }

        [Fact]
        public void GrokysTest()
        {
            var mainWindowViewModel = new MainWindowViewModel();
            var contentControl = new ContentControl();

            var synchronizer = new DataContextChangeSynchronizer(new DataContextChangeSynchronizer.BindingSource(new PropertyPath("Content"), mainWindowViewModel), new DataContextChangeSynchronizer.BindingTarget(contentControl, ContentControl.ContentProperty), _repo);

            synchronizer.StartUpdatingTargetWhenSourceChanges();

            var logInViewModel = new LogInViewModel();
            mainWindowViewModel.Content = logInViewModel;

            Assert.Equal(logInViewModel, contentControl.Content);
        }
    }
}

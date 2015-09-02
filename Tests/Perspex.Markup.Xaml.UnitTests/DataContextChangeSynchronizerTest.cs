namespace Perspex.Xaml.Base.UnitTest
{
    using System;
    using Controls;
    using GitHubClient.ViewModels;
    using Markup.Xaml.DataBinding;
    using Markup.Xaml.DataBinding.ChangeTracking;
    using OmniXaml.Builder;
    using OmniXaml.TypeConversion;
    using OmniXaml.TypeConversion.BuiltInConverters;
    using SampleModel;
    using Xunit;

    public class DataContextChangeSynchronizerTest
    {
        private TypeConverterProvider repo;
        private SamplePerspexObject guiObject;
        private ViewModelMock viewModel;

        public DataContextChangeSynchronizerTest()
        {
            repo = new TypeConverterProvider();
            guiObject = new SamplePerspexObject();
            viewModel = new ViewModelMock();
        }

        [Fact]
        public void SameTypesFromUIToModel()
        {
            var synchronizer = new DataContextChangeSynchronizer(guiObject, SamplePerspexObject.IntProperty, new PropertyPath("IntProp"), viewModel, repo);
            synchronizer.SubscribeModelToUI();

            const int someValue = 4;
            guiObject.Int = someValue;

            Assert.Equal(someValue, viewModel.IntProp);
        }

        [Fact]
        public void DifferentTypesFromUIToModel()
        {
            var synchronizer = new DataContextChangeSynchronizer(guiObject, SamplePerspexObject.StringProperty, new PropertyPath("IntProp"), viewModel, repo);
            synchronizer.SubscribeModelToUI();

            guiObject.String = "2";

            Assert.Equal(2, viewModel.IntProp);
        }

        [Fact]
        public void DifferentTypesAndNonConvertibleValueFromUIToModel()
        {
            var synchronizer = new DataContextChangeSynchronizer(guiObject, SamplePerspexObject.StringProperty, new PropertyPath("IntProp"), viewModel, repo);
            synchronizer.SubscribeModelToUI();

            guiObject.String = "";

            Assert.Equal(default(int), viewModel.IntProp);
        }


        [Fact]
        public void DifferentTypesFromModelToUI()
        {
            var synchronizer = new DataContextChangeSynchronizer(guiObject, SamplePerspexObject.StringProperty, new PropertyPath("IntProp"), viewModel, repo);
            synchronizer.SubscribeUIToModel();

            viewModel.IntProp = 2;
            
            Assert.Equal("2", guiObject.String);
        }

        [Fact]
        public void SameTypesFromModelToUI()
        {
            var synchronizer = new DataContextChangeSynchronizer(guiObject, SamplePerspexObject.IntProperty, new PropertyPath("IntProp"), viewModel, repo);
            synchronizer.SubscribeUIToModel();

            viewModel.IntProp = 2;

            Assert.Equal(2, guiObject.Int);
        }

        [Fact]
        public void GrokysTest()
        {
            var mainWindowViewModel = new MainWindowViewModel();
            var contentControl = new ContentControl();

            var synchronizer = new DataContextChangeSynchronizer(
                contentControl,
                ContentControl.ContentProperty,
                new PropertyPath("Content"),
                mainWindowViewModel,
                repo);

            synchronizer.SubscribeUIToModel();

            var logInViewModel = new LogInViewModel();
            mainWindowViewModel.Content = logInViewModel;

            Assert.Equal(logInViewModel, contentControl.Content);
        }
    }
}

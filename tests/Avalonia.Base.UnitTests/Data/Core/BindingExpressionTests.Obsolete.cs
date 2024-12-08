using System.Reactive.Linq;
using Avalonia.Data;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Xunit;

#nullable enable
#pragma warning disable CS0618 // Type or member is obsolete

namespace Avalonia.Base.UnitTests.Data.Core;

public abstract partial class BindingExpressionTests
{
    public partial class Reflection
    {
        [Fact]
        public void Obsolete_Initiate_Method_Produces_Observable_With_Correct_Target_Type()
        {
            // Issue #15081
            var viewModel = new ViewModel { DoubleValue = 42.5 };
            var target = new TargetClass { DataContext = viewModel };
            var binding = new Binding(nameof(viewModel.DoubleValue));
            var instanced = binding.Initiate(target, TargetClass.StringProperty);

            Assert.NotNull(instanced);

            var value = instanced.Observable.First();

            Assert.Equal("42.5", value);
        }
    }

    public partial class Compiled
    {
        [Fact]
        public void Obsolete_Initiate_Method_Produces_Observable_With_Correct_Target_Type()
        {
            // Issue #15081
            var viewModel = new ViewModel { DoubleValue = 42.5 };
            var target = new TargetClass { DataContext = viewModel };
            var path = CompiledBindingPathFromExpressionBuilder.Build<ViewModel, double>(x => x.DoubleValue, true);
            var binding = new CompiledBindingExtension(path);
            var instanced = binding.Initiate(target, TargetClass.StringProperty);

            Assert.NotNull(instanced);

            var value = instanced.Observable.First();

            Assert.Equal("42.5", value);
        }
    }
}

using System;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Data.Core;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests;

public class TemplateBindingLifetimeTests : ScopedTestBase
{
    [Fact]
    public void Observing_Binding_Does_Not_Retain_Collected_Targets_Parent()
    {
        var (subscription, owner) = ObserveTemporaryTarget();
        using (subscription)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.False(owner.TryGetTarget(out _));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Disposing_During_PropertyChange_Does_Not_Process_InFlight_Notifications(bool changeParent)
    {
        var owner = new ContentControl { Tag = "Initial" };
        var target = new TextBlock { TemplatedParent = owner };
        UntypedBindingExpressionBase? expression = null;
        owner.PropertyChanged += (_, e) =>
        {
            if (!changeParent && e.Property == Control.TagProperty)
                expression?.Dispose();
        };
        target.PropertyChanged += (_, e) =>
        {
            if (changeParent && e.Property == StyledElement.TemplatedParentProperty)
                expression?.Dispose();
        };
        var conversions = 0;
        var binding = new TemplateBinding(Control.TagProperty)
        {
            Converter = new FuncValueConverter<object, object>(value =>
            {
                ++conversions;
                return value!;
            }),
        };
        expression = (UntypedBindingExpressionBase)binding.CreateInstance(target, TextBlock.TextProperty, null);
        using var subscription = expression.ToObservable(target).Subscribe(_ => { });
        Assert.Equal(1, conversions);

        if (changeParent)
        {
            var newOwner = new ContentControl { Tag = "New owner" };
            target.TemplatedParent = newOwner;
            newOwner.Tag = "Changed";
        }
        else
        {
            owner.Tag = "Changed";
        }

        Assert.Equal(1, conversions);
    }

    [Fact]
    public void TwoWay_Direct_Property_Binding_Does_Not_Write_Source_Updates_Back()
    {
        var owner = new ContentControl { Tag = "Initial" };
        var target = new DirectTextTarget { TemplatedParent = owner };
        var writes = 0;
        using var binding = target.Bind(DirectTextTarget.TextProperty,
            new TemplateBinding(Control.TagProperty)
            {
                Mode = BindingMode.TwoWay,
                Converter = new FuncValueConverter<object, string>(
                    value => "Display " + value,
                    value => { ++writes; return "Edited"; }),
            });

        Assert.Equal("Initial", owner.Tag);
        Assert.Equal("Display Initial", target.Text);
        Assert.Equal(0, writes);

        owner.Tag = "Changed";
        Assert.Equal("Display Changed", target.Text);
        Assert.Equal(0, writes);

        target.Text = "User input";
        Assert.Equal("Edited", owner.Tag);
        Assert.Equal(1, writes);
    }

    private class DirectTextTarget : Control
    {
        public static readonly DirectProperty<DirectTextTarget, string?> TextProperty =
            AvaloniaProperty.RegisterDirect<DirectTextTarget, string?>(nameof(Text),
                target => target.Text, (target, value) => target.Text = value);
        private string? _text;

        public string? Text
        {
            get => _text;
            set => SetAndRaise(TextProperty, ref _text, value);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (IDisposable, WeakReference<ContentControl>) ObserveTemporaryTarget()
    {
        var owner = new ContentControl { Tag = "Initial" };
        var target = new TextBlock { TemplatedParent = owner };
        var binding = new TemplateBinding(Control.TagProperty);
        var expression = (UntypedBindingExpressionBase)binding.CreateInstance(target, TextBlock.TextProperty, null);
        var subscription = expression.ToObservable(target).Subscribe(_ => { });
        return (subscription, new(owner));
    }
    [Fact]
    public void Disposing_During_First_Publication_Does_Not_Leave_Parent_Change_Subscriptions()
    {
        var owner = new ContentControl { Tag = "Initial" };
        var target = new TextBlock { TemplatedParent = owner };
        var conversions = 0;
        var binding = new TemplateBinding(Control.TagProperty)
        {
            Converter = new FuncValueConverter<object, object>(value =>
            {
                ++conversions;
                return value!;
            }),
        };
        var expression = (UntypedBindingExpressionBase)binding.CreateInstance(target, TextBlock.TextProperty, null);
        using var subscription = expression.ToObservable(target).Subscribe(_ => expression.Dispose());
        Assert.Equal(1, conversions);

        var newOwner = new ContentControl { Tag = "New owner" };
        target.TemplatedParent = newOwner;
        newOwner.Tag = "Changed";

        Assert.Equal(1, conversions);
    }
}

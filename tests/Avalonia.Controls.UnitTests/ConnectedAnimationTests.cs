using System;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests;

public class ConnectedAnimationConfigurationTests : ScopedTestBase
{
    [Fact]
    public void GravityConfig_IsShadowEnabled_DefaultIsTrue()
    {
        var config = new GravityConnectedAnimationConfiguration();
        Assert.True(config.IsShadowEnabled);
    }

    [Fact]
    public void GravityConfig_IsShadowEnabled_CanBeSetFalse()
    {
        var config = new GravityConnectedAnimationConfiguration { IsShadowEnabled = false };
        Assert.False(config.IsShadowEnabled);
    }

    [Fact]
    public void GravityConfig_IsShadowEnabled_RoundTrips()
    {
        var config = new GravityConnectedAnimationConfiguration { IsShadowEnabled = true };
        Assert.True(config.IsShadowEnabled);
        config.IsShadowEnabled = false;
        Assert.False(config.IsShadowEnabled);
    }

    [Fact]
    public void GravityConfig_IsConnectedAnimationConfiguration()
    {
        var config = new GravityConnectedAnimationConfiguration();
        Assert.IsAssignableFrom<ConnectedAnimationConfiguration>(config);
    }

    [Fact]
    public void DirectConfig_Duration_DefaultIsNull()
    {
        var config = new DirectConnectedAnimationConfiguration();
        Assert.Null(config.Duration);
    }

    [Fact]
    public void DirectConfig_Duration_RoundTrips()
    {
        var d = TimeSpan.FromMilliseconds(200);
        var config = new DirectConnectedAnimationConfiguration { Duration = d };
        Assert.Equal(d, config.Duration);
    }

    [Fact]
    public void DirectConfig_Duration_CanBeSetToNull()
    {
        var config = new DirectConnectedAnimationConfiguration { Duration = TimeSpan.FromMilliseconds(100) };
        config.Duration = null;
        Assert.Null(config.Duration);
    }

    [Fact]
    public void DirectConfig_IsConnectedAnimationConfiguration()
    {
        var config = new DirectConnectedAnimationConfiguration();
        Assert.IsAssignableFrom<ConnectedAnimationConfiguration>(config);
    }

    [Fact]
    public void BasicConfig_IsConnectedAnimationConfiguration()
    {
        var config = new BasicConnectedAnimationConfiguration();
        Assert.IsAssignableFrom<ConnectedAnimationConfiguration>(config);
    }

    [Fact]
    public void BasicConfig_IsInstantiable()
    {
        var config = new BasicConnectedAnimationConfiguration();
        Assert.NotNull(config);
    }
}

public class ConnectedAnimationServiceTests : ScopedTestBase
{
    private static ConnectedAnimationService CreateService() => new ConnectedAnimationService();

    [Fact]
    public void GetForCurrentView_NullTopLevel_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ConnectedAnimationService.GetForCurrentView(null!));
    }

    [Fact]
    public void DefaultDuration_InitiallyIs300ms()
    {
        var service = CreateService();
        Assert.Equal(TimeSpan.FromMilliseconds(300), service.DefaultDuration);
    }

    [Fact]
    public void DefaultEasingFunction_InitiallyNull()
    {
        var service = CreateService();
        Assert.Null(service.DefaultEasingFunction);
    }

    [Fact]
    public void DefaultDuration_CanBeSet()
    {
        var service = CreateService();
        var d = TimeSpan.FromMilliseconds(500);
        service.DefaultDuration = d;
        Assert.Equal(d, service.DefaultDuration);
    }

    [Fact]
    public void DefaultEasingFunction_CanBeSet()
    {
        var service = CreateService();
        var easing = new LinearEasing();
        service.DefaultEasingFunction = easing;
        Assert.Same(easing, service.DefaultEasingFunction);
    }

    [Fact]
    public void GetAnimation_UnknownKey_ReturnsNull()
    {
        var service = CreateService();
        Assert.Null(service.GetAnimation("nonexistent"));
    }

    [Fact]
    public void PrepareToAnimate_NullKey_ThrowsArgumentException()
    {
        var service = CreateService();
        var source = new Border();
        Assert.Throws<ArgumentException>(() =>
            service.PrepareToAnimate(null!, source));
    }

    [Fact]
    public void PrepareToAnimate_EmptyKey_ThrowsArgumentException()
    {
        var service = CreateService();
        var source = new Border();
        Assert.Throws<ArgumentException>(() =>
            service.PrepareToAnimate(string.Empty, source));
    }

    [Fact]
    public void PrepareToAnimate_NullSource_ThrowsArgumentNullException()
    {
        var service = CreateService();
        Assert.Throws<ArgumentNullException>(() =>
            service.PrepareToAnimate("key", null!));
    }

    [Fact]
    public void PrepareToAnimate_ReturnsAnimation_WithMatchingKey()
    {
        var service = CreateService();
        var animation = service.PrepareToAnimate("hero", new Border());
        Assert.Equal("hero", animation.Key);
    }

    [Fact]
    public void PrepareToAnimate_AnimationInitiallyNotConsumed()
    {
        var service = CreateService();
        var animation = service.PrepareToAnimate("hero", new Border());
        Assert.False(animation.IsConsumed);
    }

    [Fact]
    public void GetAnimation_AfterPrepare_ReturnsAnimation()
    {
        var service = CreateService();
        var animation = service.PrepareToAnimate("hero", new Border());
        Assert.Same(animation, service.GetAnimation("hero"));
    }

    [Fact]
    public void GetAnimation_ReturnsSameInstance_ForSameKey()
    {
        var service = CreateService();
        service.PrepareToAnimate("hero", new Border());
        var a1 = service.GetAnimation("hero");
        var a2 = service.GetAnimation("hero");
        Assert.Same(a1, a2);
    }

    [Fact]
    public void PrepareToAnimate_SameKey_ReplacesOldAnimation()
    {
        var service = CreateService();
        var first = service.PrepareToAnimate("hero", new Border());
        var second = service.PrepareToAnimate("hero", new Border());

        Assert.NotSame(first, second);
        Assert.True(first.IsConsumed || first.IsDisposed);
        Assert.Same(second, service.GetAnimation("hero"));
    }

    [Fact]
    public void PrepareToAnimate_DifferentKeys_BothInService()
    {
        var service = CreateService();
        var a1 = service.PrepareToAnimate("key1", new Border());
        var a2 = service.PrepareToAnimate("key2", new Border());

        Assert.Same(a1, service.GetAnimation("key1"));
        Assert.Same(a2, service.GetAnimation("key2"));
    }
}

public class ConnectedAnimationTests : ScopedTestBase
{
    private static ConnectedAnimationService CreateService() => new ConnectedAnimationService();

    [Fact]
    public void Key_MatchesPreparedKey()
    {
        var service = CreateService();
        var animation = service.PrepareToAnimate("myKey", new Border());
        Assert.Equal("myKey", animation.Key);
    }

    [Fact]
    public void IsConsumed_InitiallyFalse()
    {
        var service = CreateService();
        var animation = service.PrepareToAnimate("hero", new Border());
        Assert.False(animation.IsConsumed);
    }

    [Fact]
    public void Configuration_InitiallyNull()
    {
        var service = CreateService();
        var animation = service.PrepareToAnimate("hero", new Border());
        Assert.Null(animation.Configuration);
    }

    [Fact]
    public void Configuration_GravityRoundTrips()
    {
        var service = CreateService();
        var animation = service.PrepareToAnimate("hero", new Border());
        var config = new GravityConnectedAnimationConfiguration { IsShadowEnabled = false };
        animation.Configuration = config;
        Assert.Same(config, animation.Configuration);
    }

    [Fact]
    public void Configuration_DirectRoundTrips()
    {
        var service = CreateService();
        var animation = service.PrepareToAnimate("hero", new Border());
        var config = new DirectConnectedAnimationConfiguration { Duration = TimeSpan.FromMilliseconds(100) };
        animation.Configuration = config;
        Assert.Same(config, animation.Configuration);
    }

    [Fact]
    public void Configuration_BasicRoundTrips()
    {
        var service = CreateService();
        var animation = service.PrepareToAnimate("hero", new Border());
        var config = new BasicConnectedAnimationConfiguration();
        animation.Configuration = config;
        Assert.Same(config, animation.Configuration);
    }

    [Fact]
    public void TryStart_ReturnsTrue_WhenNotConsumed()
    {
        var service = CreateService();
        var animation = service.PrepareToAnimate("hero", new Border());
        var result = animation.TryStart(new Border());
        Assert.True(result);
    }

    [Fact]
    public void TryStart_ConsumesAnimation()
    {
        var service = CreateService();
        var animation = service.PrepareToAnimate("hero", new Border());
        animation.TryStart(new Border());
        Assert.True(animation.IsConsumed);
    }

    [Fact]
    public void TryStart_SecondCall_ReturnsFalse()
    {
        var service = CreateService();
        var animation = service.PrepareToAnimate("hero", new Border());
        animation.TryStart(new Border());
        var second = animation.TryStart(new Border());
        Assert.False(second);
    }

    [Fact]
    public void TryStart_ReturnsFalse_WhenDisposed()
    {
        var service = CreateService();
        var animation = service.PrepareToAnimate("hero", new Border());
        animation.Dispose();
        var result = animation.TryStart(new Border());
        Assert.False(result);
    }

    [Fact]
    public void TryStart_WithEmptyCoordinatedElements_ReturnsTrue()
    {
        var service = CreateService();
        var animation = service.PrepareToAnimate("hero", new Border());
        var result = animation.TryStart(new Border(), Array.Empty<Visual>());
        Assert.True(result);
    }

    [Fact]
    public void TryStart_WhenNoTopLevel_FiresCompleted()
    {
        var service = CreateService();
        var animation = service.PrepareToAnimate("hero", new Border());

        ConnectedAnimationCompletedEventArgs? received = null;
        animation.Completed += (_, e) => received = e;

        animation.TryStart(new Border()); // no TopLevel ancestor

        Assert.NotNull(received);
    }

    [Fact]
    public void TryStart_WhenNoTopLevel_Completed_NotCancelled()
    {
        var service = CreateService();
        var animation = service.PrepareToAnimate("hero", new Border());

        bool? cancelled = null;
        animation.Completed += (_, e) => cancelled = e.Cancelled;

        animation.TryStart(new Border());

        Assert.False(cancelled);
    }

    [Fact]
    public void TryStart_WhenNoTopLevel_RemovesFromService()
    {
        var service = CreateService();
        var animation = service.PrepareToAnimate("hero", new Border());

        animation.TryStart(new Border());

        Assert.Null(service.GetAnimation("hero"));
    }

    [Fact]
    public void TryStart_WithCoordinatedElements_WhenNoTopLevel_FiresCompleted()
    {
        var service = CreateService();
        var animation = service.PrepareToAnimate("hero", new Border());
        var coordinated = new Visual[] { new Border(), new Border() };

        bool fired = false;
        animation.Completed += (_, _) => fired = true;

        animation.TryStart(new Border(), coordinated);

        Assert.True(fired);
    }

    [Fact]
    public void Dispose_RemovesFromService()
    {
        var service = CreateService();
        service.PrepareToAnimate("hero", new Border());
        Assert.NotNull(service.GetAnimation("hero"));

        service.GetAnimation("hero")!.Dispose();

        Assert.Null(service.GetAnimation("hero"));
    }

    [Fact]
    public void Dispose_CalledTwice_IsNoOp()
    {
        var service = CreateService();
        var animation = service.PrepareToAnimate("hero", new Border());

        animation.Dispose();
        animation.Dispose(); // must not throw
    }

    [Fact]
    public void Dispose_WhenNotMidFlight_DoesNotFireCompleted()
    {
        var service = CreateService();
        var animation = service.PrepareToAnimate("hero", new Border());

        bool fired = false;
        animation.Completed += (_, _) => fired = true;

        animation.Dispose();

        Assert.False(fired);
    }

    [Fact]
    public void GetAnimation_ReturnsNull_AfterTryStart()
    {
        var service = CreateService();
        service.PrepareToAnimate("hero", new Border());
        service.GetAnimation("hero")!.TryStart(new Border());
        Assert.Null(service.GetAnimation("hero"));
    }

    [Fact]
    public void GetAnimation_ReturnsNull_AfterDispose()
    {
        var service = CreateService();
        service.PrepareToAnimate("hero", new Border());
        service.GetAnimation("hero")!.Dispose();
        Assert.Null(service.GetAnimation("hero"));
    }

    [Fact]
    public void GetAnimation_ReturnsNull_WhenAnimationIsConsumed()
    {
        var service = CreateService();
        var animation = service.PrepareToAnimate("hero", new Border());
        animation.TryStart(new Border());
        Assert.Null(service.GetAnimation("hero"));
    }

    [Fact]
    public void MultipleAnimations_IndependentLifecycles()
    {
        var service = CreateService();
        var a1 = service.PrepareToAnimate("anim1", new Border());
        var a2 = service.PrepareToAnimate("anim2", new Border());

        a1.TryStart(new Border());

        Assert.True(a1.IsConsumed);
        Assert.False(a2.IsConsumed);
        Assert.Null(service.GetAnimation("anim1"));
        Assert.NotNull(service.GetAnimation("anim2"));
    }

    [Fact]
    public void PrepareToAnimate_AfterDispose_CanPrepareAgain()
    {
        var service = CreateService();
        var first = service.PrepareToAnimate("hero", new Border());
        first.Dispose();

        var second = service.PrepareToAnimate("hero", new Border());
        Assert.NotSame(first, second);
        Assert.Same(second, service.GetAnimation("hero"));
    }

    // ResolveTimingAndEasing tests — no reflection needed, method is internal.

    [Fact]
    public void Configuration_DirectWithDuration_UsesProvidedDuration()
    {
        var service = CreateService();
        var animation = service.PrepareToAnimate("hero", new Border());
        animation.Configuration = new DirectConnectedAnimationConfiguration
        {
            Duration = TimeSpan.FromMilliseconds(250)
        };

        animation.ResolveTimingAndEasing(service, out var resolvedDuration, out _, out _, out _);

        Assert.Equal(TimeSpan.FromMilliseconds(250), resolvedDuration);
    }

    [Fact]
    public void Configuration_DirectWithNullDuration_Uses150ms()
    {
        var service = CreateService();
        var animation = service.PrepareToAnimate("hero", new Border());
        animation.Configuration = new DirectConnectedAnimationConfiguration { Duration = null };

        animation.ResolveTimingAndEasing(service, out var resolvedDuration, out _, out _, out _);

        Assert.Equal(TimeSpan.FromMilliseconds(150), resolvedDuration);
    }

    [Fact]
    public void Configuration_Basic_UsesServiceDefaultDuration()
    {
        var service = CreateService();
        service.DefaultDuration = TimeSpan.FromMilliseconds(400);
        var animation = service.PrepareToAnimate("hero", new Border());
        animation.Configuration = new BasicConnectedAnimationConfiguration();

        animation.ResolveTimingAndEasing(service, out var resolvedDuration, out _, out _, out _);

        Assert.Equal(TimeSpan.FromMilliseconds(400), resolvedDuration);
    }

    [Fact]
    public void Configuration_Gravity_UsesServiceDefaultDuration()
    {
        var service = CreateService();
        service.DefaultDuration = TimeSpan.FromMilliseconds(350);
        var animation = service.PrepareToAnimate("hero", new Border());
        animation.Configuration = new GravityConnectedAnimationConfiguration();

        animation.ResolveTimingAndEasing(service, out var resolvedDuration, out _, out _, out _);

        Assert.Equal(TimeSpan.FromMilliseconds(350), resolvedDuration);
    }

    [Fact]
    public void Configuration_Gravity_UseGravityDip_IsTrue()
    {
        var service = CreateService();
        var animation = service.PrepareToAnimate("hero", new Border());
        animation.Configuration = new GravityConnectedAnimationConfiguration();

        animation.ResolveTimingAndEasing(service, out _, out _, out var useGravityDip, out _);

        Assert.True(useGravityDip);
    }

    [Fact]
    public void Configuration_Direct_UseGravityDip_IsFalse()
    {
        var service = CreateService();
        var animation = service.PrepareToAnimate("hero", new Border());
        animation.Configuration = new DirectConnectedAnimationConfiguration();

        animation.ResolveTimingAndEasing(service, out _, out _, out var useGravityDip, out _);

        Assert.False(useGravityDip);
    }

    [Fact]
    public void Configuration_Basic_UseGravityDip_IsFalse()
    {
        var service = CreateService();
        var animation = service.PrepareToAnimate("hero", new Border());
        animation.Configuration = new BasicConnectedAnimationConfiguration();

        animation.ResolveTimingAndEasing(service, out _, out _, out var useGravityDip, out _);

        Assert.False(useGravityDip);
    }

    [Fact]
    public void Configuration_Gravity_IsShadowEnabled_False_UseShadow_IsFalse()
    {
        var service = CreateService();
        var animation = service.PrepareToAnimate("hero", new Border());
        animation.Configuration = new GravityConnectedAnimationConfiguration { IsShadowEnabled = false };

        animation.ResolveTimingAndEasing(service, out _, out _, out _, out var useShadow);

        Assert.False(useShadow);
    }

    [Fact]
    public void Configuration_Gravity_IsShadowEnabled_True_UseShadow_IsTrue()
    {
        var service = CreateService();
        var animation = service.PrepareToAnimate("hero", new Border());
        animation.Configuration = new GravityConnectedAnimationConfiguration { IsShadowEnabled = true };

        animation.ResolveTimingAndEasing(service, out _, out _, out _, out var useShadow);

        Assert.True(useShadow);
    }
}

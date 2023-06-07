using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Avalonia.Animation.Easings;
using Avalonia.Base.UnitTests.Rendering;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Xunit;
using Xunit.Sdk;

namespace Avalonia.Base.UnitTests.Composition;

public class CompositionAnimationTests
{

    class AnimationDataProvider : DataAttribute
    {
        IEnumerable<AnimationData> Generate() =>
            new AnimationData[]
            {
                new("3 frames starting from 0")
                {
                    Frames =
                    {
                        (0f, 10f),
                        (0.5f, 30f),
                        (1f, 20f)
                    },
                    Checks =
                    {
                        (0.25f, 20f),
                        (0.5f, 30f),
                        (0.75f, 25f),
                        (1f, 20f)
                    }
                },
                new("1 final frame")
                {
                    Frames =
                    {
                        (1f, 10f)
                    },
                    Checks =
                    {
                        (0f, 0f),
                        (0.5f, 5f),
                        (1f, 10f)
                    }
                }
            };

        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            foreach (var ani in Generate())
            {
                yield return new Object[] { ani };
            }
        }
    }

    class DummyDispatcher : IDispatcher
    {
        public bool CheckAccess() => true;

        public void VerifyAccess()
        {
        }

        public void Post(Action action, DispatcherPriority priority = default) => throw new NotSupportedException();
    }
    
    [AnimationDataProvider]
    [Theory]
    public void GenericCheck(AnimationData data)
    {
        using var scope = AvaloniaLocator.EnterScope();
        var compositor =
            new Compositor(new RenderLoop(new CompositorTestServices.ManualRenderTimer()), null);
        var target = compositor.CreateSolidColorVisual();
        var ani = new ScalarKeyFrameAnimation(null);
        foreach (var frame in data.Frames)
            ani.InsertKeyFrame(frame.key, frame.value, new LinearEasing());
        ani.Duration = TimeSpan.FromSeconds(1);
        var instance = ani.CreateInstance(target.Server, null);
        instance.Initialize(TimeSpan.Zero, data.StartingValue, ServerCompositionVisual.s_IdOfRotationAngleProperty);
        var currentValue = ExpressionVariant.Create(data.StartingValue);
        foreach (var check in data.Checks)
        {
            currentValue = instance.Evaluate(TimeSpan.FromSeconds(check.time), currentValue);
            Assert.Equal(check.value, currentValue.Scalar);
        }
        
    }
    
    public class AnimationData
    {
        public AnimationData(string name)
        {
            Name = name;
        }

        public string Name { get;  }
        public List<(float key, float value)> Frames { get; set; } = new();
        public List<(float time, float value)> Checks { get; set; } = new();
        public float StartingValue { get; set; }
        public float Duration { get; set; } = 1;
        public override string ToString()
        {
            return Name;
        }
    }
}
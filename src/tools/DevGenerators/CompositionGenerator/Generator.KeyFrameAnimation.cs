namespace Avalonia.SourceGenerator.CompositionGenerator
{
    public partial class Generator
    {
        void GenerateAnimations()
        {
            var code = $@"using System.Numerics;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Expressions;

// Special license applies <see href=""https://raw.githubusercontent.com/AvaloniaUI/Avalonia/master/src/Avalonia.Base/Rendering/Composition/License.md\"">License.md</see>

namespace Avalonia.Rendering.Composition
{{
";

            foreach (var a in _config.KeyFrameAnimations)
            {
                var name = a.Name ?? a.Type;

                code += $@"
    public class {name}KeyFrameAnimation : KeyFrameAnimation
    {{
        public {name}KeyFrameAnimation(Compositor compositor) : base(compositor)
        {{
        }}

        internal override IAnimationInstance CreateInstance(Avalonia.Rendering.Composition.Server.ServerObject targetObject, ExpressionVariant? finalValue)
        {{
            return new KeyFrameAnimationInstance<{a.Type}>({name}Interpolator.Instance, _keyFrames.Snapshot(), CreateSnapshot(), 
                finalValue?.CastOrDefault<{a.Type}>(), targetObject,
                DelayBehavior, DelayTime, Direction, Duration, IterationBehavior,
                IterationCount, StopBehavior);
        }}
        
        private KeyFrames<{a.Type}> _keyFrames = new KeyFrames<{a.Type}>();
        private protected override IKeyFrames KeyFrames => _keyFrames;

        public void InsertKeyFrame(float normalizedProgressKey, {a.Type} value, Avalonia.Animation.Easings.IEasing easingFunction)
        {{
            _keyFrames.Insert(normalizedProgressKey, value, easingFunction);
        }}
        
        public void InsertKeyFrame(float normalizedProgressKey, {a.Type} value)
        {{
            _keyFrames.Insert(normalizedProgressKey, value, Compositor.DefaultEasing);
        }}
    }}

    public partial class Compositor
    {{
        public {name}KeyFrameAnimation Create{name}KeyFrameAnimation() => new {name}KeyFrameAnimation(this);
    }}
";
            }

            code += "}";
            _output.AddSource("CompositionAnimations.cs", code);
        }
    }
}

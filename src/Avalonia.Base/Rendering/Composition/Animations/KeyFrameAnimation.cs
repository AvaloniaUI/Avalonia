namespace Avalonia.Rendering.Composition.Animations
{
    public abstract class KeyFrameAnimation : CompositionAnimation
    {
        internal KeyFrameAnimation(Compositor compositor) : base(compositor)
        {
        }
        
        public AnimationDelayBehavior DelayBehavior { get; set; }
        public System.TimeSpan DelayTime { get; set; }
        public AnimationDirection Direction { get; set; }
        public System.TimeSpan Duration { get; set; }
        public AnimationIterationBehavior IterationBehavior { get; set; }
        public int IterationCount { get; set; } = 1;
        public AnimationStopBehavior StopBehavior { get; set; }
        
        private protected abstract IKeyFrames KeyFrames { get; }
        
        public void InsertExpressionKeyFrame(float normalizedProgressKey, string value,
            CompositionEasingFunction easingFunction) =>
            KeyFrames.InsertExpressionKeyFrame(normalizedProgressKey, value, easingFunction);

        public void InsertExpressionKeyFrame(float normalizedProgressKey, string value) 
            => KeyFrames.InsertExpressionKeyFrame(normalizedProgressKey, value, new LinearEasingFunction(Compositor));
    }

    public enum AnimationDelayBehavior
    {
        SetInitialValueAfterDelay,
        SetInitialValueBeforeDelay
    }

    public enum AnimationDirection
    {
        Normal,
        Reverse,
        Alternate,
        AlternateReverse
    }

    public enum AnimationIterationBehavior
    {
        Count,
        Forever
    }

    public enum AnimationStopBehavior
    {
        LeaveCurrentValue,
        SetToInitialValue,
        SetToFinalValue
    }
}
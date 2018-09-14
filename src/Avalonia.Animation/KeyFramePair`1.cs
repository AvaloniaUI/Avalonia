namespace Avalonia.Animation
{
    /// <summary>
    /// Represents a pair of keyframe, usually the
    /// Start and End keyframes of a <see cref="Animator{T}"/> object.
    /// </summary>
    public struct KeyFramePair<T>
    {
        /// <summary>
        /// Initializes this <see cref="KeyFramePair{T}"/>
        /// </summary>
        /// <param name="FirstKeyFrame"></param>
        /// <param name="LastKeyFrame"></param>
        public KeyFramePair((T TargetValue, bool isNeutral) FirstKeyFrame, (T TargetValue, bool isNeutral) LastKeyFrame) : this()
        {
            this.FirstKeyFrame = FirstKeyFrame;
            this.SecondKeyFrame = LastKeyFrame;
        }

        /// <summary>
        /// First <see cref="KeyFrame"/> object.
        /// </summary>
        public (T TargetValue, bool isNeutral) FirstKeyFrame { get; }

        /// <summary>
        /// Second <see cref="KeyFrame"/> object.
        /// </summary>
        public (T TargetValue, bool isNeutral) SecondKeyFrame { get; }
    }
}

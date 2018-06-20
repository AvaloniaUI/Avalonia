using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Collections;
using System.ComponentModel;
using Avalonia.Animation.Utils;
using System.Reactive.Linq;
using System.Linq;
using Avalonia.Data;
using System.Reactive.Disposables;

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
        public KeyFramePair(KeyValuePair<double, InternalKeyFrame<T>> FirstKeyFrame, KeyValuePair<double, InternalKeyFrame<T>> LastKeyFrame) : this()
        {
            this.FirstKeyFrame = FirstKeyFrame;
            this.SecondKeyFrame = LastKeyFrame;
        }

        /// <summary>
        /// First <see cref="KeyFrame"/> object.
        /// </summary>
        public KeyValuePair<double, InternalKeyFrame<T>> FirstKeyFrame { get; private set; }

        /// <summary>
        /// Second <see cref="KeyFrame"/> object.
        /// </summary>
        public KeyValuePair<double, InternalKeyFrame<T>> SecondKeyFrame { get; private set; }
    }


    public class InternalKeyFrame<T>
    {
        public T TargetValue { get; set; }
        public IBinding bindingObj { get; set; }
        public bool isNeutral { get; set; }
        public bool isBinding { get; set; }
    }
}